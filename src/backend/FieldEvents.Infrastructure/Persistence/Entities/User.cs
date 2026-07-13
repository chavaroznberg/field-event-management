namespace FieldEvents.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity for application users (Dispatchers and Technicians).
/// Kept in Infrastructure because it carries authentication concerns (PasswordHash)
/// that have no place in the Domain layer.
/// Full authentication behaviour (login endpoint, JWT issuance, role enforcement) is not yet implemented.
/// </summary>
public sealed class User
{
    private User() { }

    public Guid Id { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;

    /// <summary>"Dispatcher" or "Technician" — validated at the application boundary.</summary>
    public string Role { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public static User Create(string userName, string displayName, string passwordHash, string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        return new User
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            Role = role,
            IsActive = true,
        };
    }
}
