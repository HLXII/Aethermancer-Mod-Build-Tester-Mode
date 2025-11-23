using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace BuildTesterMode;

public class SettingsPageCache
{
    public Dictionary<string, float> CachedPosition { get; set; } = new();
}

public static class UIHelper
{
    public static (Transform, PagingHeader) CreateNewSettingsPage(SettingsMenu menu, string page, SettingsPageCache cache)
    {
        int pageIndex = CustomSettingsManager.Pages.Count;
        CustomSettingsManager.Pages.Add(page);

        // Adding page
        GameObject pageObject = menu.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(m => m.name == "Page_Accessibility").gameObject;
        GameObject newPageObject = Object.Instantiate(pageObject);
        newPageObject.transform.SetParent(pageObject.transform.parent, false);
        newPageObject.transform.SetSiblingIndex(pageIndex);

        newPageObject.name = $"Page_{page}";
        // Removing existing menu items from duplication
        int backItemIndex = newPageObject.GetComponentsInChildren<Transform>()
            .First(menu => menu.gameObject.name == "MenuItem_Back").GetSiblingIndex();
        for (int i = backItemIndex - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(newPageObject.transform.GetChild(0).GetChild(i).gameObject);
        }

        // Adding header
        GameObject headerObject = menu.GetComponentsInChildren<PagingHeader>(true)
            .FirstOrDefault(m => m.name == $"Header_Accessibility").gameObject;
        GameObject newHeaderObject = Object.Instantiate(headerObject);
        newHeaderObject.transform.SetParent(headerObject.gameObject.transform.parent, false);
        newHeaderObject.transform.SetSiblingIndex(pageIndex + 1);

        newHeaderObject.name = $"Header_{page}";
        PagingHeader newHeader = newHeaderObject.GetComponent<PagingHeader>();

        var tr = Traverse.Create(newHeader);
        tr.Field("pageContainer").SetValue(newPageObject);
        tr.Field("pageMenu").SetValue(newPageObject.GetComponent<MenuList>());
        tr.Field("pageMenuItems").SetValue(newPageObject.GetComponentsInChildren<MenuListItem>().ToArray());

        MouseEventHandler mouseEventHandler = newHeaderObject.GetComponent<MouseEventHandler>();
        for (int i = 0; i < mouseEventHandler.OnMouseClicked.GetPersistentEventCount(); i++)
        {
            mouseEventHandler.OnMouseClicked.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
        }
        mouseEventHandler.OnMouseClicked.AddListener(() =>
        {
            menu.OnPageIndexClicked(pageIndex);
        });

        TextMeshPro text = newHeaderObject.GetComponentInChildren<TextMeshPro>();
        text.text = page;

        // Update cache position
        cache.CachedPosition[page] = 0;

        // Inject into SettingsMenu
        menu.Pages = [.. menu.Pages, newHeader];
        menu.RevertChangesButtons = [.. menu.RevertChangesButtons, newPageObject.GetComponentsInChildren<MenuListItem>().First(menu => menu.gameObject.name == "MenuItem_Revert")];

        return (newPageObject.transform.GetChild(0), newHeader);
    }

    public static (Transform, PagingHeader) GetSettingsPage(SettingsMenu menu, string page)
    {
        GameObject pageObject = menu.GetComponentsInChildren<MenuList>(true)
            .FirstOrDefault(m => m.name == $"Page_{page}").gameObject;
        PagingHeader header = menu.GetComponentsInChildren<PagingHeader>(true)
            .FirstOrDefault(m => m.name == $"Header_{page}");
        return (pageObject.transform.GetChild(0), header);
    }
}