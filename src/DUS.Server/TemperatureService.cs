using DUS.Contracts;
using DUS.Security;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Threading;

namespace DUS.Server
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class TemperatureService : ITemperatureService
    {
        private readonly RSACryptoServiceProvider _serverPrivate;

        private readonly ConcurrentDictionary<string, RSACryptoServiceProvider> _clientPublicKeys =
            new ConcurrentDictionary<string, RSACryptoServiceProvider>();

        private readonly ConcurrentDictionary<string, ClientState> _clients =
            new ConcurrentDictionary<string, ClientState>();

        private readonly ConcurrentDictionary<string, long> _lastMsgId =
            new ConcurrentDictionary<string, long>();

        private readonly ConcurrentDictionary<string, long> _lastTs =
            new ConcurrentDictionary<string, long>();

        private readonly Timer _failureDetector;
        private readonly Timer _consensusTimer;

        private static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan FailureScanPeriod = TimeSpan.FromSeconds(5);

        public TemperatureService()
        {
            KeyPaths.EnsureDirs();

            _serverPrivate = KeyStore.LoadOrCreateRsa(KeyPaths.ServerPrivate, includePrivate: true);

            if (System.IO.File.Exists(KeyPaths.ServerPrivatePublic))
                System.IO.File.Copy(KeyPaths.ServerPrivatePublic, KeyPaths.ServerPublic, true);
            else
                System.IO.File.WriteAllText(KeyPaths.ServerPublic, _serverPrivate.ToXmlString(false));

            _failureDetector = new Timer(_ => FailoverTick(), null, FailureScanPeriod, FailureScanPeriod);
            _consensusTimer = new Timer(_ => ConsensusTick(), null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        public RegisterResponse Register(SecureEnvelope request)
        {
            return new RegisterResponse
            {
                Ok = false,
                Error = "V0: Register handshake nije ukljucen. Koristite pre-shared klijentske public key fajlove na serveru."
            };
        }

        public HeartbeatResponse Heartbeat(SecureEnvelope request)
        {
            try
            {
                AntiReplayCheck(request);

                var pub = GetClientPublicKeyOrThrow(request.ClientId);
                CryptoService.DecryptAndVerify<HeartbeatPayload>(request, _serverPrivate, pub);

                var state = _clients.AddOrUpdate(request.ClientId,
                    _ => new ClientState { ClientId = request.ClientId, Role = ClientRole.Standby },
                    (_, s) => s);

                state.LastSeenUtc = DateTime.UtcNow;
                state.IsDead = false;

                // Ako se vratio nakon mrtav, u standby
                if (DateTime.UtcNow < state.DeadUntilUtc)
                {
                    state.Role = ClientRole.Standby;
                    return new HeartbeatResponse
                    {
                        Ok = true,
                        Role = state.Role,
                        AssignedConsoleColor = state.AssignedColor
                    };
                }

                EnsureFiveActive();

                return new HeartbeatResponse
                {
                    Ok = true,
                    Role = state.Role,
                    AssignedConsoleColor = state.AssignedColor
                };
            }
            catch (Exception ex)
            {
                return new HeartbeatResponse { Ok = false, Error = ex.Message };
            }
        }

        public AckResponse SendMeasurement(SecureEnvelope request)
        {
            try
            {
                AntiReplayCheck(request);

                var pub = GetClientPublicKeyOrThrow(request.ClientId);
                var payload = CryptoService.DecryptAndVerify<MeasurementPayload>(request, _serverPrivate, pub);

                using (var db = new DusDbContext())
                {
                    db.Measurements.Add(new MeasurementEntity
                    {
                        ClientId = request.ClientId,
                        SensorId = payload.SensorId,
                        TimestampUtc = DateTimeOffset.FromUnixTimeMilliseconds(payload.MeasuredAtUnixMs).UtcDateTime,
                        Value = payload.Value,
                        Quality = (int)payload.Quality,
                        Priority = (int)payload.Priority,
                        IsConsensus = false
                    });
                    db.SaveChanges();
                }

                if (payload.Priority != AlarmPriority.P0_None)
                {
                    var old = Console.ForegroundColor;
                    if (payload.Priority == AlarmPriority.P1) Console.ForegroundColor = ConsoleColor.Yellow;
                    else if (payload.Priority == AlarmPriority.P2) Console.ForegroundColor = ConsoleColor.DarkYellow; // “narandžasto”
                    else if (payload.Priority == AlarmPriority.P3) Console.ForegroundColor = ConsoleColor.Red;

                    Console.WriteLine("ALARM {0} client={1} sensor={2} value={3:F2}",
                        payload.Priority, request.ClientId, payload.SensorId, payload.Value);

                    Console.ForegroundColor = old;
                }


                return new AckResponse { Ok = true };
            }
            catch (Exception ex)
            {
                return new AckResponse { Ok = false, Error = ex.Message };
            }
        }

        public AlarmReportResponse GetAlarmReport(SecureEnvelope request)
        {
            try
            {
                AntiReplayCheck(request);

                var pub = GetClientPublicKeyOrThrow(request.ClientId);
                var rq = CryptoService.DecryptAndVerify<ReportRequestPayload>(request, _serverPrivate, pub);

                var from = DateTimeOffset.FromUnixTimeMilliseconds(rq.FromUnixMs).UtcDateTime;
                var to = DateTimeOffset.FromUnixTimeMilliseconds(rq.ToUnixMs).UtcDateTime;

                using (var db = new DusDbContext())
                {
                    var raw = db.Measurements
                        .Where(m => !m.IsConsensus && m.Priority > 0 && m.TimestampUtc >= from && m.TimestampUtc <= to)
                        .OrderByDescending(m => m.Priority)
                        .ThenBy(m => m.TimestampUtc)
                        .Select(m => new { m.SensorId, m.Priority, m.Value, m.TimestampUtc })
                        .ToList();

                    var items = raw
                        .Select(m => new AlarmReportItemDto
                        {
                            SensorId = m.SensorId,
                            Priority = (AlarmPriority)m.Priority,
                            Value = m.Value,
                            TimeUnixMs = new DateTimeOffset(m.TimestampUtc).ToUnixTimeMilliseconds()
                        })
                        .ToList();


                    return new AlarmReportResponse { Ok = true, Items = items };
                }
            }
            catch (Exception ex)
            {
                return new AlarmReportResponse { Ok = false, Error = ex.Message };
            }
        }

        private void AntiReplayCheck(SecureEnvelope env)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // tolerancija za sat (2 min)
            if (Math.Abs(now - env.TimestampUnixMs) > 120000)
                throw new InvalidOperationException("Timestamp out of allowed skew.");

            var lastId = _lastMsgId.GetOrAdd(env.ClientId, -1);
            if (env.MessageId <= lastId)
                throw new InvalidOperationException("Replay/duplicate MessageId.");
            _lastMsgId[env.ClientId] = env.MessageId;

            var lastTs = _lastTs.GetOrAdd(env.ClientId, 0);
            if (env.TimestampUnixMs < lastTs)
                throw new InvalidOperationException("Timestamp went backwards.");
            _lastTs[env.ClientId] = env.TimestampUnixMs;
        }

        private RSACryptoServiceProvider GetClientPublicKeyOrThrow(string clientId)
        {
            RSACryptoServiceProvider rsa;
            if (_clientPublicKeys.TryGetValue(clientId, out rsa))
                return rsa;

            var path = KeyPaths.ClientPublic(clientId);
            if (!System.IO.File.Exists(path))
                throw new InvalidOperationException("Unknown client. Missing public key: " + path);

            rsa = KeyStore.LoadPublic(path);
            _clientPublicKeys[clientId] = rsa;
            return rsa;
        }

        private void FailoverTick()
        {
            var now = DateTime.UtcNow;

            foreach (var kv in _clients)
            {
                var s = kv.Value;
                if (s.Role == ClientRole.Active && (now - s.LastSeenUtc) > HeartbeatTimeout)
                {
                    s.IsDead = true;
                    s.Role = ClientRole.Standby;
                    s.DeadUntilUtc = DateTime.UtcNow.AddSeconds(30); // “karantin” 30s

                }
            }

            EnsureFiveActive();
        }

        private void EnsureFiveActive()
        {
            var active = _clients.Values.Count(c => c.Role == ClientRole.Active && !c.IsDead);
            if (active >= 5) return;

            var candidates = _clients.Values
                .Where(c => c.Role == ClientRole.Standby && !c.IsDead && (DateTime.UtcNow - c.LastSeenUtc) <= HeartbeatTimeout)
                .OrderByDescending(c => c.LastSeenUtc)
                .ToList();

            foreach (var c in candidates)
            {
                if (active >= 5) break;
                c.Role = ClientRole.Active;
                active++;
            }
        }

        private void ConsensusTick()
        {
            try
            {
                var to = DateTime.UtcNow;
                var from = to.AddMinutes(-1);

                using (var db = new DusDbContext())
                {
                    var values = db.Measurements
                        .Where(m => !m.IsConsensus
                                    && m.TimestampUtc >= from
                                    && m.TimestampUtc < to
                                    && m.Quality == (int)DataQuality.GOOD)
                        .OrderBy(m => m.TimestampUtc)
                        .ToList();

                    if (values.Count == 0) return;

                    var avg = values.Average(v => v.Value);
                    const double eps = 5.0;

                    var lastInRange = values.LastOrDefault(v => Math.Abs(v.Value - avg) <= eps) ?? values.Last();

                    db.Measurements.Add(new MeasurementEntity
                    {
                        ClientId = "SERVER",
                        SensorId = 0,
                        TimestampUtc = to,
                        Value = lastInRange.Value,
                        Quality = (int)DataQuality.GOOD,
                        Priority = 0,
                        IsConsensus = true
                    });
                    db.SaveChanges();
                }
            }
            catch
            {
                // V0: ne rusimo server zbog konsenzusa
            }
        }

        private class ClientState
        {
            public string ClientId;
            public ClientRole Role;
            public DateTime LastSeenUtc;
            public bool IsDead;
            public string AssignedColor;
            public DateTime DeadUntilUtc;


            public ClientState()
            {
                ClientId = "";
                Role = ClientRole.Standby;
                LastSeenUtc = DateTime.UtcNow;
                IsDead = false;
                AssignedColor = "Gray";
                DeadUntilUtc = DateTime.MinValue;

            }
        }
    }
}
