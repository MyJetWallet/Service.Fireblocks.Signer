using System.Runtime.Serialization;

namespace Service.Fireblocks.Signer.Grpc.Models.Encryption
{
    [DataContract]
    public class EncryptionResponse
    {
        [DataMember(Order = 1)]
        public string EncryptedData { get; set; }
    }
}
