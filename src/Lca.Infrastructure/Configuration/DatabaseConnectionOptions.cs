namespace Lca.Infrastructure.Configuration;

public sealed class DatabaseConnectionOptions
{
    public const string SectionName = "ConnectionStrings";

    public string ConnectionString { get; init; } = string.Empty;

    public string LCASolutionConnectionString { get; init; } = string.Empty;

    public string LcaOrderConnectionString { get; init; } = string.Empty;

    public string HotelshopEstore { get; init; } = string.Empty;

    public string? Find(string name) => name switch
    {
        nameof(ConnectionString) => ConnectionString,
        nameof(LCASolutionConnectionString) => LCASolutionConnectionString,
        nameof(LcaOrderConnectionString) => LcaOrderConnectionString,
        nameof(HotelshopEstore) => HotelshopEstore,
        _ => null,
    };
}
