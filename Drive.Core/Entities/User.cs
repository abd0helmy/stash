using Drive.Core.Enums;

namespace Drive.Core.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public UserRole Role { get; set; } = UserRole.User;

    public bool IsEmailVerified { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<RefreshToken> RefreshTokens { get; set; }
        = new List<RefreshToken>();
}