namespace Lca.Core.Security;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    string? UserId { get; }

    AccountType? AccountType { get; }
}
