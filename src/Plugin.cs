using BepInEx;
using BuildTesterMode.Patches;
using HarmonyLib;

namespace BuildTesterMode;

[BepInPlugin("org.hlxii.plugin.buildtestermode", "Build Tester Mode", "0.0.0")]
public class Plugin : BaseUnityPlugin
{
    private static Harmony _harmony;

    private void Awake()
    {
        _harmony = new Harmony("org.hlxii.plugin.buildtestermode");
        _harmony.PatchAll(typeof(GameSettingsControllerPatch));
        _harmony.PatchAll(typeof(SettingsMenuPatch));
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}