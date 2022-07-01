using System.ServiceModel;
using System.Threading.Tasks;
using Service.Fireblocks.Signer.Grpc.Models.Addresses;
using Service.Fireblocks.Signer.Grpc.Models.Transactions;

namespace Service.Fireblocks.Signer.Grpc
{
    [ServiceContract]
    public interface ITransactionService
    {
        [OperationContract]
        Task<CreateTransactionResponse> CreateTransactionAsync(CreateTransactionRequest request);

        [OperationContract]
        Task<EstimateTransactionResponse> EstimateTransactionAsync(EstimateTransactionRequest request);

        [OperationContract]
        Task<ValidateAddressResponse> ValidateAddressAsync(ValidateAddressRequest request);
    }
}