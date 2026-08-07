namespace HAMS.Platform.Common.Contracts;

/// <summary>
/// Testable indirection over "now" — inject this instead of calling <see cref="DateTimeOffset.UtcNow"/>
/// or <see cref="DateTime.UtcNow"/> directly, anywhere business logic needs the current time
/// (audit timestamps, effective-date defaults, expiry checks). Makes date-dependent logic
/// (e.g. "has this teaching assignment's EffectiveTo passed?") deterministically testable.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>
    /// Convenience for effective-dated comparisons (<see cref="IEffectiveDated"/> uses
    /// <see cref="DateOnly"/>, not <see cref="DateTimeOffset"/>).
    /// </summary>
    DateOnly TodayUtc { get; }
}

/// <summary>The real, production implementation — the only place actual system-clock calls happen.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateOnly TodayUtc => DateOnly.FromDateTime(DateTime.UtcNow);
}
