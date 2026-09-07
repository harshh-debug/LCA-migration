using System.Collections.Concurrent;

using Lca.Infrastructure.Configuration;

using Microsoft.Extensions.Options;

namespace Lca.Infrastructure.Identity;

public interface IAccountRecoveryThrottle
{
    bool TryAcquire(string normalizedEmail, DateTimeOffset now);
}

public sealed class InMemoryAccountRecoveryThrottle(IOptions<AccountRecoveryOptions> options) : IAccountRecoveryThrottle
{
    private readonly ConcurrentDictionary<string, EmailWindow> windows = new(StringComparer.Ordinal);
    private readonly AccountRecoveryOptions settings = options.Value;

    public bool TryAcquire(string normalizedEmail, DateTimeOffset now)
    {
        if (windows.Count > 10_000)
        {
            foreach ((string key, EmailWindow value) in windows)
            {
                if (now - value.WindowStartedAt >= TimeSpan.FromHours(1))
                {
                    windows.TryRemove(key, out _);
                }
            }
        }

        bool acquired = false;
        windows.AddOrUpdate(
            normalizedEmail,
            _ =>
            {
                acquired = true;
                return new EmailWindow(now, now, 1);
            },
            (_, current) =>
            {
                EmailWindow active = now - current.WindowStartedAt >= TimeSpan.FromHours(1)
                    ? new EmailWindow(now, DateTimeOffset.MinValue, 0)
                    : current;
                if (active.Count >= settings.MaximumEmailsPerHour
                    || now - active.LastSentAt < TimeSpan.FromMinutes(settings.MinimumEmailIntervalMinutes))
                {
                    return active;
                }

                acquired = true;
                return active with { LastSentAt = now, Count = active.Count + 1 };
            });
        return acquired;
    }

    private sealed record EmailWindow(DateTimeOffset WindowStartedAt, DateTimeOffset LastSentAt, int Count);
}
