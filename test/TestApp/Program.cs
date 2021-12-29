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


            var factory = new FireblocksApiClientFactory("http://localhost:5001");
            var client = factory.GetVaultAccountService();
            var encryption = factory.GetEncryptionService();

            var publicKey = await File.ReadAllTextAsync(@"D:\fireblocks uat\fireblocks_api_key");
            var privateKey = await File.ReadAllTextAsync(@"D:\fireblocks uatfireblocks_secret.key");

            var x = await encryption.SetApiKeysAsync(new ()
            {
                ApiKey = publicKey,
                PrivateKey = privateKey 
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
