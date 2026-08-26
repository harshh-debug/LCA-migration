namespace Lca.Application.Security;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    string? UserId { get; }

    IReadOnlyCollection<string> Permissions { get; }
}

