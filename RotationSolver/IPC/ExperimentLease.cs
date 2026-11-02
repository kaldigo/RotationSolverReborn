namespace RotationSolver.IPC;

// Monotonic, session-only consent. A lost client never leaves the engine running.
internal sealed class ExperimentLease
{
    internal const long DurationMs = 4000;
    private string? token;
    private long expires;
    internal bool Active(long now, bool blocked) => !blocked && token != null && now < expires;
    internal string Begin(long now, bool blocked)
    {
        if (blocked) throw new InvalidOperationException("Disable stable Accord, Wrath and stock RSR before starting an experiment.");
        token = Guid.NewGuid().ToString("N");
        expires = now + DurationMs;
        return token;
    }
    internal bool Renew(string supplied, long now, bool blocked)
    {
        if (!Active(now, blocked) || supplied != token) return false;
        expires = now + DurationMs;
        return true;
    }
    internal bool End(string supplied)
    {
        if (token == null || supplied != token) return false;
        token = null;
        expires = 0;
        return true;
    }
}
