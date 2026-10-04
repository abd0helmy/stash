using System.ComponentModel.DataAnnotations;

namespace Drive.Api.Contracts.Folders;

public record CreateFolderRequest(
    [Required] string Name,
    Guid? ParentFolderId
);

public record RenameFolderRequest(
    [Required] string Name
);

public record MoveFolderRequest(
    Guid? TargetFolderId
);
