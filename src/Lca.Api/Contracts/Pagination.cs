namespace Lca.Api.Contracts;

public sealed record PagedResponse<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);
