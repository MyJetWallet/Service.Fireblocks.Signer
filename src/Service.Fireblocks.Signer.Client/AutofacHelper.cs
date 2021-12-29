using Autofac;
using Service.Fireblocks.Signer.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.Fireblocks.Signer.Client
{
    public static class AutofacHelper
    {
        public static void RegisterFireblocksApiClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new FireblocksApiClientFactory(grpcServiceUrl);

            builder.RegisterInstance(factory.GetVaultAccountService()).As<ITransactionService>().SingleInstance();
        }
    }
}
