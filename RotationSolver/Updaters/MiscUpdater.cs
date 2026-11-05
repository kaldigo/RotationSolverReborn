using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using RotationSolver.Commands;
using RotationSolver.UI.HighlightTeachingMode;

namespace RotationSolver.Updaters;

internal static class MiscUpdater
{
	internal static void UpdateMisc()
	{
		UpdateEntry();
		CancelCastUpdater.UpdateCancelCast();
	}

	private static IDtrBarEntry? _dtrEntry;

	internal static void UpdateEntry()
    {
        // Accord owns the server-bar automation control, including on old configs.
        HideEntry();
    }

	private static void HideEntry()
	{
		if (_dtrEntry != null && _dtrEntry.Shown)
		{
			_dtrEntry.Shown = false;
		}
	}

	private static BitmapFontIcon GetJobIcon(Job job)
	{
		return job switch
		{
			Job.WAR => BitmapFontIcon.Warrior,
			Job.PLD => BitmapFontIcon.Paladin,
			Job.DRK => BitmapFontIcon.DarkKnight,
			Job.GNB => BitmapFontIcon.Gunbreaker,
			Job.AST => BitmapFontIcon.Astrologian,
			Job.WHM => BitmapFontIcon.WhiteMage,
			Job.SGE => BitmapFontIcon.Sage,
			Job.SCH => BitmapFontIcon.Scholar,
			Job.BLM => BitmapFontIcon.BlackMage,
			Job.SMN => BitmapFontIcon.Summoner,
			Job.RDM => BitmapFontIcon.RedMage,
			Job.PCT => BitmapFontIcon.Pictomancer,
			Job.BLU => BitmapFontIcon.BlueMage,
			Job.MNK => BitmapFontIcon.Monk,
			Job.SAM => BitmapFontIcon.Samurai,
			Job.DRG => BitmapFontIcon.Dragoon,
			Job.RPR => BitmapFontIcon.Reaper,
			Job.NIN => BitmapFontIcon.Ninja,
			Job.VPR => BitmapFontIcon.Viper,
			Job.BRD => BitmapFontIcon.Bard,
			Job.MCH => BitmapFontIcon.Machinist,
			Job.DNC => BitmapFontIcon.Dancer,
			Job.BSM => BitmapFontIcon.Blacksmith,
			Job.ARM => BitmapFontIcon.Armorer,
			Job.WVR => BitmapFontIcon.Weaver,
			Job.ALC => BitmapFontIcon.Alchemist,
			Job.CRP => BitmapFontIcon.Carpenter,
			Job.LTW => BitmapFontIcon.Leatherworker,
			Job.CUL => BitmapFontIcon.Culinarian,
			Job.GSM => BitmapFontIcon.Goldsmith,
			Job.FSH => BitmapFontIcon.Fisher,
			Job.MIN => BitmapFontIcon.Miner,
			Job.BTN => BitmapFontIcon.Botanist,
			Job.GLA => BitmapFontIcon.Gladiator,
			Job.CNJ => BitmapFontIcon.Conjurer,
			Job.MRD => BitmapFontIcon.Marauder,
			Job.PGL => BitmapFontIcon.Pugilist,
			Job.LNC => BitmapFontIcon.Lancer,
			Job.ROG => BitmapFontIcon.Rogue,
			Job.ARC => BitmapFontIcon.Archer,
			Job.THM => BitmapFontIcon.Thaumaturge,
			Job.ACN => BitmapFontIcon.Arcanist,
			Job.BST => BitmapFontIcon.Beastmaster,
			_ => BitmapFontIcon.ExclamationRectangle,
		};
	}

	internal static void PulseActionBar(uint actionID)
	{
		LoopAllSlotBar((bar, hot, index) =>
		{
			return IsActionSlotRight(bar, hot, actionID);
		});
	}

	private static bool IsActionSlotRight(ActionBarSlot slot, RaptureHotbarModule.HotbarSlot? hot, uint actionID)
	{
		if (hot.HasValue
			&& (hot.Value.OriginalApparentSlotType is not RaptureHotbarModule.HotbarSlotType.CraftAction and not RaptureHotbarModule.HotbarSlotType.Action
				|| hot.Value.ApparentSlotType is not RaptureHotbarModule.HotbarSlotType.CraftAction and not RaptureHotbarModule.HotbarSlotType.Action))
		{
			return false;
		}

		return Service.GetAdjustedActionId((uint)slot.ActionId) == actionID;
	}

	private static readonly List<nint> _pulseAddons = [];

	private delegate bool ActionBarAction(ActionBarSlot bar, RaptureHotbarModule.HotbarSlot? hot, uint highLightID);
	private static unsafe void LoopAllSlotBar(ActionBarAction doingSomething)
	{
		var framework = Framework.Instance();
		var uiModule = framework == null ? null : framework->GetUIModule();
		var raptureModule = uiModule == null ? null : uiModule->GetRaptureHotbarModule();
		if (raptureModule == null)
		{
			return;
		}

		var index = 0;
		var hotBarIndex = 0;

		HotbarAddonHelper.GetHotbarAddons(_pulseAddons);

		foreach (var intPtr in _pulseAddons)
		{
			var actionBar = (AddonActionBarBase*)intPtr;
			var hotBar = raptureModule->Hotbars[Math.Min(hotBarIndex, raptureModule->Hotbars.Length - 1)];

			var slotIndex = 0;
			foreach (var slot in actionBar->ActionBarSlotVector.AsSpan())
			{
				var highLightId = 0x53550000 + index;
				RaptureHotbarModule.HotbarSlot? hotSlot = hotBarIndex > 9 || slotIndex >= hotBar.Slots.Length ? null : hotBar.Slots[slotIndex];

				var iconAddon = slot.Icon;
				if (doingSomething(slot, hotSlot, (uint)highLightId)
					&& iconAddon != null && iconAddon->AtkResNode.IsVisible())
				{
					actionBar->PulseActionBarSlot(slotIndex);
					UIGlobals.PlaySoundEffect(12);
				}

				slotIndex++;
				index++;
			}
			hotBarIndex++;
		}
	}

	public static void Dispose()
	{
		if (_dtrEntry == null)
		{
			return;
		}

		_dtrEntry.Remove();
		_dtrEntry = null;
	}
}