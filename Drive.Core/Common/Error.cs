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
}