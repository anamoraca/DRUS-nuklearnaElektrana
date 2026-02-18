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

            // 1) Centralni key store (nema lokalnog .\keys)
            KeyPaths.EnsureDirs();

            // 2) Klijent napravi svoje klju?eve u shared store
            var clientRsa = KeyStore.LoadOrCreateRsa(KeyPaths.ClientPrivate(clientId), includePrivate: true);
            File.WriteAllText(KeyPaths.ClientPublic(clientId), clientRsa.ToXmlString(false));

            // 3) Klijent ?ita server public key iz shared store
            var serverPubPath = KeyPaths.ServerPublic;
            if (!File.Exists(serverPubPath))
            {
                Console.WriteLine("Missing server public key: " + serverPubPath);
                Console.WriteLine("Start SERVER once to generate it.");
                Console.ReadLine();
                return;
            }
            var serverPub = KeyStore.LoadPublic(serverPubPath);

            var factory = new ChannelFactory<ITemperatureService>("TemperatureServiceEndpoint");
            var proxy = factory.CreateChannel();

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
                var t = DateTimeOffset.FromUnixTimeMilliseconds(it.TimeUnixMs).LocalDateTime;
                Console.WriteLine("{0} sensor={1} value={2:F2} time={3}",
                    it.Priority, it.SensorId, it.Value, t.ToString("HH:mm:ss"));
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
