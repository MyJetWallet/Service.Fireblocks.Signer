using System.ServiceModel;
using System.Threading.Tasks;
using Service.Fireblocks.Signer.Grpc.Models.Encryption;

namespace Service.Fireblocks.Signer.Grpc
{
    [ServiceContract]
    public interface IEncryptionService
    {
        [OperationContract]
        Task<EncryptionResponse> EncryptAsync(EncryptionRequest request);

        [OperationContract]
        Task<SetApiKeyResponse> SetApiKeysAsync(SetApiKeyRequest request);
    }
}