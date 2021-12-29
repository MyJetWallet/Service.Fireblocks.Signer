using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Service.Fireblocks.Signer.Grpc.Models.Encryption
{
    [DataContract]
    public class EncryptionRequest
    {
        [DataMember(Order = 1)]
        public string Data { get; set; }

    }
}
