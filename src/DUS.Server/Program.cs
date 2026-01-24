using System;
using System.Data.Entity;
using System.IO;
using System.ServiceModel;

namespace DUS.Server
{
    class Program
    {
        static void Main()
        {
            Database.SetInitializer(new CreateDatabaseIfNotExists<DusDbContext>());

            Directory.CreateDirectory("keys");
            Directory.CreateDirectory(Path.Combine("keys", "clients"));

            var svc = new TemperatureService();
            using (var host = new ServiceHost(svc))
            {
                host.Open();

                Console.WriteLine("Server up: net.tcp://localhost:9001/TemperatureService");
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
