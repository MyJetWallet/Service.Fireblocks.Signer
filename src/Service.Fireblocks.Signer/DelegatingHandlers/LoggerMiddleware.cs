using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using System.Net.Http;
using System.Threading.Tasks;

namespace MyJetWallet.Fireblocks.Client.DelegateHandlers
{
    //TODO: Complete logging and telemetry
    public class LoggerMiddleware : DelegatingHandler
    {
        private readonly ILogger<LoggerMiddleware> _logger;

        public LoggerMiddleware(ILogger<LoggerMiddleware> logger)
        {
            this._logger = logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            var activity = MyTelemetry.StartActivity("Fireblocks.Signer.Request");
            activity.AddTag("url", request.RequestUri);
            activity.AddTag("method", request.Method);
            var contentAsStr = request.Content?.ReadAsStringAsync().Result;

            if (!string.IsNullOrEmpty(contentAsStr))
            {
                activity.AddTag("content", request.Method);
                _logger.LogInformation(contentAsStr);
            }

            _logger.LogInformation($"REQUEST :: URL: {request.RequestUri} METHOD: {request.Method} CONTENT:{contentAsStr}");
            var response = await base.SendAsync(request, cancellationToken);

            contentAsStr = await response.Content?.ReadAsStringAsync();

            _logger.LogInformation($"RESPONSE :: URL: {request.RequestUri} METHOD: {request.Method} CONTENT:{contentAsStr} STATUS CODE: {response.StatusCode}");

            activity.AddTag("responseCode", response.StatusCode);

            if (!string.IsNullOrEmpty(contentAsStr))
                activity.AddTag("response", contentAsStr);

            return response;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }

            base.Dispose(disposing);
        }
    }
}
