global using Newtonsoft.Json;
global using System.Reflection;

namespace RotationSolver.Basic.Configuration
{
    // Match the actual source generator: wrapper property has a private setter.
    internal class Configs
    {
        public int Version { get; set; } = 1;
        public ConditionBoolean TeachingMode { get; private set; } = new(true, "TeachingMode");
        public bool Ordinary { get; set; }
        public bool ReadOnly { get; private set; }
        public static bool Static { get; set; }
        internal bool FailSave;
        internal int Saves;
        public void Save() { Saves++; if (FailSave) throw new InvalidOperationException("Save failed"); }
    }
}
namespace RotationSolver.IPC
{
    internal static class Service
    {
        internal static readonly Basic.Configuration.Configs Config = new();
    }
}
namespace AccordDiagnostics
{
    internal static class DiagnosticLog
    {
        internal static void Event(string name, Func<object> value) { }
    }
}
