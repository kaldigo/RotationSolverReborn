namespace RotationSolver.IPC;

// Separate from per-intent Wrath exclusions: these permissions govern only the
// coordinated RSR runner, and never modify the player's native action buttons.
internal static class ManualActionPolicy
{
    internal static string Key(uint job, bool pvp) => $"{job}:{(pvp ? "pvp" : "pve")}";
    internal static bool Allowed(bool managed, System.Collections.Generic.IReadOnlyCollection<uint>? excluded, uint action, uint adjusted)
    {
        if (!managed || excluded == null) return true;
        foreach (uint id in excluded) if (id == action || id == adjusted) return false;
        return true;
    }
}
