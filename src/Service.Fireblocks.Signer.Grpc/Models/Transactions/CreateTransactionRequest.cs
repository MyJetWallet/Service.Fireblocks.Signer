using System.Runtime.Serialization;

namespace Service.Fireblocks.Signer.Grpc.Models.Transactions
{
    [DataContract]
    public class CreateTransactionRequest
    {
        [DataMember(Order = 1)]
        public string ExternalTransactionId { get; set; }

        [DataMember(Order = 2)]
        public decimal Amount { get; set; }

        [DataMember(Order = 3)]
        public string AssetSymbol { get; set; }

        [DataMember(Order = 4)]
        public string AssetNetwork { get; set; }

        [DataMember(Order = 5)]
        public string ToAddress { get; set; }

        [DataMember(Order = 6)]
        public string Tag { get; set; }

        /// <summary>
        /// Should fee be included in amount?
        /// </summary>
        [DataMember(Order = 7)]
        public bool TreatAsGrossAmount { get; set; }

        [DataMember(Order = 8)]
        public string DestinationVaultAccountId { get; set; }
    }
}