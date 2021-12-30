using Autofac;
using Service.Fireblocks.Signer.Grpc;

// ReSharper disable UnusedMember.Global

namespace Service.Fireblocks.Signer.Client
{
    public static class AutofacHelper
    {
        public static void RegisterFireblocksSignerClient(this ContainerBuilder builder, string grpcServiceUrl)
        {
            var factory = new FireblocksSignerClientFactory(grpcServiceUrl);

            builder.RegisterInstance(factory.GetTransactionService()).As<ITransactionService>().SingleInstance();
        }
    }
}
