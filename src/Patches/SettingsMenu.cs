using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using TMPro;

namespace BuildTesterMode.Patches;

internal static class SettingsMenuPatch
{
    [HarmonyPatch(typeof(SettingsMenu), "Awake")]
    [HarmonyPostfix]
    static void Patch_Awake(SettingsMenu __instance)
    {
        // Adding additional settings
        var extra = __instance.Extra();

        MenuListItemToggle menuItem = __instance.GetComponentsInChildren<MenuListItemToggle>(true)
            .FirstOrDefault(m => m.name == "MenuItem_ColorblindAether");

        GameObject duplicate = Object.Instantiate(menuItem.gameObject);
        duplicate.name = "MenuItem_BuildTesterMode";
        duplicate.transform.position = new Vector3(duplicate.transform.position.x, -165, duplicate.transform.position.z);
        MenuListItemToggle newToggle = duplicate.GetComponent<MenuListItemToggle>();
        newToggle.ItemDescription = "Turns on Build Testing mode; All available monsters in Monster Shrines, Infinite rerolls";

        TextMeshPro text = duplicate.GetComponentInChildren<TextMeshPro>();
        text.text = "Build Tester Mode";

        // Disable all persistent listeners
        for (int i = 0; i < newToggle.OnToggle.GetPersistentEventCount(); i++)
        {
            newToggle.OnToggle.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
        }

        newToggle.OnToggle.AddListener((value) =>
        {
            __instance.Extra().SetAccessibilityBuildTesterMode(value);
        });
        newToggle.OnToggle.AddListener((value) =>
        {
            duplicate.GetComponent<WwiseSFX>().PlayEventByName("Play_SFX_menu_toggle");
        });

        duplicate.transform.SetParent(menuItem.gameObject.transform.parent, false);
        duplicate.transform.SetSiblingIndex(menuItem.gameObject.transform.GetSiblingIndex() + 1);

        // Add to paging header
        PagingHeader header = __instance.GetComponentsInChildren<PagingHeader>(true)
            .FirstOrDefault(m => m.name == "Header_Accessibility");
        FieldInfo field = typeof(PagingHeader).GetField("pageMenuItems", BindingFlags.NonPublic | BindingFlags.Instance);
        List<MenuListItem> oldArray = (field.GetValue(header) as MenuListItem[]).ToList();
        oldArray.Insert(5, newToggle);
        field.SetValue(header, oldArray.ToArray());

        extra.AccessibilityBuildTesterMode = newToggle;

        // Add to default settings
        __instance.defaultSettings.Extra().BuildTesterMode = false;
    }

    [HarmonyPatch(typeof(SettingsMenu), "Open")]
    [HarmonyPrefix]
    static void Patch_Open(SettingsMenu __instance)
    {
        var extra = __instance.Extra();
        extra.AccessibilityBuildTesterMode.SetState(GameSettingsController.Instance.Extra().BuildTesterMode, shouldFireEvent: false);
        extra.AccessibilityBuildTesterMode.SetDisabled(!GameStateManager.Instance.IsMainMenu && ExplorationController.Instance.CurrentArea != EArea.PilgrimsRest);
    }

    [HarmonyPatch(typeof(SettingsMenu), "CreateSettingsSnapshot")]
    [HarmonyPostfix]
    static void Patch_CreateSettingsSnapshot(SettingsMenu __instance)
    {
        SettingsSnapshot snapshot = Traverse.Create(__instance)
                .Field("rollBackSnapshot")
                .GetValue<SettingsSnapshot>();

        if (snapshot == null)
            return;

        var extra = snapshot.Extra();
        extra.BuildTesterMode = GameSettingsController.Instance.Extra().BuildTesterMode;
    }

    [HarmonyPatch(typeof(SettingsMenu), "ApplySnapshot")]
    [HarmonyPostfix]
    static void Patch_ApplySnapshot(SettingsMenu __instance, SettingsSnapshot snapshot)
    {
        int index = Traverse.Create(__instance)
                .Field("currentPageIndex")
                .GetValue<int>();

        // 4 is the Accessibility page
        if (index == 4)
        {
            if (GameStateManager.Instance.IsMainMenu || ExplorationController.Instance.CurrentArea == EArea.PilgrimsRest)
            {
                ExtendedSettingsMenu extra = __instance.Extra();
                bool newValue = snapshot.Extra().BuildTesterMode;
                extra.SetAccessibilityBuildTesterMode(newValue);
                extra.AccessibilityBuildTesterMode.SetState(newValue, shouldFireEvent: false);
            }
        }
    }
}

public class CustomSettings
{
    public bool BuildTesterMode { get; set; }
}

public class CustomSetting
{
    public string Name { get; set; }
    public object DefaultValue { get; set; }
    public object Value { get; set; }
}

public static class SettingsSnapshotExtensions
{
    private static readonly ConditionalWeakTable<SettingsSnapshot, CustomSettings> _extra
        = new();

    public static CustomSettings Extra(this SettingsSnapshot snapshot)
        => _extra.GetOrCreateValue(snapshot);
}

public static class SettingsMenuExtensions
{
    private static readonly ConditionalWeakTable<SettingsMenu, ExtendedSettingsMenu> _extra
        = new();

    public static ExtendedSettingsMenu Extra(this SettingsMenu menu)
    {
        var extra = _extra.GetOrCreateValue(menu);
        extra.Instance = menu;
        return extra;
    }
}

public class ExtendedSettingsMenu
{
    public SettingsMenu Instance { get; set; }
    public MenuListItemToggle AccessibilityBuildTesterMode { get; set; }

    public void SetAccessibilityBuildTesterMode(bool buildTesterMode)
    {
        int currentPageIndex = Traverse.Create(Instance)
                                       .Field("currentPageIndex")
                                       .GetValue<int>();

        GameSettingsController.Instance.Extra().SetBuildTesterMode(buildTesterMode);

        Traverse.Create(Instance)
            .Method("EnableRevertSettings", currentPageIndex)
            .GetValue();
    }
}