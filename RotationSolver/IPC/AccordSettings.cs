using Newtonsoft.Json.Linq;
using RotationSolver.Basic.Configuration;

namespace RotationSolver.IPC;

// Additive local settings access only. Rotation, commands, identity and execution stay upstream.
internal static class AccordSettings
{
    private static bool Supported(PropertyInfo p) => p.Name != "Version" && p.GetIndexParameters().Length == 0 &&
        p.GetMethod?.IsPublic == true && !p.GetMethod.IsStatic && p.SetMethod?.IsPublic == true &&
        (p.PropertyType == typeof(ConditionBoolean) || SettingsValue.Supported(p.PropertyType));

    internal static string Read()
    {
        var values = new JObject();
        foreach (var p in typeof(Configs).GetProperties())
        {
            if (!Supported(p)) continue;
            var value = p.GetValue(Service.Config);
            values[p.Name] = value is ConditionBoolean flag ? new JValue(flag.Value) : value == null ? JValue.CreateNull() : JToken.FromObject(value);
        }
        return JsonConvert.SerializeObject(new { Success = true, ApiVersion = 1, Settings = values });
    }

    internal static string Set(string name, string valueJson)
    {
        try
        {
            var p = typeof(Configs).GetProperty(name);
            if (p == null || !Supported(p)) throw new ArgumentException("Unknown or unsupported setting.");
            var flag = p.GetValue(Service.Config) as ConditionBoolean;
            var converted = SettingsValue.Parse(flag == null ? p.PropertyType : typeof(bool), valueJson);
            var previous = flag == null ? p.GetValue(Service.Config) : flag.Value;
            try
            {
                if (flag != null) flag.Value = (bool)converted;
                else p.SetValue(Service.Config, converted);
                Service.Config.Save();
            }
            catch
            {
                if (flag != null) flag.Value = (bool)previous!;
                else p.SetValue(Service.Config, previous);
                throw;
            }
            return JsonConvert.SerializeObject(new { Success = true, ApiVersion = 1 });
        }
        catch (Exception ex)
        {
            return JsonConvert.SerializeObject(new { Success = false, ApiVersion = 1, Error = ex.Message });
        }
    }
}
