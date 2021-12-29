using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Fireblocks.Client.Auth;
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
        private readonly KeyActivator _keyActivator;
        private readonly SymmetricEncryptionService _symmetricEncryptionService;

        public ApplicationLifetimeManager(
            IHostApplicationLifetime appLifetime, 
            ILogger<ApplicationLifetimeManager> logger,
            IMyNoSqlServerDataWriter<FireblocksApiKeysNoSql> myNoSqlServerData,
            KeyActivator keyActivator,
            SymmetricEncryptionService symmetricEncryptionService)
            : base(appLifetime)
        {
            _logger = logger;
            this._myNoSqlServerData = myNoSqlServerData;
            this._keyActivator = keyActivator;
            this._symmetricEncryptionService = symmetricEncryptionService;
        }

        protected override void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");
            var key = _myNoSqlServerData.GetAsync(FireblocksApiKeysNoSql.GeneratePartitionKey(), FireblocksApiKeysNoSql.GenerateRowKey()).Result;

            if (key != null)
            {
                try
                {
                    var apiKey = _symmetricEncryptionService.Decrypt(key.ApiKey);
                    var privateKey = _symmetricEncryptionService.Decrypt(key.PrivateKey);
                    _keyActivator.ActivateKeys(apiKey, privateKey);
                }
                catch (System.Exception e)
                {
                    _logger.LogError(e, "PLS< SET UP KEYS FOR API");
                }
            }
        }

        protected override void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");
        }

        protected override void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");
        }
    }
}
