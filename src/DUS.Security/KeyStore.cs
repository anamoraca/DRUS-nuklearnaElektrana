using System.IO;
using System.Security.Cryptography;

namespace DUS.Security
{
    public static class KeyStore
    {
        public static RSACryptoServiceProvider LoadOrCreateRsa(string path, bool includePrivate)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);

            var rsa = new RSACryptoServiceProvider(2048);
            if (File.Exists(path))
            {
                rsa.FromXmlString(File.ReadAllText(path));
                return rsa;
            }

            File.WriteAllText(path, rsa.ToXmlString(includePrivate));
            if (includePrivate)
            {
                var pubPath = Path.ChangeExtension(path, ".public.xml");
                File.WriteAllText(pubPath, rsa.ToXmlString(false));
            }
            return rsa;
        }

        public static RSACryptoServiceProvider LoadPublic(string path)
        {
            var rsa = new RSACryptoServiceProvider(2048);
            rsa.FromXmlString(File.ReadAllText(path));
            return rsa;
        }
    }
}
