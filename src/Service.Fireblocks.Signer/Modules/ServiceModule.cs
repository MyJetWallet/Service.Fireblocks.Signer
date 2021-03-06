using Autofac;
using Autofac.Core;
using Autofac.Core.Registration;
using MyJetWallet.Fireblocks.Client.Autofac;
using Service.Fireblocks.Signer.Services;
using System.IO;
using MyJetWallet.Sdk.NoSql;
using Service.Fireblocks.Signer.NoSql;
using MyJetWallet.Fireblocks.Client.DelegateHandlers;
using Microsoft.Extensions.Logging;
using Service.Blockchain.Wallets.MyNoSql.AssetsMappings;
using MyJetWallet.ApiSecurityManager.Autofac;

namespace Service.Fireblocks.Signer.Modules
{
    public class ServiceModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var myNoSqlClient = builder.CreateNoSqlClient(Program.Settings.MyNoSqlReaderHostPort, Program.LogFactory);
            var logger = Program.LogFactory.CreateLogger<LoggerMiddleware>();

            builder.RegisterFireblocksClient(new MyJetWallet.Fireblocks.Client.ClientConfigurator()
            {
                //ApiKey = ,
                //ApiPrivateKey = ,
                BaseUrl = Program.Settings.FireblocksBaseUrl,
            }, new LoggerMiddleware(logger));

            builder.RegisterMyNoSqlWriter<FireblocksApiKeysNoSql>(() => Program.Settings.MyNoSqlWriterUrl, FireblocksApiKeysNoSql.TableName);

            builder.RegisterMyNoSqlReader<AssetMappingNoSql>(myNoSqlClient, AssetMappingNoSql.TableName);

            builder.RegisterEncryptionServiceClient();
        }
    }
}