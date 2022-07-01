using Service.Fireblocks.Signer.Grpc.Models.Common;
using System.Runtime.Serialization;

namespace Service.Fireblocks.Signer.Grpc.Models.Transactions
{
    [DataContract]
    public class EstimateTransactionResponse
    {
        [DataMember(Order = 1)]
        public ErrorResponse Error { get; set; }

        [DataMember(Order = 2)]
        public EstimatedFee Low { get; set; }

        [DataMember(Order = 3)]
        public EstimatedFee Medium { get; set; }

        [DataMember(Order = 4)]
        public EstimatedFee High { get; set; }
    }
}