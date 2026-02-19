using DUS.Contracts;
using DUS.Security;
using System;
using System.IO;
using System.ServiceModel;

namespace DUS.ReportClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var clientId = GetArg(args, "--clientId") ?? "REPORT";
            var fromMin = int.Parse(GetArg(args, "--fromMin") ?? "60");

            Directory.CreateDirectory("keys");

            // SECURITY: report klijent ima svoj RSA klju? (potpisuje poruku)
            var clientRsa = KeyStore.LoadOrCreateRsa(Path.Combine("keys", clientId + ".private.xml"), includePrivate: true);
            File.WriteAllText(Path.Combine("keys", clientId + ".public.xml"), clientRsa.ToXmlString(false));

            // SECURITY: server public key
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

            //  report pravi izveštaj iz baze u trenutku pokretanja
            var to = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var from = DateTimeOffset.UtcNow.AddMinutes(-fromMin).ToUnixTimeMilliseconds();

            long msgId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var rq = new ReportRequestPayload { FromUnixMs = from, ToUnixMs = to };
            var env = CryptoService.EncryptAndSign(clientId, msgId, MessageType.ReportRequest, rq, clientRsa, serverPub);


            var resp = proxy.GetAlarmReport(env);
            if (resp == null || !resp.Ok)
            {
                Console.WriteLine("Report error: " + (resp == null ? "null response" : resp.Error));
                return;
            }

            Console.WriteLine("ALARM REPORT (sorted by priority desc):");
            foreach (var it in resp.Items)
            {
                var old = Console.ForegroundColor;

                if (it.Priority == AlarmPriority.P1) Console.ForegroundColor = ConsoleColor.Yellow;
                else if (it.Priority == AlarmPriority.P2) Console.ForegroundColor = ConsoleColor.DarkYellow;
                else if (it.Priority == AlarmPriority.P3) Console.ForegroundColor = ConsoleColor.Red;

                var t = DateTimeOffset.FromUnixTimeMilliseconds(it.TimeUnixMs).LocalDateTime;
                Console.WriteLine("{0} client={1} sensor={2} value={3:F2} time={4}",
                    it.Priority, it.ClientId, it.SensorId, it.Value, t.ToString("HH:mm:ss"));

                Console.ForegroundColor = old;
            }

        }

        static string GetArg(string[] args, string key)
        {
            var idx = Array.IndexOf(args, key);
            if (idx >= 0 && idx + 1 < args.Length) return args[idx + 1];
            return null;
        }
    }
}
