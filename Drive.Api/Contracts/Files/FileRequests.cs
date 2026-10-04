using System.ComponentModel.DataAnnotations;

namespace Drive.Api.Contracts.Files;

public record RenameFileRequest(
    [Required] string Name
);

public record MoveFileRequest(
    Guid? TargetFolderId
);
