using Drive.Core.Enums;

namespace Drive.Core.Entities;

public class ItemShare
{
    public Guid Id { get; set; }
 
    // The File or Folder being shared
    public Guid ItemId { get; set; }
    public ShareableItemType ItemType { get; set; }
 
    // Who owns the item and who it's shared with
    public Guid OwnerId { get; set; }
    public Guid SharedWithUserId { get; set; }
 
    public SharePermission Permission { get; set; }
 
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
}
