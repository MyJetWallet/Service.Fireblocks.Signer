using System;
using System.IO;
using System.Linq;
using System.Text.Unicode;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyJetWallet.ApiSecurityManager.AsymmetricEncryption;
using MyJetWallet.ApiSecurityManager.Autofac;
using MyJetWallet.ApiSecurityManager.TransactionSignature;
using ProtoBuf.Grpc.Client;
using Service.Fireblocks.Signer.Client;
using Service.Fireblocks.Signer.Grpc.Models;

namespace TestApp
{
    class Program
    {
        private static IContainer BuildContainer()
        {
            var serviceCollection = new ServiceCollection();

            var container = new ContainerBuilder();

            var loggerFactory =
                LoggerFactory.Create(builder =>
                    builder.AddSimpleConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.SingleLine = true;
                        options.TimestampFormat = "hh:mm:ss ";
                    }));

            serviceCollection.AddSingleton(loggerFactory);
            serviceCollection.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            container.Populate(serviceCollection);
            container.RegisterEncryptionServiceClient();

            var provider = container.Build();
            return provider;
        }
        static async Task Main(string[] args)
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            Console.Write("Press enter to start");
            Console.ReadLine();


            var container = BuildContainer();
            var securityFactory = new ApiSecurityManagerClientFactory("http://localhost:5001");//("http://192.168.70.21:80");//;

            var apiKeyService = securityFactory.GetApiKeyService();
            var asymmetricService = container.Resolve<IAsymmetricEncryptionService>();
            var apikids = await apiKeyService.GetApiKeyIdsAsync(new MyJetWallet.ApiSecurityManager.Grpc.Models.GetApiKeyIdsRequest { });

            var factory = new FireblocksSignerClientFactory("http://localhost:5001");
            //var factory = new FireblocksSignerClientFactory("http://fireblocks-signer.spot-services.svc.cluster.local");
            var client = factory.GetTransactionService();

            {
                var publicKey = await File.ReadAllTextAsync(@"C:\Git\fireblocks-uat\fireblocks_api_key");
                var privateKey = await File.ReadAllTextAsync(@"C:\Git\fireblocks-uat\fireblocks_secret.key");

                await apiKeyService.SetApiKeysAsync(new MyJetWallet.ApiSecurityManager.Grpc.Models.SetApiKeyRequest()
                {
                    ApiKey = publicKey,
                    ApiKeyId = "fireblocks-api-key-id",
                    EncryptionKeyId = "fireblocks-signer",
                    PrivateKey = privateKey,
                });
            }

            {

                var publicKey = await File.ReadAllTextAsync(@"C:\Git\rsa-test\public-key.pem");
                var privateKey = await File.ReadAllTextAsync(@"C:\Git\rsa-test\private-key.pem");

                await apiKeyService.SetApiKeysAsync(new MyJetWallet.ApiSecurityManager.Grpc.Models.SetApiKeyRequest()
                {
                    ApiKey = "any",
                    ApiKeyId = "transaction-signature-public-key",
                    EncryptionKeyId = "fireblocks-signer",
                    PrivateKey = publicKey,
                });

                var issuedAt = DateTime.UtcNow;
                var signatureComponents = TransactionSignatureComponents.Create(
                            0.01m,
                            "ETH",
                            "fireblocks-eth-test",
                            "test",
                            issuedAt,
                            "0x83ceAC6A4b7060348d8Ebf4996817962Db7e3758",
                            "");

                var signatureContent = signatureComponents.GetSignatureContent();

                Console.WriteLine(signatureContent);

                var signature = asymmetricService.Sign(signatureContent, AsymmetricEncryptionUtils.ReadPrivateKeyFromPem(privateKey));

                var tx = await client.CreateTransactionAsync(new Service.Fireblocks.Signer.Grpc.Models.Transactions.CreateTransactionRequest
                {
                    Amount = 0.01m,
                    AssetSymbol = "ETH",
                    AssetNetwork = "fireblocks-eth-test",
                    ExternalTransactionId = Guid.NewGuid().ToString(),
                    Tag = "",
                    ToAddress = "0x83ceAC6A4b7060348d8Ebf4996817962Db7e3758",
                    //DestinationVaultAccountId = "16",
                    FromVaultAccountId = "11",
                    AmountWithFee = 0.01m,
                    ClientId = "test",
                    IssuedAt = issuedAt,
                    Signature = signature,
                TreatAsGrossAmount = true,
                });
            }

            
            //var encryption = factory.GetEncryptionService();

            ////var publicKey = await File.ReadAllTextAsync(@"D:\fireblocks uat\fireblocks_api_key");
            ////var privateKey = await File.ReadAllTextAsync(@"D:\fireblocks uat\fireblocks_secret.key");

            //var publicKey = args[0];
            //var privateKey = await File.ReadAllTextAsync(@"/Users/acidworx/cosigner-prod/fireblocks_secret_signer.key");

            //var x = await encryption.SetApiKeysAsync(new()
            //{
            //    ApiKey = publicKey,
            //    PrivateKey = privateKey
            //});

            //Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(x));

            
            //var resp = await client.GetVaultAccountAsync(new()
            //{
            //    VaultAccountId = "3"
            //});
            //Console.WriteLine(resp?.VaultAccount?.FirstOrDefault().Id);

            Console.WriteLine("End");
            Console.ReadLine();
        }
    }
}
