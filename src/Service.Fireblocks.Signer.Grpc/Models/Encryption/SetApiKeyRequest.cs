using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Service.Fireblocks.Signer.Grpc.Models.Encryption
{
    [DataContract]
    public class SetApiKeyRequest
    {
        [DataMember(Order = 1)]
        public string PrivateKey { get; set; }

        [DataMember(Order = 2)]
        public string ApiKey { get; set; }
    }

    [DataContract]
    public class SetApiKeyResponse
    {
    }
}
