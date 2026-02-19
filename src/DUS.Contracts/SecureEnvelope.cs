using System;
using System.Runtime.Serialization;

namespace DUS.Contracts
{
    // SECURITY: SecureEnvelope (šifrovanje + potpis + anti-replay meta)
    [DataContract]
    public class SecureEnvelope
    {
        [DataMember] public string ClientId { get; set; }

        //  (pouzdana komunikacija): ID poruke raste posle svake poruke
        [DataMember] public long MessageId { get; set; }

        //  timestamp uz poruku (anti-fabrikacija)
        [DataMember] public long TimestampUnixMs { get; set; }

        [DataMember] public MessageType Type { get; set; }

        [DataMember] public byte[] EncryptedAesKey { get; set; }
        [DataMember] public byte[] AesIv { get; set; }
        [DataMember] public byte[] Ciphertext { get; set; }

        [DataMember] public byte[] Signature { get; set; }

        public SecureEnvelope()
        {
            ClientId = "";
            EncryptedAesKey = Array.Empty<byte>();
            AesIv = Array.Empty<byte>();
            Ciphertext = Array.Empty<byte>();
            Signature = Array.Empty<byte>();
        }
    }
}
