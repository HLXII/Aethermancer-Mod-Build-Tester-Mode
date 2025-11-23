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

        CustomSettingsManager.CustomSettings.Add(
            new BooleanCustomSetting()
            {
                Page = "Accessibility",
                Name = "Build Tester Mode",
                Description = "Turns on Build Testing mode; All available monsters in Monster Shrines, Infinite rerolls",
                Key = "Accessibility_build_tester_mode",
                IsEnabled = () => GameStateManager.Instance.IsMainMenu || ExplorationController.Instance.CurrentArea == EArea.PilgrimsRest
            }
        );
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}