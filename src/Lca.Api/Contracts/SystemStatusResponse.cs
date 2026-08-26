namespace Lca.Api.Contracts;

public sealed record SystemStatusResponse(
    string Service,
    string Status,
    string Version,
    DateTimeOffset TimestampUtc,
    string CorrelationId);

