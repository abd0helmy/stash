namespace Drive.Application.Files.DTOs;

public record FileResponse(
    Guid Id,
    string Name,
    string MimeType,
    long Size,
    Guid? FolderId,
    DateTime CreatedAt,
    DateTime? UpdatedAt);