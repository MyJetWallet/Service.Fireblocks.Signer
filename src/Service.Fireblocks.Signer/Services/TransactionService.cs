using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Fireblocks.Client;
using MyJetWallet.Fireblocks.Domain.Models.Addresses;
using MyNoSqlServer.Abstractions;
using Service.Blockchain.Wallets.MyNoSql.AssetsMappings;
using Service.Fireblocks.Signer.Grpc;
using Service.Fireblocks.Signer.Grpc.Models.Transactions;

namespace Service.Fireblocks.Signer.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ILogger<TransactionService> _logger;
        private readonly ITransactionsClient _transactionsClient;
        private readonly IClient _client;
        private readonly IMyNoSqlServerDataReader<AssetMappingNoSql> _assetMappings;

        public TransactionService(ILogger<TransactionService> logger,
            ITransactionsClient transactionsClient,
            IClient client,
            IMyNoSqlServerDataReader<AssetMappingNoSql> assetMappings)
        {
            _logger = logger;
            this._transactionsClient = transactionsClient;
            this._client = client;
            this._assetMappings = assetMappings;
        }

        public async Task<Grpc.Models.Transactions.CreateTransactionResponse> CreateTransactionAsync(CreateTransactionRequest request)
        {
            try
            {
                var idempotencyKey = $"transaction_{request.ExternalTransactionId}";
                idempotencyKey = idempotencyKey.Substring(0, Math.Min(40, idempotencyKey.Length));

                var assetMapping = _assetMappings.Get(AssetMappingNoSql.GeneratePartitionKey(request.AssetSymbol), 
                    AssetMappingNoSql.GenerateRowKey(request.AssetNetwork));

                var response = await _client.TransactionsPostAsync(idempotencyKey, new TransactionRequest
                {
                    Amount = request.Amount,
                    AssetId = assetMapping.AssetMapping.FireblocksAssetId,
                    Source = new TransferPeerPath
                    {
                        Id = assetMapping.AssetMapping.WithdrawalVaultAccountId,
                        Type = TransferPeerPathType.VAULT_ACCOUNT
                    },
                    ExternalTxId = request.ExternalTransactionId,
                    Destination = new DestinationTransferPeerPath
                    {
                        OneTimeAddress = new OneTimeAddress
                        {
                            Address = request.ToAddress,
                            Tag = request.Tag,
                        }
                    },
                    TreatAsGrossAmount = false,
                    FailOnLowFee = false,
                    FeeLevel = TransactionRequestFeeLevel.MEDIUM,
                    Operation = TransactionOperation.TRANSFER,
                });

                return new ()
                {
                };

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating VaultAccount @{context}", request);

                return new ()
                {
                    Error = new Grpc.Models.Common.ErrorResponse
                    {
                        ErrorCode = Grpc.Models.Common.ErrorCode.Unknown,
                        Message = e.Message
                    }
                };
            }
        }
    }
}
