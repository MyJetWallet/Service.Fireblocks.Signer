using System;
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

        [DataMember(Order = 9)]
        public string FromVaultAccountId { get; set; }

        /// <summary>
        /// For Polkadot only
        /// </summary>
        [DataMember(Order = 10)]
        public bool ForceSweep { get; set; }

        [DataMember(Order = 11)]
        public string Signature { get; set; }

        [DataMember(Order = 12)]
        public string ClientId { get; set; }

        [Obsolete]
        [DataMember(Order = 13)]
        public DateTime IssuedAt { get; set; }

        /// <summary>
        /// For Signature Only
        /// </summary>
        [DataMember(Order = 14)]
        public decimal AmountWithFee { get; set; }

        [DataMember(Order = 15)]
        public long IssuedAtUnixTime { get; set; }
    }

    [DataContract]
    public class FeeSettings
    {
        public enum FeeLevel
        {
            Low,
            Medium,
            High
        }

        [DataMember(Order = 1)]
        public FeeLevel? Level { get; set; }

    }
}