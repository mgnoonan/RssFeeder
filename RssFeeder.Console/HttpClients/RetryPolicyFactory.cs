using OpenQA.Selenium;
using Polly;
using Polly.Retry;

namespace RssFeeder.Console.HttpClients;

internal static class RetryPolicyFactory
{
    public static Policy GetPolicy(int initialWait = 5)
    {
        return Policy
            .Handle<WebDriverException>(ex => ShouldRetryOn(ex))
            .Or<TimeoutException>()
            .WaitAndRetry(initialWait, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public static AsyncRetryPolicy GetAsyncPolicy(int initialWait = 5)
    {
        return Policy
            .Handle<WebDriverException>(ex => ShouldRetryOn(ex))
            .Or<TimeoutException>()
            .WaitAndRetryAsync(initialWait, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public static bool ShouldRetryOn(Exception ex)
    {
        if (ex is WebDriverException wdException)
        { 
            return wdException.Message.Contains("timed out after");
        }

        return ex is TimeoutException;
    }
}
