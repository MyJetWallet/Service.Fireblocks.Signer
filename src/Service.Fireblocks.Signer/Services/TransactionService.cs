using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.ApiSecurityManager.ApiKeys;
using MyJetWallet.ApiSecurityManager.AsymmetricEncryption;
using MyJetWallet.ApiSecurityManager.TransactionSignature;
using MyJetWallet.Fireblocks.Client;
using MyJetWallet.Fireblocks.Domain.Models.Addresses;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.Abstractions;
using Service.Blockchain.Wallets.MyNoSql.AssetsMappings;
using Service.Fireblocks.Signer.Grpc;
using Service.Fireblocks.Signer.Grpc.Models.Addresses;
using Service.Fireblocks.Signer.Grpc.Models.Transactions;

namespace Service.Fireblocks.Signer.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ILogger<TransactionService> _logger;
        private readonly IClient _client;
        private readonly ITransactionsClient _transactionsClient;
        private readonly IVaultClient _vaultClient;
        private readonly IMyNoSqlServerDataReader<AssetMappingNoSql> _assetMappings;
        private readonly IAsymmetricEncryptionService _asymmetricEncryptionService;
        private readonly IApiKeyStorage _apiKeyStorage;

        public TransactionService(ILogger<TransactionService> logger,
            IClient client,
            ITransactionsClient transactionsClient,
            IVaultClient vaultClient,
            IMyNoSqlServerDataReader<AssetMappingNoSql> assetMappings,
            IAsymmetricEncryptionService asymmetricEncryptionService,
            IApiKeyStorage apiKeyStorage)
        {
            _logger = logger;
            _client = client;
            _transactionsClient = transactionsClient;
            _vaultClient = vaultClient;
            _assetMappings = assetMappings;
            _asymmetricEncryptionService = asymmetricEncryptionService;
            _apiKeyStorage = apiKeyStorage;
        }

        public async Task<Grpc.Models.Transactions.CreateTransactionResponse> CreateTransactionAsync(CreateTransactionRequest request)
        {
            try
            {
                _logger.LogInformation("Signer CreateTransactionAsync {@context}", request.ToJson());

                if (Program.Settings.CheckTransactionSignature)
                {
                    //Withdrawal case on external address
                    if (string.IsNullOrEmpty(request.DestinationVaultAccountId))
                    {
                        var publicKey = _apiKeyStorage.Get(Program.Settings.SignaturePublicApiKeyId);

                        if (publicKey == null)
                        {
                            _logger.LogError("No key is set for signature check FIREBLOCKS! {@context}", request.ToJson());

                            return new Grpc.Models.Transactions.CreateTransactionResponse
                            {
                                Error = new Grpc.Models.Common.ErrorResponse
                                {
                                    ErrorCode = Grpc.Models.Common.ErrorCode.NoKey,
                                }
                            };
                        }

                        var signatureComponents = TransactionSignatureComponents.Create(
                            request.AmountWithFee,
                            request.AssetSymbol,
                            request.AssetNetwork,
                            request.ClientId,
                            request.IssuedAtUnixTime,
                            request.ToAddress,
                            request.Tag);

                        var pubKey = AsymmetricEncryptionUtils.ReadPublicKeyFromPem(publicKey.PrivateKeyValue);
                        var signatureContent = signatureComponents.GetSignatureContent();
                        var isSigned = _asymmetricEncryptionService.VerifySign(
                            signatureContent,
                            request.Signature,
                            pubKey);

                        if (!isSigned)
                        {
                            _logger.LogError("Wrong signature FIREBLOCKS! {@context}", request.ToJson());

                            return new Grpc.Models.Transactions.CreateTransactionResponse
                            {
                                Error = new Grpc.Models.Common.ErrorResponse
                                {
                                    ErrorCode = Grpc.Models.Common.ErrorCode.WrongSignature,
                                }
                            };
                        }

                        var date = DateTimeOffset.FromUnixTimeMilliseconds(signatureComponents.IssuedAt).UtcDateTime;
                        if (DateTime.UtcNow - date > Program.Settings.SignatureValidFor)
                        {
                            _logger.LogError("Signature has expired FIREBLOCKS! {@context}", request.ToJson());

                            return new Grpc.Models.Transactions.CreateTransactionResponse
                            {
                                Error = new Grpc.Models.Common.ErrorResponse
                                {
                                    ErrorCode = Grpc.Models.Common.ErrorCode.SignatureExpired,
                                }
                            };
                        }

                        if (request.Amount > request.AmountWithFee)
                        {
                            _logger.LogError("SOMEONE USING OLD Signature TO TRANSFER MORE FUNDS FIREBLOCKS! {@context}", request.ToJson());

                            return new Grpc.Models.Transactions.CreateTransactionResponse
                            {
                                Error = new Grpc.Models.Common.ErrorResponse
                                {
                                    ErrorCode = Grpc.Models.Common.ErrorCode.SignatureExpired,
                                }
                            };
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No Signature needed, internal transfer {@context}", request.ToJson());
                    }
                }

                var assetMapping = _assetMappings.Get(AssetMappingNoSql.GeneratePartitionKey(request.AssetSymbol),
                                                      AssetMappingNoSql.GenerateRowKey(request.AssetNetwork));

                var fromVaultAccountId = string.IsNullOrEmpty(request.FromVaultAccountId) ? assetMapping.AssetMapping.WithdrawalVaultAccountId : request.FromVaultAccountId;
                var vaultAcc = await _vaultClient.AccountsGetAsync(fromVaultAccountId, default);

                if (vaultAcc.StatusCode != 200)
                {
                    _logger.LogError("Fireblocks signer can't execute http request: {@context}", request.ToJson());

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

                if (balance == null || !decimal.TryParse(balance.Available, out var availableBalance) || (availableBalance <= request.Amount && 
                    !request.TreatAsGrossAmount))
                {
                    return new Grpc.Models.Transactions.CreateTransactionResponse
                    {
                        Error = new Grpc.Models.Common.ErrorResponse
                        {
                            ErrorCode = Grpc.Models.Common.ErrorCode.NotEnoughBalance,
                            Message = $"Not enough balance ASSET: {assetMapping.AssetMapping.FireblocksAssetId}; VAULT ACCOUNT: {fromVaultAccountId}"
                        }
                    };
                }

                var idempotencyKey = $"transaction_{request.ExternalTransactionId}";
                idempotencyKey = idempotencyKey.Substring(0, Math.Min(40, idempotencyKey.Length));
                DestinationTransferPeerPath destination;

                if (!string.IsNullOrEmpty(request.ToAddress))
                {
                    destination = new DestinationTransferPeerPath
                    {
                        Type = TransferPeerPathType.ONE_TIME_ADDRESS,
                        OneTimeAddress = new OneTimeAddress
                        {
                            Address = request.ToAddress,
                            Tag = request.Tag,
                        }
                    };
                }
                else if (!string.IsNullOrEmpty(request.DestinationVaultAccountId))
                {
                    destination = new DestinationTransferPeerPath
                    {
                        Type = TransferPeerPathType.VAULT_ACCOUNT,
                        Id = request.DestinationVaultAccountId
                    };
                } else
                {
                    return new Grpc.Models.Transactions.CreateTransactionResponse
                    {
                        Error = new Grpc.Models.Common.ErrorResponse
                        {
                            ErrorCode = Grpc.Models.Common.ErrorCode.ApiError,
                            Message = $"Either {nameof(request.DestinationVaultAccountId)} or {nameof(request.ToAddress)} should be filled!"
                        }
                    };
                }

                var response = await _client.TransactionsPostAsync(idempotencyKey, new TransactionRequest
                {
                    Amount = request.Amount,
                    AssetId = assetMapping.AssetMapping.FireblocksAssetId,
                    Source = new TransferPeerPath
                    {
                        Id = fromVaultAccountId,
                        Type = TransferPeerPathType.VAULT_ACCOUNT
                    },
                    ExternalTxId = request.ExternalTransactionId,
                    Destination = destination,
                    TreatAsGrossAmount = request.TreatAsGrossAmount,
                    FailOnLowFee = false,
                    FeeLevel = TransactionRequestFeeLevel.MEDIUM,
                    Operation = TransactionOperation.TRANSFER,
                    ForceSweep = request.ForceSweep,
                });

                _logger.LogInformation("Signer Response {@context}", new 
                {
                    Request = request.ToJson(),
                    Response = response.ToJson(),
                });

                return new()
                {
                    FireblocksTxId = response.Result.Id
                };

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating Transaction {@context}", request.ToJson());

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

        public async Task<Grpc.Models.Transactions.EstimateTransactionResponse> EstimateTransactionAsync(EstimateTransactionRequest request)
        {
            try
            {
                _logger.LogInformation("Signer EstimateTransactionAsync {@context}", request.ToJson());

                var assetMapping = _assetMappings.Get(AssetMappingNoSql.GeneratePartitionKey(request.AssetSymbol),
                                                      AssetMappingNoSql.GenerateRowKey(request.AssetNetwork));

                var fromVaultAccountId = string.IsNullOrEmpty(request.FromVaultAccountId) ? assetMapping.AssetMapping.WithdrawalVaultAccountId : request.FromVaultAccountId;
                var vaultAcc = await _vaultClient.AccountsGetAsync(fromVaultAccountId, default);

                if (vaultAcc.StatusCode != 200)
                {
                    _logger.LogError("Fireblocks signer can't execute http request: {@context}", request.ToJson());

                    return new Grpc.Models.Transactions.EstimateTransactionResponse
                    {
                        Error = new Grpc.Models.Common.ErrorResponse
                        {
                            ErrorCode = Grpc.Models.Common.ErrorCode.ApiError,
                        }
                    };
                }

                if (vaultAcc == null)
                {
                    return new Grpc.Models.Transactions.EstimateTransactionResponse
                    {
                        Error = new Grpc.Models.Common.ErrorResponse
                        {
                            ErrorCode = Grpc.Models.Common.ErrorCode.ApiError,
                        }
                    };
                }

                var balance = vaultAcc.Result.Assets.FirstOrDefault(x => x.Id == assetMapping.AssetMapping.FireblocksAssetId);

                if (balance == null || !decimal.TryParse(balance.Available, out var availableBalance) || (availableBalance <= request.Amount &&
                    !request.TreatAsGrossAmount))
                {
                    return new Grpc.Models.Transactions.EstimateTransactionResponse
                    {
                        Error = new Grpc.Models.Common.ErrorResponse
                        {
                            ErrorCode = Grpc.Models.Common.ErrorCode.NotEnoughBalance,
                            Message = $"Not enough balance ASSET: {assetMapping.AssetMapping.FireblocksAssetId}; VAULT ACCOUNT: {fromVaultAccountId}"
                        }
                    };
                }

                var idempotencyKey = $"transaction_{request.ExternalTransactionId}";
                idempotencyKey = idempotencyKey.Substring(0, Math.Min(40, idempotencyKey.Length));
                DestinationTransferPeerPath destination;

                if (!string.IsNullOrEmpty(request.ToAddress))
                {
                    destination = new DestinationTransferPeerPath
                    {
                        Type = TransferPeerPathType.ONE_TIME_ADDRESS,
                        OneTimeAddress = new OneTimeAddress
                        {
                            Address = request.ToAddress,
                            Tag = request.Tag,
                        }
                    };
                }
                else if (!string.IsNullOrEmpty(request.DestinationVaultAccountId))
                {
                    destination = new DestinationTransferPeerPath
                    {
                        Type = TransferPeerPathType.VAULT_ACCOUNT,
                        Id = request.DestinationVaultAccountId
                    };
                }
                else
                {
                    return new Grpc.Models.Transactions.EstimateTransactionResponse
                    {
                        Error = new Grpc.Models.Common.ErrorResponse
                        {
                            ErrorCode = Grpc.Models.Common.ErrorCode.ApiError,
                            Message = $"Either {nameof(request.DestinationVaultAccountId)} or {nameof(request.ToAddress)} should be filled!"
                        }
                    };
                }

                var response = await _transactionsClient.Estimate_feeAsync(new TransactionRequest
                {
                    Amount = request.Amount,
                    AssetId = assetMapping.AssetMapping.FireblocksAssetId,
                    Source = new TransferPeerPath
                    {
                        Id = fromVaultAccountId,
                        Type = TransferPeerPathType.VAULT_ACCOUNT
                    },
                    ExternalTxId = "",
                    Destination = destination,
                    TreatAsGrossAmount = request.TreatAsGrossAmount,
                    FailOnLowFee = false,
                    FeeLevel = TransactionRequestFeeLevel.MEDIUM,
                    Operation = TransactionOperation.TRANSFER,
                    ForceSweep = request.ForceSweep,
                });

                _logger.LogInformation("Signer Response {@context}", new
                {
                    Request = request.ToJson(),
                    Response = response.ToJson(),
                });

                return new()
                {
                    High = new EstimatedFee
                    {
                        GasLimit = response.Result.High.GasLimit,
                        GasPrice = response.Result.High.GasPrice
                    },
                    Medium = new EstimatedFee
                    {
                        GasLimit = response.Result.High.GasLimit,
                        GasPrice = response.Result.High.GasPrice
                    },
                    Low = new EstimatedFee
                    {
                        GasLimit = response.Result.High.GasLimit,
                        GasPrice = response.Result.High.GasPrice
                    },
                };

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error creating Transaction {@context}", request.ToJson());

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

        public async Task<Grpc.Models.Addresses.ValidateAddressResponse> ValidateAddressAsync(ValidateAddressRequest request)
        {
            try
            {
                var response = await _transactionsClient.Validate_addressAsync(request.AssetId, request.Address);

                return new Grpc.Models.Addresses.ValidateAddressResponse
                {
                    Address = request.Address,
                    IsValid = response.Result.IsValid,
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error validating address @{context}", request);

                return new Grpc.Models.Addresses.ValidateAddressResponse
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
