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
}