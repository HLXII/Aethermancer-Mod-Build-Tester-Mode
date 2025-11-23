
using System.Linq;
using BuildTesterMode.Patches;
using TMPro;
using UnityEngine;

namespace BuildTesterMode;

public interface ICustomSetting
{
    public string Page { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public void SetDefaultSnapshot(ExtendedSettingsSnapshot defaultSettings);
    public void SetRollbackSnapshot(ExtendedSettingsSnapshot rollbackSnapshot);
    public void ApplySnapshot(ExtendedSettingsSnapshot snapshot);
    public void InitializeValue(ExtendedGameSettingsController controller);
    public (MenuListItem, float) BuildControl(SettingsMenu menu);
    public void UpdateControlState();
}

public interface IBasicCustomSetting<T> : ICustomSetting
{
    public string Key { get; set; }
    public T DefaultValue { get; set; }
    public abstract void SetValue(ExtendedGameSettingsController controller, T value);
}

public class BooleanCustomSetting : IBasicCustomSetting<bool>
{
    public string Page { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Key { get; set; }
    public bool DefaultValue { get; set; }
    public MenuListItemToggle Control { get; set; }
    public System.Func<bool> IsEnabled { get; set; } = () => true;

    public void SetDefaultSnapshot(ExtendedSettingsSnapshot defaultSettings)
    {
        defaultSettings.CustomSettings.Add(Name, DefaultValue);
    }
    public void SetRollbackSnapshot(ExtendedSettingsSnapshot rollbackSnapshot)
    {
        rollbackSnapshot.CustomSettings.Add(Name, GameSettingsController.Instance.GetCustom<bool>(Name));
    }
    public void ApplySnapshot(ExtendedSettingsSnapshot snapshot)
    {
        if (!IsEnabled())
        {
            return;
        }

        bool newValue = (bool)snapshot.CustomSettings[Name];
        SetValue(GameSettingsController.Instance.Extension(), newValue);
        UpdateControlState();
    }

    public void InitializeValue(ExtendedGameSettingsController controller)
    {
        controller.Set(Name, PlayerPrefsManager.GetInt(Key) > 0);
    }

    public (MenuListItem, float) BuildControl(SettingsMenu menu)
    {
        MenuListItemToggle menuItem = menu.GetComponentsInChildren<MenuListItemToggle>(true)
            .FirstOrDefault(m => m.name == "MenuItem_ColorblindAether");

        GameObject newGameObject = Object.Instantiate(menuItem.gameObject);
        newGameObject.name = $"MenuItem_{Name}";
        MenuListItemToggle newToggle = newGameObject.GetComponent<MenuListItemToggle>();
        newToggle.ItemDescription = Description;

        TextMeshPro text = newGameObject.GetComponentInChildren<TextMeshPro>();
        text.text = Name;

        // Disable all persistent listeners
        for (int i = 0; i < newToggle.OnToggle.GetPersistentEventCount(); i++)
        {
            newToggle.OnToggle.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
        }

        newToggle.OnToggle.AddListener((value) =>
        {
            SetValue(GameSettingsController.Instance.Extension(), value);
            menu.EnableRevertSettings();
        });
        newToggle.OnToggle.AddListener((value) =>
        {
            newGameObject.GetComponent<WwiseSFX>().PlayEventByName("Play_SFX_menu_toggle");
        });

        Control = newToggle;
        return (newToggle, 33);
    }

    public void SetValue(ExtendedGameSettingsController controller, bool value)
    {
        controller.Set(Name, value);
        PlayerPrefsManager.SetInt(Key, value ? 1 : 0);
    }

    public void UpdateControlState()
    {
        Control.SetState(GameSettingsController.Instance.GetCustom<bool>(Name), shouldFireEvent: false);
        Control.SetDisabled(!IsEnabled());
    }
}