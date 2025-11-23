using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace BuildTesterMode.Patches;

internal static class SettingsMenuPatch
{
    [HarmonyPatch(typeof(SettingsMenu), "Awake")]
    [HarmonyPostfix]
    static void Awake(SettingsMenu __instance)
    {
        // Add to default settings
        foreach (var customSetting in CustomSettingsManager.CustomSettings)
        {
            // TODO: handle location + page
            customSetting.InjectControl(__instance);

            customSetting.SetDefaultSnapshot(__instance.defaultSettings.Extra());
        }
    }

    [HarmonyPatch(typeof(SettingsMenu), "Open")]
    [HarmonyPrefix]
    static void Open(SettingsMenu __instance)
    {
        foreach (var customSetting in CustomSettingsManager.CustomSettings)
        {
            customSetting.UpdateControlState();
        }
    }

    [HarmonyPatch(typeof(SettingsMenu), "CreateSettingsSnapshot")]
    [HarmonyPostfix]
    static void CreateSettingsSnapshot(SettingsMenu __instance)
    {
        SettingsSnapshot snapshot = Traverse.Create(__instance)
                .Field("rollBackSnapshot")
                .GetValue<SettingsSnapshot>();

        if (snapshot == null)
            return;

        var extra = snapshot.Extra();

        foreach (var customSetting in CustomSettingsManager.CustomSettings)
        {
            customSetting.SetRollbackSnapshot(extra);
        }
    }

    [HarmonyPatch(typeof(SettingsMenu), "ApplySnapshot")]
    [HarmonyPostfix]
    static void ApplySnapshot(SettingsMenu __instance, SettingsSnapshot snapshot)
    {
        int currentIndex = Traverse.Create(__instance)
                .Field("currentPageIndex")
                .GetValue<int>();

        foreach (var customSetting in CustomSettingsManager.CustomSettings)
        {
            int pageIndex = CustomSettingsManager.Pages.FindIndex((page) => page == customSetting.Page);
            if (pageIndex == -1)
            {
                Debug.Log($"Custom setting {customSetting.Name} on invalid page {customSetting.Page}");
                continue;
            }
            if (pageIndex != currentIndex)
            {
                continue;
            }

            customSetting.ApplySnapshot(snapshot.Extra());
        }
    }
}


public class ExtendedSettingsSnapshot
{
    public Dictionary<string, object> CustomSettings { get; set; } = new();
}

public static class SettingsSnapshotExtensions
{
    private static readonly ConditionalWeakTable<SettingsSnapshot, ExtendedSettingsSnapshot> _extra
        = new();

    public static ExtendedSettingsSnapshot Extra(this SettingsSnapshot snapshot)
        => _extra.GetOrCreateValue(snapshot);
}

public static class SettingsMenuExtensions
{
    public static void EnableRevertSettings(this SettingsMenu menu)
    {
        int currentPageIndex = Traverse.Create(menu)
                               .Field("currentPageIndex")
                               .GetValue<int>();

        Traverse.Create(menu)
            .Method("EnableRevertSettings", currentPageIndex)
            .GetValue();
    }
}