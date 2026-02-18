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
        static long _msgId = 0;
        static ClientRole _role = ClientRole.Standby;

        static void Main(string[] args)
        {
            var clientId = GetArg(args, "--clientId") ?? ("C" + new Random().Next(100, 999));
            var sensorId = int.Parse(GetArg(args, "--sensorId") ?? "1");

            var min = double.Parse(GetArg(args, "--min") ?? "10");
            var max = double.Parse(GetArg(args, "--max") ?? "100");

            var a1 = double.Parse(GetArg(args, "--a1") ?? "60");
            var a2 = double.Parse(GetArg(args, "--a2") ?? "75");
            var a3 = double.Parse(GetArg(args, "--a3") ?? "90");

            var qStr = GetArg(args, "--q") ?? "GOOD";
            DataQuality q;
            if (!Enum.TryParse(qStr, out q)) q = DataQuality.GOOD;

            KeyPaths.EnsureDirs();

            // Client keys (privatni u private folderu)
            var clientRsa = KeyStore.LoadOrCreateRsa(KeyPaths.ClientPrivate(clientId), includePrivate: true);

            // Public klju? u shared clients folder (da server vidi)
            File.WriteAllText(KeyPaths.ClientPublic(clientId), clientRsa.ToXmlString(false));

            // Server public key u shared root
            var serverPubPath = KeyPaths.ServerPublic;
            if (!File.Exists(serverPubPath))
            {
                Console.WriteLine("Missing server public key: " + serverPubPath);
                Console.WriteLine("Start SERVER once (on first run) to generate it.");
                Console.ReadLine();
                return;
            }
            var serverPub = KeyStore.LoadPublic(serverPubPath);


            var factory = new ChannelFactory<ITemperatureService>("TemperatureServiceEndpoint");
            var proxy = factory.CreateChannel();

            Console.WriteLine("Client {0} sensor={1}", clientId, sensorId);
            Console.WriteLine("Commands: type 'sleep' or 'crash' then ENTER");
            Console.WriteLine("NOTE: Copy keys\\{0}.public.xml to Server\bin\\...\\keys\\clients\\{0}.public.xml", clientId);
            Console.WriteLine();

            // Heartbeat every 5s
            var hbTimer = new Timer(_ =>
            {
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
                    if (resp != null && resp.Ok)
                    {
                        if (resp.Role != _role)
                        {
                            _role = resp.Role;
                            Console.WriteLine("ROLE -> " + _role);
                        }
                    }
                }
                catch { }
            }, null, 0, 5000);

            var rnd = new Random();

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var cmd = Console.ReadLine();
                    if (cmd == "crash") Environment.Exit(0);
                    if (cmd == "sleep") Thread.Sleep(20000);
                }

                if (_role == ClientRole.Active)
                {
                    Thread.Sleep(rnd.Next(1000, 10001));

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
            if (p == AlarmPriority.P1) Console.ForegroundColor = ConsoleColor.Yellow;
            else if (p == AlarmPriority.P2) Console.ForegroundColor = ConsoleColor.DarkYellow;
            else if (p == AlarmPriority.P3) Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine("Temperature is {0:F2} at {1}", v, DateTime.Now.ToString("HH:mm:ss"));

            Console.ForegroundColor = old;
        }

        static string GetArg(string[] args, string key)
        {
            var idx = Array.IndexOf(args, key);
            if (idx >= 0 && idx + 1 < args.Length) return args[idx + 1];
            return null;
        }
    }
}
