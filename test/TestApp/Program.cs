using System;
using System.IO;
using System.Linq;
using System.Text.Unicode;
using System.Threading.Tasks;
using ProtoBuf.Grpc.Client;
using Service.Fireblocks.Signer.Client;
using Service.Fireblocks.Signer.Grpc.Models;

namespace TestApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            GrpcClientFactory.AllowUnencryptedHttp2 = true;

            Console.Write("Press enter to start");
            Console.ReadLine();


            //var factory = new FireblocksSignerClientFactory("http://localhost:5001");
            var factory = new FireblocksSignerClientFactory("http://fireblocks-signer.spot-services.svc.cluster.local");
            var client = factory.GetTransactionService();
            var encryption = factory.GetEncryptionService();

            //var publicKey = await File.ReadAllTextAsync(@"D:\fireblocks uat\fireblocks_api_key");
            //var privateKey = await File.ReadAllTextAsync(@"D:\fireblocks uat\fireblocks_secret.key");

            //var x = await encryption.SetApiKeysAsync(new ()
            //{
            //    ApiKey = publicKey,
            //    PrivateKey = privateKey 
            //});

            var tx = await client.CreateTransactionAsync(new Service.Fireblocks.Signer.Grpc.Models.Transactions.CreateTransactionRequest
            {
                Amount = 0.01m,
                AssetNetwork = "ETH",
                AssetSymbol = "fireblocks-eth-test",
                ExternalTransactionId = "test1",
                Tag = "",
                ToAddress = "0x1Eab7d412a25a5d00Ec3d04648aa54CeA4aB7e94"
            });
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
