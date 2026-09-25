namespace Drive.Core.Entities;

public class File
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string ObjectKey { get; set; } = null!;

    public string MimeType { get; set; } = null!;

    public long Size { get; set; }

    public Guid OwnerId { get; set; }

    public Guid? FolderId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public User Owner { get; set; } = null!;

    public Folder? Folder { get; set; }
}