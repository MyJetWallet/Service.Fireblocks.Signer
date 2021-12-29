using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Service.Fireblocks.Signer.Grpc.Models.Common
{
    [DataContract]
    public class ErrorResponse
    {
        [DataMember(Order = 1)]
        public ErrorCode ErrorCode { get; set; }

        [DataMember(Order = 2)]
        public string Message { get; set; }
    }
}
