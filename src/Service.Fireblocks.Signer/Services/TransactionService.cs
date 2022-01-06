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
        private readonly IClient _client;
        private readonly IVaultClient _vaultClient;
        private readonly IMyNoSqlServerDataReader<AssetMappingNoSql> _assetMappings;

        public TransactionService(ILogger<TransactionService> logger,
            IClient client,
            IVaultClient vaultClient,
            IMyNoSqlServerDataReader<AssetMappingNoSql> assetMappings)
        {
            _logger = logger;
            _client = client;
            _vaultClient = vaultClient;
            _assetMappings = assetMappings;
        }

        public async Task<Grpc.Models.Transactions.CreateTransactionResponse> CreateTransactionAsync(CreateTransactionRequest request)
        {
            try
            {
                var assetMapping = _assetMappings.Get(AssetMappingNoSql.GeneratePartitionKey(request.AssetSymbol),
                                                      AssetMappingNoSql.GenerateRowKey(request.AssetNetwork));

                var vaultAcc = await _vaultClient.AccountsGetAsync(assetMapping.AssetMapping.WithdrawalVaultAccountId, default);

                if (vaultAcc.StatusCode != 200)
                {
                    _logger.LogError("Fireblocks signer can't execute http request: {@context}", request);

                    return new Grpc.Models.Transactions.CreateTransactionResponse
                    {
                        Error = new Grpc.Models.Common.ErrorResponse
                        {
                            ErrorCode = Grpc.Models.Common.ErrorCode.ApiError,
                        }
                    };
                }

                if (vaultAcc == null)
                {
                    return new Grpc.Models.Transactions.CreateTransactionResponse
                    {
                        Error = new Grpc.Models.Common.ErrorResponse
                        {
                            ErrorCode = Grpc.Models.Common.ErrorCode.ApiError,
                        }
                    };
                }

                var balance = vaultAcc.Result.Assets.FirstOrDefault(x => x.Id == assetMapping.AssetMapping.FireblocksAssetId);

                if (balance == null || !decimal.TryParse(balance.Available, out var availableBalance) || availableBalance <= request.Amount)
                {
                    return new Grpc.Models.Transactions.CreateTransactionResponse
                    {
                        Error = new Grpc.Models.Common.ErrorResponse
                        {
                            ErrorCode = Grpc.Models.Common.ErrorCode.NotEnoughBalance,
                            Message = $"Not enough balance ASSET: {assetMapping.AssetMapping.FireblocksAssetId}; VAULT ACCOUNT: {assetMapping.AssetMapping.WithdrawalVaultAccountId}"
                        }
                    };
                }

                var idempotencyKey = $"transaction_{request.ExternalTransactionId}";
                idempotencyKey = idempotencyKey.Substring(0, Math.Min(40, idempotencyKey.Length));

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
                        Type = TransferPeerPathType.ONE_TIME_ADDRESS,
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

                return new()
                {
                    FireblocksTxId = response.Result.Id
                };

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating Transaction {@context}", request);

                return new()
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
