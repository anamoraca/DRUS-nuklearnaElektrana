using System;
using System.Runtime.Serialization;

namespace DUS.Contracts
{
    [DataContract]
    public class SecureEnvelope
    {
        [DataMember] public string ClientId { get; set; }
        [DataMember] public long MessageId { get; set; }
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
