using ECommons.DalamudServices;
using ECommons.GameHelpers;
using Newtonsoft.Json.Linq;
using RotationSolver.Updaters;

namespace RotationSolver.IPC;

internal static class AccordActionPermissions
{
    internal static bool Allowed(uint action, uint adjusted)
    {
        Service.Config.AccordManualActions.TryGetValue(ManualActionPolicy.Key((uint)Player.Job, DataCenter.IsPvP), out var exclusions);
        return ManualActionPolicy.Allowed(AutomationOwnership.Managed, exclusions, action, adjusted);
    }

    internal static string Handle(string json)
    {
        try
        {
            if (!Svc.Framework.IsInFrameworkUpdateThread) throw new InvalidOperationException("Use the framework thread.");
            if (!Player.Available) throw new InvalidOperationException("Log in to configure the current job.");
            var request = JObject.Parse(json);
            uint job = (uint)Player.Job;
            bool pvp = DataCenter.IsPvP;
            string key = ManualActionPolicy.Key(job, pvp);
            string command = request.Value<string>("Command") ?? "read";
            if (command == "replace")
            {
                if (request.Value<uint>("Job") != job || request.Value<bool>("PvP") != pvp)
                    throw new InvalidOperationException("Job or combat mode changed. Refresh before saving.");
                if (request["ManualActions"] is not JArray values || values.Count > 1024)
                    throw new ArgumentException("Expected an action list.");
                var unique = new HashSet<uint>();
                foreach (var value in values)
                {
                    if (value.Type != JTokenType.Integer) throw new ArgumentException("Expected an action ID.");
                    uint id = value.Value<uint>();
                    if (id == 0 || !Service.GetSheet<Lumina.Excel.Sheets.Action>().TryGetRow(id, out _))
                        throw new ArgumentException("Unknown action.");
                    unique.Add(id);
                }
                uint[] ids = new uint[unique.Count];
                unique.CopyTo(ids);
                Array.Sort(ids);
                Service.Config.AccordManualActions.TryGetValue(key, out var previous);
                Service.Config.AccordManualActions[key] = ids;
                try { Service.Config.Save(); }
                catch
                {
                    if (previous == null) Service.Config.AccordManualActions.Remove(key);
                    else Service.Config.AccordManualActions[key] = previous;
                    throw;
                }
                // Drop a previously selected/scheduled action; selection will re-evaluate
                // with the new policy. Already accepted game actions cannot be recalled.
                DataCenter.ClearAccordPendingActions();
                ActionUpdater.ClearNextAction();
            }
            else if (command != "read") throw new ArgumentException("Unknown command.");
            Service.Config.AccordManualActions.TryGetValue(key, out var manual);
            // Enumerate the action sheet for this job instead of evaluating a rotation.
            // This also works while the coordinated runner is stopped.
            var actions = new JArray();
            foreach (var a in Service.GetSheet<Lumina.Excel.Sheets.Action>())
                if (a.IsPlayerAction && a.ClassJobCategory.IsValid && a.IsPvP == pvp &&
                    a.ClassJobCategory.Value.DoesJobMatchCategory(Player.Job))
                    actions.Add(new JObject { ["Id"] = a.RowId, ["Name"] = a.Name.ToString() });
            return JsonConvert.SerializeObject(new { Success = true, Version = 1, Job = job, PvP = pvp,
                ManualActions = manual ?? [], Actions = actions });
        }
        catch (Exception ex) { return JsonConvert.SerializeObject(new { Success = false, Version = 1, Error = ex.Message }); }
    }
}
