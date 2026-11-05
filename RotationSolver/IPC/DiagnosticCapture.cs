using AccordDiagnostics;
using ECommons.DalamudServices;
using RotationSolver.Updaters;
namespace RotationSolver.IPC;
internal static class DiagnosticCapture
{
    private static DiagnosticEndpoint? endpoint;
    internal static void Start() => endpoint = new("RotationSolver", "RotationSolverReborn.Diagnostics.v1",
        () => new { AutomationOwnership.Allowed, AutomationOwnership.Managed, DataCenter.State, DataCenter.IsManual, DataCenter.IsTargetOnly,
            DataCenter.IsAutoDuty, DataCenter.IsHenched, DataCenter.IsPvPStateEnabled, Service.Config.TeachingMode,
            Forecast = new { DataCenter.BMRHasActiveModule, DataCenter.BMRActiveModuleName, DataCenter.BMRNextRaidwideIn, DataCenter.BMRNextTankbusterIn,
                DataCenter.BMRNextDowntimeIn, DataCenter.BMRNextDowntimeEndIn, DataCenter.BMRNextKnockbackIn, DataCenter.BMRNextDamageIn,
                DataCenter.BMRNextDamageType, DataCenter.BMRSpecialModeType, DataCenter.BMRForceCancelCast, DataCenter.BMRForceCancelCastAI, DataCenter.BMRIsMoving },
            NextAction = ActionUpdater.NextAction?.ID, NextGcd = ActionUpdater.NextGCDAction?.ID },
        () => new { Settings = Service.Config, Permissions = Service.Config.AccordManualActions });
    internal static void Stop() { endpoint?.Dispose(); endpoint = null; }
}
