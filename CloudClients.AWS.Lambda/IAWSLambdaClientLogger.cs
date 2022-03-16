namespace CloudClients.AWS.Lambda
{
    public interface IAWSLambdaClientLogger
    {
        void LogInformation(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogDebug(string message);
    }
}
