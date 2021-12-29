using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;
using Service.Fireblocks.Signer.Grpc;

namespace Service.Fireblocks.Signer.Client
{
    [UsedImplicitly]
    public class FireblocksApiClientFactory: MyGrpcClientFactory
    {
        public FireblocksApiClientFactory(string grpcServiceUrl) : base(grpcServiceUrl)
        {
        }

        public ITransactionService GetVaultAccountService() => CreateGrpcService<ITransactionService>();

        public IEncryptionService GetEncryptionService() => CreateGrpcService<IEncryptionService>();
    }
}
