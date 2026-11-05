"""Execute the production ownership cleanup and auto-attack hook against held controls."""
from pathlib import Path
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]


def method(path, signature):
    source = (ROOT / path).read_text(encoding='utf-8-sig')
    start = source.index(signature)
    end = source.index('{', start) + 1
    depth = 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    return source[start:end]


class OwnershipCleanupTests(unittest.TestCase):
    def test_lost_owner_releases_controls_and_passes_native_autoattack(self):
        cleanup = method('RotationSolver/IPC/AutomationOwnership.cs', 'internal static void StopExecution()')
        hook = method('RotationSolver/Updaters/AutoAttackUpdater.cs', 'private static unsafe bool SetAutoAttackStateDetour(')
        hook = hook.replace('private static unsafe', 'public static unsafe', 1)
        isolation = method('RotationSolver/IPC/AutomationOwnership.cs', 'internal static bool RequiresWrathIsolation')
        harness = r'''
using System;
namespace AccordDiagnostics { static class DiagnosticLog { public static void Event(string kind, Func<object?> data) {} public static bool Reject(string reason) => false; } }
static class DataCenter {
    public static bool State, IsManual, IsTargetOnly, IsAutoDuty, IsHenched, IsPvPStateEnabled;
    public static int Role, Pending, Cleared;
    public static bool IsActivated() => true; // Teaching mode stays active while the runner is stopped.
    public static void ClearAccordPendingActions() { Pending = 0; Cleared++; }
}
static class Service { public static bool CanMove; }
static class Svc { public static Interface PluginInterface = new(); }
class Interface { public System.Collections.Generic.List<Plugin> InstalledPlugins = new(); }
class Plugin { public string InternalName = ""; public bool IsLoaded; }
static class ActionUpdater { public static int Next; public static void ClearNextAction() => Next = 0; }
enum StateCommandType { Off }
static class Player { public static bool Available = true; }
static class PluginLog { public static void Debug(string value) { } }
struct AutoAttackState { }
namespace RotationSolver.Commands {
    static class RSCommands {
        public static int Stops;
        public static void UpdateState(StateCommandType command, int role) {
            Stops++;
            DataCenter.State = DataCenter.IsManual = DataCenter.IsTargetOnly = DataCenter.IsAutoDuty =
                DataCenter.IsHenched = DataCenter.IsPvPStateEnabled = false;
        }
    }
}
namespace RotationSolver.IPC {
    static class AutomationOwnership {
        public static bool Allowed, Managed;
        ISOLATION
        CLEANUP
    }
}
static unsafe class Hook {
    sealed class OriginalHook {
        public int Calls;
        public bool Original(AutoAttackState* self, bool value, bool sendPacket, bool instant) { Calls++; return false; }
    }
    static OriginalHook _setAutoAttackStateHook = new();
    static bool PlayerHasNoCastingStatus() => true;
    HOOK
    public static void Main() {
        RotationSolver.IPC.AutomationOwnership.Managed = true;
        if (RotationSolver.IPC.AutomationOwnership.RequiresWrathIsolation) throw new Exception("Absent Wrath must not require its IPC");
        var peer = new Plugin { InternalName = "WrathCombo", IsLoaded = true };
        Svc.PluginInterface.InstalledPlugins.Add(peer);
        if (!RotationSolver.IPC.AutomationOwnership.RequiresWrathIsolation) throw new Exception("Any loaded Wrath must require verified hook isolation");
        peer.IsLoaded = false;
        if (RotationSolver.IPC.AutomationOwnership.RequiresWrathIsolation) throw new Exception("Disabled Wrath must not require IPC");
        peer.IsLoaded = true; RotationSolver.IPC.AutomationOwnership.Managed = false;
        if (RotationSolver.IPC.AutomationOwnership.RequiresWrathIsolation) throw new Exception("Unmanaged upstream behavior must remain independent");
        foreach (var name in new[] { "State", "IsManual", "IsTargetOnly", "IsAutoDuty", "IsHenched", "IsPvPStateEnabled" }) {
            typeof(DataCenter).GetField(name)!.SetValue(null, true);
            Service.CanMove = false; DataCenter.Pending = 3; ActionUpdater.Next = 5;
            int before = RotationSolver.Commands.RSCommands.Stops;
            RotationSolver.IPC.AutomationOwnership.StopExecution();
            if (!Service.CanMove || DataCenter.Pending != 0 || ActionUpdater.Next != 0 || RotationSolver.Commands.RSCommands.Stops != before + 1)
                throw new Exception("Lost ownership left controls held: " + name);
            RotationSolver.IPC.AutomationOwnership.StopExecution();
            if (RotationSolver.Commands.RSCommands.Stops != before + 1) throw new Exception("Repeated stop reran normal state transition");
        }
        if (SetAutoAttackStateDetour(null, true, false, false) || _setAutoAttackStateHook.Calls != 1)
            throw new Exception("Stopped RSR intercepted manual auto attack");
        RotationSolver.IPC.AutomationOwnership.Allowed = true;
        if (!SetAutoAttackStateDetour(null, true, false, false) || _setAutoAttackStateHook.Calls != 1)
            throw new Exception("Active RSR lost its ordinary no-casting-status protection");
    }
}
'''.replace('CLEANUP', cleanup).replace('HOOK', hook).replace('ISOLATION', isolation)
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            (directory / 'Review.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><AllowUnsafeBlocks>true</AllowUnsafeBlocks></PropertyGroup></Project>')
            (directory / 'Program.cs').write_text(harness)
            result = subprocess.run(['dotnet', 'run', '--project', str(directory / 'Review.csproj'), '-c', 'Release'], capture_output=True, text=True)
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
