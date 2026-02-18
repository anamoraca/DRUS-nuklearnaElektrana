using DUS.Security;
using System;
using System.Data.Entity;
using System.IO;
using System.ServiceModel;

namespace DUS.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            // 1) Samo generiši klju?eve pa iza?i (za build automatizaciju)
            if (args != null && args.Length > 0 && args[0] == "--genKeysOnly")
            {
                DUS.Security.KeyPaths.EnsureDirs();
                Directory.CreateDirectory(Path.Combine("keys", "clients"));
                KeyStore.LoadOrCreateRsa(@"keys\server.private.xml", includePrivate: true);
                return;
            }

            // 2) Normalan server start
            Database.SetInitializer(new CreateDatabaseIfNotExists<DusDbContext>());

            Directory.CreateDirectory("keys");
            Directory.CreateDirectory(Path.Combine("keys", "clients"));

            var svc = new TemperatureService();
            using (var host = new ServiceHost(svc))
            {
                host.Open();

                Console.WriteLine("Key store: " + DUS.Security.KeyPaths.Root);
                Console.WriteLine("Keys:");
                Console.WriteLine("- server private: keys\\server.private.xml");
                Console.WriteLine("- server public : keys\\server.private.public.xml");
                Console.WriteLine("- client pubkeys: keys\\clients\\{clientId}.public.xml");
                Console.WriteLine();
                Console.WriteLine("Press ENTER to stop.");
                Console.ReadLine();

                host.Close();
            }
        }

    }
}
