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
