using Amazon.Lambda;

namespace CloudClients.AWS.Lambda
{
    public class AWSLambdaClientConfig
    {
        public bool UseAnonymousCredentials { get; set; }
        public AmazonLambdaConfig AmazonLambdaConfig { get; set; }
        public InvokeRequestConfig InvokeRequestConfig { get; set; }
        public bool DebugMode { get; set; }
    }

    public class InvokeRequestConfig
    {
        public string FunctionName { get; set; }
        public string InvocationType { get; set; }
        public string LogType { get; set; }
        public int? ExpectedStatusCode { get; set; }
    }
}
