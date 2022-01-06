using MyJetWallet.Fireblocks.Domain.Models.VaultAccounts;
using Service.Fireblocks.Signer.Grpc.Models.Common;
using System.Runtime.Serialization;

namespace Service.Fireblocks.Signer.Grpc.Models.Transactions
{
    [DataContract]
    public class CreateTransactionResponse
    {
        [DataMember(Order = 1)]
        public ErrorResponse Error { get; set; }

        [DataMember(Order = 2)]
        public string FireblocksTxId { get; set; }
    }
}