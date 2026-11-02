using ECommons.DalamudServices;
using RotationSolver.Commands;
using RotationSolver.Updaters;

namespace RotationSolver.IPC;

internal static class AccordExperiment
{
    private static readonly ExperimentLease Lease = new();
    private static bool Conflicts
    {
        get
        {
            var clientLoaded = false;
            foreach (var plugin in Svc.PluginInterface.InstalledPlugins)
            {
                if (!plugin.IsLoaded) continue;
                if (plugin.InternalName is "Accord" or "WrathCombo" or "WrathFork" or "RotationSolver") return true;
                if (plugin.InternalName == "AccordRSRTesting") clientLoaded = true;
            }
            return !clientLoaded;
        }
    }
    internal static bool CanRun => Lease.Active(Environment.TickCount64, Conflicts) && !DataCenter.IsPvP;
    private static string Reply(bool success, string? error = null) => JsonConvert.SerializeObject(new { Success = success, Error = error });
    internal static string Discover() => JsonConvert.SerializeObject(new {
        Success = true, Identity = "AccordRSREngine", ApiVersion = 1,
        EngineVersion = typeof(AccordExperiment).Assembly.GetName().Version?.ToString(),
        UpstreamCommit = "ec34ec9433fd04012fb5fa059db8b775c932161f",
        Stage = "Native RSR baseline and selector observation", CanRun,
        Blocked = Conflicts || DataCenter.IsPvP,
        Capabilities = new[] { "SessionLease", "NativeManualTargetRotation", "CachedSelectorObservation" }
    });
    internal static string Begin()
    {
        try
        {
            var token = Lease.Begin(Environment.TickCount64, Conflicts || DataCenter.IsPvP);
            // Starting a session never starts a rotation left enabled by another controller.
            RSCommands.CancelState();
            return JsonConvert.SerializeObject(new { Success = true, Token = token });
        }
        catch (InvalidOperationException ex) { return Reply(false, ex.Message); }
    }
    internal static string Heartbeat(string token) => Reply(Lease.Renew(token, Environment.TickCount64, Conflicts || DataCenter.IsPvP), "Session expired or another engine is loaded.");
    internal static string End(string token)
    {
        if (!Lease.End(token)) return Reply(false, "Unknown session.");
        RSCommands.CancelState();
        ActionUpdater.ClearNextAction();
        return Reply(true);
    }
    internal static string Run(string token)
    {
        if (!Lease.Renew(token, Environment.TickCount64, Conflicts || DataCenter.IsPvP)) return Reply(false, "Start an eligible test session first.");
        RSCommands.UpdateState(StateCommandType.Manual, DataCenter.Role);
        return Reply(true);
    }
    internal static string Stop(string token)
    {
        if (!Lease.Renew(token, Environment.TickCount64, Conflicts || DataCenter.IsPvP)) return Reply(false, "No active session.");
        RSCommands.CancelState();
        ActionUpdater.ClearNextAction();
        return Reply(true);
    }
    internal static string Observe()
    {
        // Observe existing results only: no TryInvoke, CanUse, config mutation or execution.
        var r = DataCenter.CurrentRotation;
        uint? Id(IAction? action) => action?.AdjustedID;
        return JsonConvert.SerializeObject(new {
            Success = true, Job = (uint)DataCenter.Job, Running = CanRun && DataCenter.State,
            SelectorPreviewAvailable = CanRun && DataCenter.DrawingActions && r?.IsValid == true,
            NextAction = CanRun ? Id(ActionUpdater.NextAction) : null,
            Selectors = !CanRun || !DataCenter.DrawingActions || r == null ? null : new {
                HealSingleSpell = Id(r.ActionHealSingleGCD), HealSingleAbility = Id(r.ActionHealSingleAbility),
                HealPartySpell = Id(r.ActionHealAreaGCD), HealPartyAbility = Id(r.ActionHealAreaAbility),
                ProtectSingleSpell = Id(r.ActionDefenseSingleGCD), ProtectSingleAbility = Id(r.ActionDefenseSingleAbility),
                ProtectPartySpell = Id(r.ActionDefenseAreaGCD), ProtectPartyAbility = Id(r.ActionDefenseAreaAbility),
                MoveForwardSpell = Id(r.ActionMoveForwardGCD), MoveForwardAbility = Id(r.ActionMoveForwardAbility),
                MoveBackAbility = Id(r.ActionMoveBackAbility)
            }
        });
    }
}
