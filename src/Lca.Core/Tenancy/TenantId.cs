namespace Lca.Core.Tenancy;

public readonly record struct TenantId
{
    public TenantId(long value)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "A tenant identifier must be positive.");
        }

        Value = value;
    }

    public long Value { get; }

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
