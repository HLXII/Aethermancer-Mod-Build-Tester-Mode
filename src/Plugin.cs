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
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}