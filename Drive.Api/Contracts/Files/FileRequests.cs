using System.ComponentModel.DataAnnotations;

namespace Drive.Api.Contracts.Files;

public class UploadFileRequest
{
    [Required]
    public IFormFile File { get; set; } = null!;

    public Guid? FolderId { get; set; }
}

public record RenameFileRequest(
    [Required] string Name
);

public record MoveFileRequest(
    Guid? TargetFolderId
);
