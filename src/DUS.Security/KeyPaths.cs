using System;
using System.IO;

namespace DUS.Security
{
    public static class KeyPaths
    {
        public static string Root =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                         "DUS_Temperature", "keys");

        public static string ClientsDir => Path.Combine(Root, "clients");
        public static string ClientPrivateDir => Path.Combine(ClientsDir, "private");

        public static string ServerPrivate => Path.Combine(Root, "server.private.xml");
        public static string ServerPrivatePublic => Path.Combine(Root, "server.private.public.xml");

        // ovo je “standardno ime” koje klijenti čitaju:
        public static string ServerPublic => Path.Combine(Root, "server.public.xml");

        public static string ClientPrivate(string clientId) =>
            Path.Combine(ClientPrivateDir, $"{clientId}.private.xml");

        public static string ClientPublic(string clientId) =>
            Path.Combine(ClientsDir, $"{clientId}.public.xml");

        public static void EnsureDirs()
        {
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory(ClientsDir);
            Directory.CreateDirectory(ClientPrivateDir);
        }
    }
}
