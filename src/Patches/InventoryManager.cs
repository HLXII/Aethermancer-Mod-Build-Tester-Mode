using HarmonyLib;

namespace BuildTesterMode.Patches;

internal static class InventoryManagerPatch
{
    [HarmonyPatch(typeof(InventoryManager), "RemoveSkillReroll")]
    [HarmonyPrefix]
    static bool RemoveSkillReroll(InventoryManager __instance)
    {
        // Don't run this if BuildTesterMode is on
        return !GameSettingsController.Instance.Extra().BuildTesterMode;
    }
}