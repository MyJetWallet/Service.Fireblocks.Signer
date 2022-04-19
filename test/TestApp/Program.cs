﻿using System;
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
                    EncryptionKeyId = "encryption-key-id",
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
                    EncryptionKeyId = "encryption-key-id",
                    PrivateKey = publicKey,
                });

                /*
                  "{\"ExternalTransactionId\":\"fire_tx_812_59\",\"Amount\":-0.00041,\"AssetSymbol\":\"BTC\",\"AssetNetwork\":\"fireblocks-btc-test\",\"ToAddress\":\"2N3oefVeg6stiTb5Kh3ozCSkaqmx91FDbsm\",\"Tag\":null,\"TreatAsGrossAmount\":false,\"DestinationVaultAccountId\":null,\"FromVaultAccountId\":null,\"ForceSweep\":false,\"Signature\":\"T7Ng8TgnFx1sKDfeWCQ5FMwszQKfek2V5Men0gc9t5397PtStNYv7LTU65FTPBJ0OAGHfpiQ6yUKFjX2oeJVCGu8tH6TM6c9RZS7SyNKSazvNZC2CaS0UDuyP0KmfqB2Bzu834Wv/C9A4Rd60U9X8bp3AhjwzEkz0J5GFFUaeFmvCdhQcCUSDuHMeNrAIAdXxENOILH7zPVpckSckiz2tWrzTbA0jeluqwM9PM3sFwdE7LC2nmHfyAz78ceyJzZb/uYXfFyDnVkffwTpDFKagBiob/BFaBmz0rX7wUTINbM9qXUpPFyUppDzNlRYTTjMTPzLrptXzdJ1mJHO9qduupdMD3fmQZ0UBbdKyo4Wh/ntSrg6b/5QcF03rWiHztXxH0qyyd2gUQqHz7b0HL6eND/6y6x52zAreLj/KWOVb0ssxziwkCQCCjACI5HmOTKUDXGdAj3lCgOeNeoJPwQUuWRz2v1rM8luaL2Pg3ren4lcjf3CyWeIxhWeb8r5G/KFbo8IJlDgQolYusxaLHeaRNnkiI1mprBNivjLraeBYkSWt4uBHZc7yyuKoaOWUXWchRui5NIuxjtpvxpt4zBJVBQ56UWGyOlk0zGszRuZTZseccy4vAQIeHbu/ta2oS60RYLTxqbjtCKlXHUB88X5TjRNllBVYM0E=\",\"ClientId\":\"9ddecae3dba14861a934551b0bfa9e33\",\"IssuedAt\":\"2022-04-19T09:31:26.91699\",\"AmountWithFee\":0.0001}"
                 */

                var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var signatureComponents = TransactionSignatureComponents.Create(
                            0.005m,
                            "BTC",
                            "fireblocks-btc-test",
                            "9ddecae3dba14861a934551b0bfa9e33",
                            issuedAt,
                            "2MtiUTjVGZ3fgj4ZuE63E7i31vP5HboMibx");

                var signatureContent = signatureComponents.GetSignatureContent();
                var signatureOld = "a+tGq8diwdBNOfEfyMSnQoqXr9mspeTws0llOubucTfq3KiojijzdVEHnYssLEXGAZQ4oqfdaKywnZBz+ccQbpSfVbOwHtQTmRa30HUrZAC+xjX/QZ0KsDqj3tPwts+JqEi3HmacnguRs2PE8PyK3GNhMC0zTEfo9hxPMybKr86qy5gxHJWCUA9BkjPRSfOFqog6+zHzY1WE2/oI+n3QoHpueCDdfDP93oVcKf7hLTyw7v3YfW4hyae2UX4U6UANMd9wCBbBiZlLJYfGC+KGZbUVbW2J3yF712WkBn7U5wr0/WtUXAuToe2KPJt2cp5t4DbAJCsDqgsgfa9Dgk9vuqxG767AXc1JW1P3lV0Wkm5dF2FvVeACHXobjVrhcLfL4mFaONggNXzANca/yjH8GHnbyWIHPOpXSHpOO4X4bKmtILrm3ZbaIeSkEuo/mH/yhoM4e5jy7CcsuWoYFnWghzCAC0IwR6oeQn0fGzJomxmEAnFXIg0ZYnEvD1O/LeVIzhriZT2AVLAbdIANy3BaII4xpOUxLetbar2DvDqlvGA+VoK6gXRnwM6rOKgm9OE1ecxpiHPvNbsU+MGmwWKWAgrTlPJnuAYEk5wCUHIjALHW734XhuXg/2alBJh8/JlQRjdh7i9ArExS+25Pc4QVnk5efrPUbpjmNdP1DhY3bIA=";

                Console.WriteLine(signatureContent);

                var signature = asymmetricService.Sign(signatureContent, AsymmetricEncryptionUtils.ReadPrivateKeyFromPem(privateKey));

                {
                    var expSignContent = "{\"amount\":0.005,\"assetSymbol\":\"BTC\",\"assetNetwork\":\"fireblocks-btc-test\",\"toAddress\":\"2MtiUTjVGZ3fgj4ZuE63E7i31vP5HboMibx\",\"clientId\":\"9ddecae3dba14861a934551b0bfa9e33\",\"issuedAt\":\"2022-04-19T11:48:28.9243665Z\"}";

                    var expSignature = asymmetricService.Sign(expSignContent, AsymmetricEncryptionUtils.ReadPrivateKeyFromPem(privateKey));

                    Console.WriteLine(expSignature);
                }

                var tx = await client.CreateTransactionAsync(new Service.Fireblocks.Signer.Grpc.Models.Transactions.CreateTransactionRequest
                {
                    Amount = signatureComponents.Amount,
                    AssetSymbol = signatureComponents.AssetSymbol,
                    AssetNetwork = signatureComponents.AssetNetwork,
                    ExternalTransactionId = Guid.NewGuid().ToString(),
                    Tag = signatureComponents.Tag,
                    ToAddress = signatureComponents.ToAddress,
                    //DestinationVaultAccountId = "16",
                    //FromVaultAccountId = "11",
                    AmountWithFee = signatureComponents.Amount,
                    ClientId = signatureComponents.ClientId,
                    IssuedAtUnixTime = issuedAt,
                    Signature = signatureOld,
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
