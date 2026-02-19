using DUS.Contracts;
using DUS.Security;
using System;
using System.IO;
using System.ServiceModel;
using System.Threading;

namespace DUS.SensorClient
{
    class Program
    {
        //  MessageId raste posle svake poruke (anti-replay)
        static long _msgId = 0;

        //  rola Active/Standby (server dodeljuje); volatile zbog Timer thread-a
        static volatile ClientRole _role = ClientRole.Standby;

        //  boja koju server dodeli active senzoru
        static ConsoleColor _baseColor = ConsoleColor.Gray;
        static ClientRole _lastRolePrinted = (ClientRole)(-1);

        //  sleep simulira diskonekt (bez heartbeata)
        static volatile bool _commPaused = false;

        //  TIMER mora biti globalan da bismo mogli da ga pauziramo
        static Timer _hbTimer;

        static void Main(string[] args)
        {
            // pre pokretanja dati ID/ime
            var clientId = GetArg(args, "--clientId") ?? ("C" + new Random().Next(100, 999));

            // senzor ID
            var sensorId = int.Parse(GetArg(args, "--sensorId") ?? "1");

            // Opseg temperature [min, max]
            var min = double.Parse(GetArg(args, "--min") ?? "10");
            var max = double.Parse(GetArg(args, "--max") ?? "100");

            // Granice alarma a1,a2,a3
            var a1 = double.Parse(GetArg(args, "--a1") ?? "60");
            var a2 = double.Parse(GetArg(args, "--a2") ?? "75");
            var a3 = double.Parse(GetArg(args, "--a3") ?? "90");

            // Kvalitet (GOOD/BAD/UNCERTAIN)
            var qStr = GetArg(args, "--q") ?? "GOOD";
            DataQuality q;
            if (!Enum.TryParse(qStr, out q)) q = DataQuality.GOOD;

            Directory.CreateDirectory("keys");

            // Client keys
            var clientRsa = KeyStore.LoadOrCreateRsa(
                Path.Combine("keys", clientId + ".private.xml"),
                includePrivate: true);

            File.WriteAllText(
                Path.Combine("keys", clientId + ".public.xml"),
                clientRsa.ToXmlString(false));

            CopyPublicKeyToServer(clientId);

            // Server public key
            var serverPubPath = Path.Combine("keys", "server.public.xml");
            if (!File.Exists(serverPubPath))
            {
                Console.WriteLine("Missing keys\\server.public.xml. Copy server public key here first.");
                Console.ReadLine();
                return;
            }

            var serverPub = KeyStore.LoadPublic(serverPubPath);

            var factory = new ChannelFactory<ITemperatureService>("TemperatureServiceEndpoint");
            var proxy = factory.CreateChannel();

            Console.WriteLine("Client {0} sensor={1}", clientId, sensorId);
            Console.WriteLine("Commands: type 'sleep' or 'crash' then ENTER");
            Console.WriteLine("NOTE: Copy keys\\{0}.public.xml to Server\\bin\\...\\keys\\clients\\{0}.public.xml", clientId);
            Console.WriteLine();

            //  Heartbeat na svakih 5 sekundi
            _hbTimer = new Timer(_ =>
            {
                if (_commPaused) return;

                try
                {
                    var env = CryptoService.EncryptAndSign(
                        clientId,
                        Interlocked.Increment(ref _msgId),
                        MessageType.Heartbeat,
                        new HeartbeatPayload(),
                        clientRsa,
                        serverPub);

                    var resp = proxy.Heartbeat(env);

                    // ako je u međuvremenu user ukucao sleep
                    if (_commPaused) return;

                    if (resp != null && resp.Ok)
                    {
                        _role = resp.Role;

                        if (_role != _lastRolePrinted)
                        {
                            Console.WriteLine($"ROLE UPDATE: {_role} (assignedColor={resp.AssignedConsoleColor})");
                            _lastRolePrinted = _role;
                        }

                        ConsoleColor cc;
                        if (!string.IsNullOrWhiteSpace(resp.AssignedConsoleColor)
                            && Enum.TryParse(resp.AssignedConsoleColor, true, out cc))
                        {
                            _baseColor = cc;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (!_commPaused)
                        Console.WriteLine("HB ERROR: " + ex.Message);
                }

            }, null, 0, 5000);

            var rnd = new Random();

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var cmd = Console.ReadLine();

                    if (cmd == "crash")
                    {
                        Console.WriteLine("CRASHING NOW...");
                        Environment.FailFast("Simulated crash");
                    }

                    if (cmd == "sleep")
                    {
                        _commPaused = true;

                        // odmah postani standby
                        _role = ClientRole.Standby;
                        _lastRolePrinted = (ClientRole)(-1);

                        // ZAUSTAVI heartbeat potpuno
                        _hbTimer?.Change(Timeout.Infinite, Timeout.Infinite);

                        Console.WriteLine("SIMULATING DISCONNECT for 20s...");
                        Thread.Sleep(20000);

                        Console.WriteLine("BACK ONLINE.");

                        _commPaused = false;

                        // PONOVO uključi heartbeat
                        _hbTimer?.Change(0, 5000);
                    }
                }

                if (_role == ClientRole.Active)
                {
                    Thread.Sleep(rnd.Next(1000, 10001));

                    // rola se možda promenila dok si spavao
                    if (_commPaused || _role != ClientRole.Active)
                        continue;

                    var value = min + rnd.NextDouble() * (max - min);
                    var priority = CalcPriority(value, a1, a2, a3);

                    PrintValue(value, priority);

                    var payload = new MeasurementPayload
                    {
                        SensorId = sensorId,
                        Value = value,
                        Quality = q,
                        Priority = priority,
                        MeasuredAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                    };

                    var env = CryptoService.EncryptAndSign(
                        clientId,
                        Interlocked.Increment(ref _msgId),
                        MessageType.Measurement,
                        payload,
                        clientRsa,
                        serverPub);

                    proxy.SendMeasurement(env);
                }
                else
                {
                    Thread.Sleep(300);
                }
            }
        }

