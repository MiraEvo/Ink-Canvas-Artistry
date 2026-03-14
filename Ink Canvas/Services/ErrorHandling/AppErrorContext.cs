using System;

namespace Ink_Canvas.Services.ErrorHandling
{
    public sealed record AppErrorContext(string Source, string Operation)
    {
        public AppErrorSeverity Severity { get; init; } = AppErrorSeverity.Error;

        public string? UserMessage { get; init; }

        public bool ShouldNotifyUser { get; init; }

        public bool IsFatal { get; init; }

        public bool AllowRateLimit { get; init; }

        public string? RateLimitKey { get; init; }

        public string ResolveRateLimitKey(Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            if (!AllowRateLimit)
            {
                throw new InvalidOperationException("Rate limiting is not enabled for this error context.");
            }

            return !string.IsNullOrWhiteSpace(RateLimitKey)
                ? RateLimitKey
                : $"{Source}|{Operation}|{exception.GetType().FullName}";
        }
    }
}
