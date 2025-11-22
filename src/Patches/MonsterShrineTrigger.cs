using HarmonyLib;

namespace BuildTesterMode.Patches;

internal static class MonsterShrineTriggerPatch
{
    [HarmonyPatch(typeof(MonsterShrineTrigger), "GenerateMementosForShrine")]
    [HarmonyPrefix]
    static bool GenerateMementosForShrine(MonsterShrineTrigger __instance)
    {
        if (!GameSettingsController.Instance.Extra().BuildTesterMode)
        {
            return true;
        }

        __instance.ShrineSpecificSouls.Clear();
        foreach (var monster in InventoryManager.Instance.GetAvailableMonsterSouls(excludeActiveMonsters: true))
        {
            MonsterMemento memento = new MonsterMemento
            {
                Monster = monster
            };
            __instance.ShrineSpecificSouls.Add(memento);
        }

        return false;
    }
}