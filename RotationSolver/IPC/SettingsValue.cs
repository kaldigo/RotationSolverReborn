using Newtonsoft.Json.Linq;

namespace RotationSolver.IPC;

internal static class SettingsValue
{
    internal static bool Supported(Type t) => t.IsEnum || t == typeof(bool) || t == typeof(string) ||
        t == typeof(int) || t == typeof(uint) || t == typeof(byte) || t == typeof(float) || t == typeof(double);

    internal static object Parse(Type type, string json)
    {
        if (!Supported(type)) throw new ArgumentException("Unsupported setting type.");
        var token = JToken.Parse(json);
        if (type.IsEnum)
        {
            if (token.Type != JTokenType.String || !Enum.TryParse(type, token.Value<string>(), false, out var value) || !Enum.IsDefined(type, value!))
                throw new ArgumentException("Use a defined enum name.");
            return value!;
        }
        var expected = type == typeof(bool) ? JTokenType.Boolean : type == typeof(string) ? JTokenType.String :
            type == typeof(float) || type == typeof(double) ? JTokenType.Float : JTokenType.Integer;
        if (token.Type != expected && !(expected == JTokenType.Float && token.Type == JTokenType.Integer))
            throw new ArgumentException("JSON value has the wrong type.");
        var result = token.ToObject(type)!;
        if (result is float f && !float.IsFinite(f) || result is double d && !double.IsFinite(d))
            throw new ArgumentException("A finite number is required.");
        return result;
    }
}
