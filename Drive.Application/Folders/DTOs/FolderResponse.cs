namespace Drive.Application.Folders.DTOs;

public record FolderResponse(
    Guid Id,
    string Name,
    Guid? ParentFolderId,
    Guid OwnerId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);