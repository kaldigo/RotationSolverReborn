using Newtonsoft.Json.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using RotationSolver.IPC;

static void Check(bool value) { if (!value) throw new Exception("Setting conversion failed"); }
static void Reject(Type type, string value)
{
    try { SettingsValue.Parse(type, value); }
    catch { return; }
    throw new Exception("Invalid setting accepted: " + value);
}
Check((bool)SettingsValue.Parse(typeof(bool), "true"));
Check((int)SettingsValue.Parse(typeof(int), "7") == 7);
Check((float)SettingsValue.Parse(typeof(float), "0.75") == 0.75f);
Check((DayOfWeek)SettingsValue.Parse(typeof(DayOfWeek), "\"Monday\"") == DayOfWeek.Monday);
Reject(typeof(bool), "\"false\"");
Reject(typeof(int), "1.5");
Reject(typeof(byte), "256");
Reject(typeof(uint), "-1");
Reject(typeof(DayOfWeek), "\"NotADay\"");
Reject(typeof(DayOfWeek), "999");
Reject(typeof(float), "NaN");
Reject(typeof(int), "null");
Reject(typeof(int), "{}");
Console.WriteLine("Settings conversion checks passed.");
Check(ManualActionPolicy.Allowed(false, new uint[] { 1 }, 1, 2));
Check(!ManualActionPolicy.Allowed(true, new uint[] { 1 }, 1, 2));
Check(!ManualActionPolicy.Allowed(true, new uint[] { 2 }, 1, 2));
Check(ManualActionPolicy.Allowed(true, new uint[] { 3 }, 1, 2));
Check(ManualActionPolicy.Allowed(true, null, 1, 2));
Check(ManualActionPolicy.Key(24, true) != ManualActionPolicy.Key(24, false));
Check(ManualActionPolicy.Key(24, false) != ManualActionPolicy.Key(28, false));
Console.WriteLine("Manual action permission checks passed.");

var binary = Path.Combine("RotationSolver", "bin", "Release", "RotationSolver.dll");
if (File.Exists(binary))
{
    using var stream = File.OpenRead(binary);
    using var pe = new PEReader(stream);
    var metadata = pe.GetMetadataReader();
    var config = metadata.TypeDefinitions.Select(h => metadata.GetTypeDefinition(h))
        .Single(t => metadata.GetString(t.Name) == "Configs");
    var properties = config.GetProperties().Select(h => metadata.GetString(metadata.GetPropertyDefinition(h).Name)).ToHashSet();
    foreach (var key in new[] { "TeachingMode", "ShowInfoOnDtr", "AutoBurst", "UseAbility", "UseDefenseAbility", "UseStDefense", "UseAoeDefense", "UseBmrTimeline", "UseDutyActions", "AutoTankStance", "UseGroundBeneficialAbility", "UseGroundBeneficialAbilityWhenMoving", "UseGroundBeneficialAbilityOnlySelf", "AutoHeal" })
        if (!properties.Contains(key)) throw new Exception("Missing Accord-owned RSR setting: " + key);
    Console.WriteLine("Built RSR settings surface verified.");
}

var initial = Newtonsoft.Json.Linq.JObject.Parse(AccordSettings.Read())["Settings"]!;
Check(initial["TeachingMode"]!.Value<bool>());
Check(initial["ReadOnly"] == null && initial["Static"] == null && initial["Version"] == null);
Check(Newtonsoft.Json.Linq.JObject.Parse(AccordSettings.Set("TeachingMode", "false")).Value<bool>("Success"));
Check(!Service.Config.TeachingMode.Value);
Check(!Newtonsoft.Json.Linq.JObject.Parse(AccordSettings.Read())["Settings"]!["TeachingMode"]!.Value<bool>());
Check(Newtonsoft.Json.Linq.JObject.Parse(AccordSettings.Set("Ordinary", "true")).Value<bool>("Success"));
Check(Service.Config.Ordinary);
Check(!Newtonsoft.Json.Linq.JObject.Parse(AccordSettings.Set("ReadOnly", "true")).Value<bool>("Success"));
Check(!Newtonsoft.Json.Linq.JObject.Parse(AccordSettings.Set("TeachingMode", "1")).Value<bool>("Success"));
Service.Config.FailSave = true;
Check(!Newtonsoft.Json.Linq.JObject.Parse(AccordSettings.Set("TeachingMode", "true")).Value<bool>("Success"));
Check(!Service.Config.TeachingMode.Value);
Check(!Newtonsoft.Json.Linq.JObject.Parse(AccordSettings.Set("Ordinary", "false")).Value<bool>("Success"));
Check(Service.Config.Ordinary);
Console.WriteLine("Production settings API: private-set wrapper read/write, validation and save rollback passed.");
