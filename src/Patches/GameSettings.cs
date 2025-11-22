using System.Runtime.CompilerServices;
using HarmonyLib;

namespace BuildTesterMode.Patches;

internal static class GameSettingsControllerPatch
{
    [HarmonyPatch(typeof(GameSettingsController), "LoadSettings")]
    [HarmonyPostfix]
    private static void LoadSettings(GameSettingsController __instance)
    {
        var extra = __instance.Extra();

        extra.SetBuildTesterMode(PlayerPrefsManager.GetInt("Accessibility_build_tester_mode") > 0);
    }
}

public static class GameSettingsControllerExtensions
{
    private static readonly ConditionalWeakTable<GameSettingsController, ExtendedGameSettingsController> _extra
        = new();

    public static ExtendedGameSettingsController Extra(this GameSettingsController menu)
        => _extra.GetOrCreateValue(menu);
}

public class ExtendedGameSettingsController
{
    public bool BuildTesterMode { get; set; }

    public void SetBuildTesterMode(bool buildTesterMode)
    {
        BuildTesterMode = buildTesterMode;
        PlayerPrefsManager.SetInt("Accessibility_build_tester_mode", buildTesterMode ? 1 : 0);
    }
}