        static AlarmPriority CalcPriority(double v, double a1, double a2, double a3)
        {
            if (v >= a3) return AlarmPriority.P3;
            if (v >= a2) return AlarmPriority.P2;
            if (v >= a1) return AlarmPriority.P1;
            return AlarmPriority.P0_None;
        }

        static void PrintValue(double v, AlarmPriority p)
        {
            var old = Console.ForegroundColor;

            Console.ForegroundColor = _baseColor;

            if (p == AlarmPriority.P1) Console.ForegroundColor = ConsoleColor.Yellow;
            else if (p == AlarmPriority.P2) Console.ForegroundColor = ConsoleColor.DarkYellow;
            else if (p == AlarmPriority.P3) Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine("Temperature is {0:F2} at {1}",
                v, DateTime.Now.ToString("HH:mm:ss"));

            Console.ForegroundColor = old;
        }

        static string GetArg(string[] args, string key)
        {
            var idx = Array.IndexOf(args, key);
            if (idx >= 0 && idx + 1 < args.Length) return args[idx + 1];
            return null;
        }

        static void CopyPublicKeyToServer(string clientId)
        {
            try
            {
                string currentDir = AppDomain.CurrentDomain.BaseDirectory;
                string clientPublic = Path.Combine(currentDir, "keys", $"{clientId}.public.xml");

                string serverClientsDir = Environment.GetEnvironmentVariable("DUS_SERVER_CLIENT_KEYS");

                if (string.IsNullOrWhiteSpace(serverClientsDir))
                {
                    Console.WriteLine("[AUTO] Env var DUS_SERVER_CLIENT_KEYS not set.");
                    return;
                }

                serverClientsDir = Path.GetFullPath(serverClientsDir);

                if (!Directory.Exists(serverClientsDir))
                    return;

                string dest = Path.Combine(serverClientsDir, $"{clientId}.public.xml");

                if (File.Exists(clientPublic))
                {
                    File.Copy(clientPublic, dest, true);
                    Console.WriteLine($"[AUTO] Public key copied to server: {dest}");
                }
            }
            catch
            {
                // ne ruši klijenta ako ne uspe copy
            }
        }
    }
}
