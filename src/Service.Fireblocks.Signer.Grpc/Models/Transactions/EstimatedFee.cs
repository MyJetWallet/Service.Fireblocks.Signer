using System.Runtime.Serialization;

namespace Service.Fireblocks.Signer.Grpc.Models.Transactions
{
    [DataContract]
    public class EstimatedFee
    {
        [DataMember(Order =1 )]
        public decimal GasLimit { get; set; }

        [DataMember(Order = 2)]
        public decimal GasPrice { get; set; }
    }
}