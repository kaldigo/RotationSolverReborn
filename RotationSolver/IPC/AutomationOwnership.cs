using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using Newtonsoft.Json.Linq;
using RotationInterop;
using RotationSolver.Updaters;

namespace RotationSolver.IPC;

internal static class AutomationOwnership
{
    private static readonly AutomationLease Lease = new();
    private static Guid controller;
    [ThreadStatic] private static int externalDepth;

    internal static IDisposable? TryIsolateWrathHooks()
    {
        try { return IsolateWrathHooks(); }
        catch
        {
            Lease.Stop();
            StopExecution();
            return null;
        }
    }

    internal static void StopExecution()
    {
        // The normal updater cannot release its movement lock after an early
        // ownership return. Use RSR's ordinary Off transition once, then keep
        // transient suggestions/requests empty while the gate remains closed.
        if (DataCenter.State || DataCenter.IsManual || DataCenter.IsTargetOnly ||
            DataCenter.IsAutoDuty || DataCenter.IsHenched || DataCenter.IsPvPStateEnabled)
            RotationSolver.Commands.RSCommands.UpdateState(StateCommandType.Off, DataCenter.Role);
        Service.CanMove = true;
        DataCenter.ClearAccordPendingActions();
        ActionUpdater.ClearNextAction();
    }

    internal static IDisposable IsolateWrathHooks()
    {
        if (!Managed) return new Scope(false);
        if (externalDepth == 0 && !Svc.PluginInterface.GetIpcSubscriber<Guid, bool>("WrathCombo.AutomationExternal.Begin.v1").InvokeFunc(controller))
            throw new InvalidOperationException("Wrath did not confirm independent RSR action execution.");
        externalDepth++;
        return new Scope(true);
    }
    private sealed class Scope(bool managed) : IDisposable
    {
        public void Dispose()
        {
            if (!managed) return;
            if (--externalDepth == 0)
                Svc.PluginInterface.GetIpcSubscriber<Guid, bool>("WrathCombo.AutomationExternal.End.v1").InvokeFunc(controller);
        }
    }
    internal static bool Managed => Lease.Managed || Service.Config.AccordAutomationManaged;
    internal static bool Allowed => (!Service.Config.AccordAutomationManaged || Lease.Managed) && Lease.Allowed(Environment.TickCount64);
    private static unsafe bool Busy => ActionManager.Instance() is var am && am != null &&
        (am->ActionQueued || am->CastActionId != 0 || am->AnimationLock > 0);

    internal static string Handle(string json)
    {
        try
        {
            if (!Svc.Framework.IsInFrameworkUpdateThread) throw new InvalidOperationException("Use the framework thread.");
            var request = JObject.Parse(json);
            string command = request.Value<string>("Command") ?? "status";
            Guid.TryParse(request.Value<string>("Token"), out var token);
            long now = Environment.TickCount64;
            bool success = command switch
            {
                "status" => true,
                "block" => Lease.Block(token, now),
                "renew" => Lease.Renew(token, now),
                "start" => Lease.Start(token, now, Busy),
                "release" => Lease.Release(token, now),
                _ => false,
            };
            if (success && command == "block")
            {
                controller = token;
                RotationSolver.Commands.RSCommands.UpdateState(StateCommandType.Off, DataCenter.Role);
                DataCenter.ClearAccordPendingActions();
                ActionUpdater.ClearNextAction();
                Service.Config.AccordAutomationManaged = true;
                Service.Config.Save();
            }
            if (success && command == "release")
            {
                Service.Config.AccordAutomationManaged = false;
                try { Service.Config.Save(); }
                catch { Service.Config.AccordAutomationManaged = true; throw; }
            }
            if (success && command == "start")
            {
                RotationSolver.Commands.RSCommands.UpdateState(DataCenter.IsPvP ? StateCommandType.PvP : StateCommandType.Auto, DataCenter.Role);
                success = DataCenter.State;
                if (!success) Lease.Stop();
            }
            return new JObject { ["Success"] = success, ["Version"] = 1, ["Managed"] = Managed,
                ["Allowed"] = Allowed, ["Owned"] = Lease.OwnedBy(token, now), ["Busy"] = Busy,
                ["Build"] = typeof(AutomationOwnership).Assembly.ManifestModule.ModuleVersionId.ToString() }.ToString();
        }
        catch (Exception ex) { Lease.Stop(); return new JObject { ["Success"] = false, ["Error"] = ex.Message }.ToString(); }
    }
}
