using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace BuildTesterMode.Patches;

internal static class SettingsMenuPatch
{
    [HarmonyPatch(typeof(SettingsMenu), "Awake")]
    [HarmonyPrefix]
    static void Awake(SettingsMenu __instance)
    {
        // Initializing cache
        SettingsPageCache cache = new();
        cache.CachedPosition["General"] = -199;
        cache.CachedPosition["Input"] = -210;
        cache.CachedPosition["Audio"] = -165;
        cache.CachedPosition["Video"] = -110;
        cache.CachedPosition["Accessibility"] = -165;

        // Inject new settings to menu
        foreach (var customSetting in CustomSettingsManager.CustomSettings)
        {
            int pageIndex = CustomSettingsManager.Pages.FindIndex((page) => page == customSetting.Page);
            Transform pageMenuRoot;
            PagingHeader header;
            // Unrecognized page, creating new tab
            if (pageIndex == -1)
            {
                (pageMenuRoot, header) = UIHelper.CreateNewSettingsPage(__instance, customSetting.Page, cache);
            }
            else
            {
                (pageMenuRoot, header) = UIHelper.GetSettingsPage(__instance, customSetting.Page);
            }

            (MenuListItem newControl, float height) = customSetting.BuildControl(__instance);

            newControl.transform.position = new Vector3(newControl.transform.position.x, cache.CachedPosition[customSetting.Page], newControl.transform.position.z);
            cache.CachedPosition[customSetting.Page] -= height;

            newControl.transform.SetParent(pageMenuRoot, false);
            newControl.transform.SetSiblingIndex(pageMenuRoot.childCount - 3);

            // Add to paging header
            FieldInfo field = typeof(PagingHeader).GetField("pageMenuItems", BindingFlags.NonPublic | BindingFlags.Instance);
            List<MenuListItem> oldArray = (field.GetValue(header) as MenuListItem[]).ToList();
            oldArray.Insert(oldArray.Count - 3, newControl);
            field.SetValue(header, oldArray.ToArray());

            // Setting up default value
            customSetting.SetDefaultSnapshot(__instance.defaultSettings.Extra());
        }

        // Fixing header widths
        int totalPages = CustomSettingsManager.Pages.Count;
        float currentSpacing = 95; // Width between headers
        float requiredSpacing = 5 * currentSpacing / totalPages;
        float currentWidth = 85; // Width of labels
        float requiredWidth = 5 * currentWidth / totalPages;
        float currentBorder = 96;
        float requiredBorder = 5 * currentBorder / totalPages;
        for (int i = 0; i < totalPages; i++)
        {
            (_, PagingHeader header) = UIHelper.GetSettingsPage(__instance, CustomSettingsManager.Pages[i]);
            header.transform.localPosition = new Vector3(30 + i * requiredSpacing, header.transform.localPosition.y, header.transform.localPosition.z);
            RectTransform labelTransform = header.transform.GetChild(0).transform as RectTransform;
            labelTransform.sizeDelta = new Vector2(requiredWidth, labelTransform.sizeDelta.y);
            var border = header.transform.GetChild(1).GetChild(0).GetComponent<SpriteRenderer>();
            border.size = new Vector2(requiredBorder, border.size.y);
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
                Debug.LogError($"Custom setting {customSetting.Name} on invalid page {customSetting.Page}");
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