namespace Drive.Core.Common;

public enum ErrorType
{
    Failure = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Unauthenticated = 4,
    Forbidden = 5
}

public sealed record Error(
    string Code,
    string Description,
    ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None =
        new(string.Empty, string.Empty);

    public static readonly Error NullValue =
        new(
            "Error.NullValue",
            "The specified result value is null.",
            ErrorType.Failure);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Validation(string code, string description) =>
        new(code, description, ErrorType.Validation);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);

    public static Error Unauthenticated(string code, string description) =>
        new(code, description, ErrorType.Unauthenticated);

    public static readonly Error InternalServerError =
        new(
            "Error.InternalServerError",
            "An internal server error occurred.",
            ErrorType.Failure);

    public static readonly Error InvalidCredentials =
        new(
            "Auth.InvalidCredentials",
            "The username/email or password is incorrect.",
            ErrorType.Unauthenticated);
    
    public static readonly Error InvalidRefreshToken =
        new(
            "Auth.InvalidRefreshToken",
            "The refresh token is invalid or has expired.",
            ErrorType.Unauthenticated);

    public static readonly Error EmailNotVerified =
        new(
            "Auth.EmailNotVerified",
            "Please verify your email address before logging in.",
            ErrorType.Forbidden);

    public static readonly Error SubscriptionNotFound =
        NotFound("Billing.SubscriptionNotFound", "The user does not have an active subscription.");

    public static readonly Error PlanNotFound =
        NotFound("Billing.PlanNotFound", "The requested subscription plan was not found.");

    public static Error StorageQuotaExceeded(long usedBytes, long limitBytes, long requestedBytes) =>
        Forbidden(
            "Billing.StorageQuotaExceeded",
            $"Storage quota exceeded. Used: {FormatBytes(usedBytes)}, Limit: {FormatBytes(limitBytes)}, Requested: {FormatBytes(requestedBytes)}");

    public static Error ApiRequestQuotaExceeded(int usedRequests, int limitRequests) =>
        Forbidden(
            "Billing.ApiRequestQuotaExceeded",
            $"Monthly API request quota exceeded. Used: {usedRequests}, Limit: {limitRequests}");

    public static Error FileSizeLimitExceeded(long requestedBytes, long maxFileSizeBytes) =>
        Validation(
            "Billing.FileSizeLimitExceeded",
            $"File size exceeds the plan maximum. Requested: {FormatBytes(requestedBytes)}, Max allowed: {FormatBytes(maxFileSizeBytes)}");

    public static readonly Error SubscriptionInactive =
        Forbidden("Billing.SubscriptionInactive", "The subscription is not active.");

    public static Error FeatureNotAvailable(string feature) =>
        Forbidden("Billing.FeatureNotAvailable", $"The feature '{feature}' is not available in the current plan.");

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = ["B", "KB", "MB", "GB", "TB"];
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:0.##} {suffixes[counter]}";
    }
}