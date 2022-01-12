using Service.Fireblocks.Signer.Grpc.Models.Common;
using System.Runtime.Serialization;

namespace Service.Fireblocks.Signer.Grpc.Models.Addresses
{
    [DataContract]
    public class ValidateAddressResponse
    {
        [DataMember(Order = 1)]
        public string Address { get; set; }

        [DataMember(Order = 2)]
        public bool IsValid { get; set; }

        [DataMember(Order = 3)]
        public ErrorResponse Error { get; set; }
    }
}