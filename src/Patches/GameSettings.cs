using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace BuildTesterMode.Patches;

internal static class GameSettingsControllerPatch
{
    [HarmonyPatch(typeof(GameSettingsController), "LoadSettings")]
    [HarmonyPostfix]
    private static void LoadSettings(GameSettingsController __instance)
    {
        var custom = __instance.Extension();
        foreach (var setting in CustomSettingsManager.CustomSettings)
        {
            setting.InitializeValue(custom);
        }
    }
}

public static class GameSettingsControllerExtensions
{
    private static readonly ConditionalWeakTable<GameSettingsController, ExtendedGameSettingsController> _extra
        = new();

    public static ExtendedGameSettingsController Extension(this GameSettingsController menu)
        => _extra.GetOrCreateValue(menu);

    public static T GetCustom<T>(this GameSettingsController menu, string setting)
        => _extra.GetOrCreateValue(menu).Get<T>(setting);
}

public class ExtendedGameSettingsController
{
    public Dictionary<string, object> CustomSettings { get; } = new();

    public T Get<T>(string setting)
    {
        if (CustomSettings.TryGetValue(setting, out object value))
        {
            if (value is T typedValue)
            {
                return typedValue;
            }
            else
            {
                Debug.LogError($"Custom setting {setting} is not of type {typeof(T)}.");
                return default;
            }
        }
        else
        {
            Debug.LogError($"Custom setting {setting} does not have stored value.");
            return default;
        }
    }

    public void Set(string setting, object value)
    {
        CustomSettings[setting] = value;
    }
}