using BepInEx;
using HarmonyLib;

namespace BuildTesterMode;

[BepInPlugin("org.hlxii.plugin.buildtestermode", "Build Tester Mode", "0.0.0")]
public class Plugin : BaseUnityPlugin
{
    private static Harmony _harmony;

    private void Awake()
    {
        _harmony = new Harmony("org.hlxii.plugin.buildtestermode");
        _harmony.PatchAll(typeof(Patches.GameSettingsControllerPatch));
        _harmony.PatchAll(typeof(Patches.SettingsMenuPatch));
        _harmony.PatchAll(typeof(Patches.InventoryManagerPatch));
        _harmony.PatchAll(typeof(Patches.SkillSelectMenuPatch));
        _harmony.PatchAll(typeof(Patches.MonsterShrineMenuPatch));
        _harmony.PatchAll(typeof(Patches.MonsterShrineTriggerPatch));
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}