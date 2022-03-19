using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.ApiSecurityManager.Notifications;
using MyJetWallet.Fireblocks.Client.Auth;
using MyJetWallet.Sdk.NoSql;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.Abstractions;
using Service.Fireblocks.Signer.NoSql;
using Service.Fireblocks.Signer.Services;

namespace Service.Fireblocks.Signer
{
    public class ApplicationLifetimeManager : ApplicationLifetimeManagerBase
    {
        private readonly ILogger<ApplicationLifetimeManager> _logger;
        private readonly IMyNoSqlServerDataWriter<FireblocksApiKeysNoSql> _myNoSqlServerData;
        private readonly INotificatorSubscriber _notificatorSubscriber;
        private readonly KeyActivator _keyActivator;
        private readonly SymmetricEncryptionService _symmetricEncryptionService;
        private readonly MyNoSqlClientLifeTime _myNoSqlClient;

        public ApplicationLifetimeManager(
            IHostApplicationLifetime appLifetime,
            ILogger<ApplicationLifetimeManager> logger,
            IMyNoSqlServerDataWriter<FireblocksApiKeysNoSql> myNoSqlServerData,
            INotificatorSubscriber notificatorSubscriber,
            KeyActivator keyActivator,
            SymmetricEncryptionService symmetricEncryptionService,
            MyNoSqlClientLifeTime myNoSqlClient)
            : base(appLifetime)
        {
            _logger = logger;
            _myNoSqlServerData = myNoSqlServerData;
            _notificatorSubscriber = notificatorSubscriber;
            _keyActivator = keyActivator;
            _symmetricEncryptionService = symmetricEncryptionService;
            _myNoSqlClient = myNoSqlClient;
        }

        protected override void OnStarted()
        {
            _notificatorSubscriber.Subscribe((key) =>
            {
                if (key.Id == Program.Settings.ApiKeyId)
                    try
                    {
                        _logger.LogInformation("Activating keys");
                        _keyActivator.ActivateKeys(key.ApiKeyValue, key.PrivateKeyValue);
                    }
                    catch (System.Exception e)
                    {
                        _logger.LogError(e, "PLS< SET UP KEYS FOR API");
                    }
            });

            _logger.LogInformation("OnStarted has been called.");
            _myNoSqlClient.Start();

            //var key = _myNoSqlServerData.GetAsync(FireblocksApiKeysNoSql.GeneratePartitionKey(), FireblocksApiKeysNoSql.GenerateRowKey()).Result;

            //if (key != null)
            //{
            //    try
            //    {
            //        var apiKey = _symmetricEncryptionService.Decrypt(key.ApiKey);
            //        var privateKey = _symmetricEncryptionService.Decrypt(key.PrivateKey);
            //        _keyActivator.ActivateKeys(apiKey, privateKey);
            //    }
            //    catch (System.Exception e)
            //    {
            //        _logger.LogError(e, "PLS< SET UP KEYS FOR API");
            //    }
            //}
        }

        protected override void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");
            _myNoSqlClient.Stop();
        }

        protected override void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");
        }
    }
}
