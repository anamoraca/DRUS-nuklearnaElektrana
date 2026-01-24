using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DUS.Contracts;
using Newtonsoft.Json;

namespace DUS.Security
{
    public static class CryptoService
    {
        public static SecureEnvelope EncryptAndSign<TPayload>(
            string clientId,
            long messageId,
            MessageType type,
            TPayload payload,
            RSACryptoServiceProvider clientPrivateKey,
            RSACryptoServiceProvider serverPublicKey)
        {
            var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var payloadJson = JsonConvert.SerializeObject(payload);
            var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.GenerateKey();
                aes.GenerateIV();

                byte[] ciphertext;
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(payloadBytes, 0, payloadBytes.Length);
                    cs.FlushFinalBlock();
                    ciphertext = ms.ToArray();
                }

                var encAesKey = serverPublicKey.Encrypt(aes.Key, false);

                var signedBytes = BuildSignedBytes(clientId, messageId, nowMs, type, encAesKey, aes.IV, ciphertext);
                var signature = clientPrivateKey.SignData(signedBytes, CryptoConfig.MapNameToOID("SHA256"));

                return new SecureEnvelope
                {
                    ClientId = clientId,
                    MessageId = messageId,
                    TimestampUnixMs = nowMs,
                    Type = type,
                    EncryptedAesKey = encAesKey,
                    AesIv = aes.IV,
                    Ciphertext = ciphertext,
                    Signature = signature
                };
            }
        }

        public static TPayload DecryptAndVerify<TPayload>(
            SecureEnvelope env,
            RSACryptoServiceProvider serverPrivateKey,
            RSACryptoServiceProvider clientPublicKey)
        {
            var signedBytes = BuildSignedBytes(env.ClientId, env.MessageId, env.TimestampUnixMs, env.Type,
                env.EncryptedAesKey, env.AesIv, env.Ciphertext);

            var ok = clientPublicKey.VerifyData(signedBytes, CryptoConfig.MapNameToOID("SHA256"), env.Signature);
            if (!ok) throw new CryptographicException("Bad signature");

            var aesKey = serverPrivateKey.Decrypt(env.EncryptedAesKey, false);

            using (var aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.IV = env.AesIv;

                byte[] plain;
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(env.Ciphertext, 0, env.Ciphertext.Length);
                    cs.FlushFinalBlock();
                    plain = ms.ToArray();
                }

                var json = Encoding.UTF8.GetString(plain);
                return JsonConvert.DeserializeObject<TPayload>(json);
            }
        }

        private static byte[] BuildSignedBytes(
            string clientId, long messageId, long ts, MessageType type,
            byte[] encKey, byte[] iv, byte[] ciphertext)
        {
            var meta = string.Format("{0}|{1}|{2}|{3}|", clientId, messageId, ts, (int)type);
            var metaBytes = Encoding.UTF8.GetBytes(meta);
            return metaBytes.Concat(encKey).Concat(iv).Concat(ciphertext).ToArray();
        }
    }
}
