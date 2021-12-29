using JetBrains.Annotations;
using MyJetWallet.Sdk.Grpc;
using Service.Fireblocks.Signer.Grpc;

namespace Service.Fireblocks.Signer.Client
{
    [UsedImplicitly]
    public class FireblocksSignerClientFactory: MyGrpcClientFactory
    {
        public FireblocksSignerClientFactory(string grpcServiceUrl) : base(grpcServiceUrl)
        {
        }

        public ITransactionService GetTransactionService() => CreateGrpcService<ITransactionService>();

        public IEncryptionService GetEncryptionService() => CreateGrpcService<IEncryptionService>();
    }
}
