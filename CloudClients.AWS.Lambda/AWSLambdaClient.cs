using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CloudClients.AWS.Lambda
{
    public class AWSLambdaClient : IDisposable
    {
        private readonly AWSLambdaClientConfig _config;
        private readonly AmazonLambdaClient _client;
        private readonly IAWSLambdaClientLogger _logger;
        private bool _disposed = false;

        public AWSLambdaClient(AWSLambdaClientConfig config, IAWSLambdaClientLogger logger)
        {
            ValidateConfig(config);

            _config = config;
            _logger = logger;

            _client = CreateAmazonLambdaClient(_config);
        }

        public static AmazonLambdaClient CreateAmazonLambdaClient(AWSLambdaClientConfig config)
        {
            return config.UseAnonymousCredentials ?
                CreateAmazonLambdaClientWithAnonymousCredentials(config) :
                CreateAmazonLambdaClientWithCredentials(config);
        }

        private static AmazonLambdaClient CreateAmazonLambdaClientWithAnonymousCredentials(AWSLambdaClientConfig config)
        {
            return config.AmazonLambdaConfig == null ?
                new AmazonLambdaClient(new AnonymousAWSCredentials()) :
                new AmazonLambdaClient(new AnonymousAWSCredentials(), config.AmazonLambdaConfig);
        }

        private static AmazonLambdaClient CreateAmazonLambdaClientWithCredentials(AWSLambdaClientConfig config)
        {
            return config.AmazonLambdaConfig == null ?
                new AmazonLambdaClient() :
                new AmazonLambdaClient(config.AmazonLambdaConfig);
        }

        public static InvokeRequest CreateInvokeRequest<TPayload>(TPayload payload, InvokeRequestConfig config)
        {
            var result = CreateInvokeRequest(config);
            result.Payload = Serializer.Serialize(payload);
            return result;
        }

        public static InvokeRequest CreateInvokeRequest(InvokeRequestConfig config)
        {
            return new InvokeRequest
            {
                FunctionName = config.FunctionName,
                InvocationType = config.InvocationType,
                LogType = config.LogType
            };
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;
                if (disposing)
                {
                    _client.Dispose();
                }                
            }
        }

        public async Task<InvokeResponse> InvokeLambdaAsync(InvokeRequest invokeRequest)
        {
            if (_config.DebugMode)
            {
                var json = Serializer.Serialize(_client.Config);
                _logger.LogDebug($"Client config: {json}");
            }

            var invokeResponse = await _client.InvokeAsync(invokeRequest);

            LogResult(invokeResponse);
            ValidateStatusCode(invokeResponse);
            return invokeResponse;
        }

        public async Task<TResponse> InvokeAsync<TRequest, TResponse>(TRequest request)
            where TResponse: class
        {
            var invokeRequest = CreateInvokeRequest(request, _config.InvokeRequestConfig);
            var invokeResponse = await InvokeLambdaAsync(invokeRequest);
            return GetResponseFromPayload<TResponse>(invokeResponse);
        }

        public Task InvokeAsync<TRequest>(TRequest request)
        {
            var invokeRequest = CreateInvokeRequest(request, _config.InvokeRequestConfig);
            return InvokeLambdaAsync(invokeRequest);
        }

        public async Task<TResponse> InvokeAsync<TResponse>()
            where TResponse : class
        {
            var invokeRequest = CreateInvokeRequest(_config.InvokeRequestConfig);
            var invokeResponse = await InvokeLambdaAsync(invokeRequest);
            return GetResponseFromPayload<TResponse>(invokeResponse);
        }

        public Task InvokeAsync()
        {
            var invokeRequest = CreateInvokeRequest(_config.InvokeRequestConfig);
            return InvokeLambdaAsync(invokeRequest);
        }

        private void ValidateStatusCode(InvokeResponse invokeResponse)
        {
            if (_config.InvokeRequestConfig.ExpectedStatusCode.HasValue && invokeResponse.StatusCode != _config.InvokeRequestConfig.ExpectedStatusCode.Value)
            {
                _logger.LogWarning($"Invalid Status Code. Actual: {invokeResponse.StatusCode}, Expected: {_config.InvokeRequestConfig.ExpectedStatusCode.Value}");
            }
        }

        private void LogResult(InvokeResponse invokeResponse)
        {
            var logs = DecodeLogResult(invokeResponse);
            if (!string.IsNullOrEmpty(logs))
            {
                _logger.LogInformation($"Logs from Lambda:{System.Environment.NewLine}{logs}");
            }
        }

        private void ValidateConfig(AWSLambdaClientConfig config)
        {
            if (config.InvokeRequestConfig == null)
            {
                throw new ArgumentNullException(nameof(config.InvokeRequestConfig));
            }
        }

        private static string DecodeLogResult(InvokeResponse invokeResponse)
        {
            if (string.IsNullOrEmpty(invokeResponse.LogResult))
            {
                return null;
            }

            var logsData = Convert.FromBase64String(invokeResponse.LogResult);
            return Encoding.UTF8.GetString(logsData);
        }

        private TResponse GetResponseFromPayload<TResponse>(InvokeResponse invokeResponse)
            where TResponse : class
        {
            if (invokeResponse.Payload.Length == 0)
            {
                return null;
            }

            var json = Encoding.UTF8.GetString(invokeResponse.Payload.ToArray());
            _logger.LogInformation($"Response from Lambda:{System.Environment.NewLine}{json}");
            return Serializer.Deserialize<TResponse>(json);
        }
    }
}
