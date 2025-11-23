using System.Collections.Generic;

namespace BuildTesterMode;

public static class CustomSettingsManager
{
    public static List<string> Pages = [
        "General",
        "Input",
        "Audio",
        "Video",
        "Accessibility"
    ];

    public static List<ICustomSetting> CustomSettings { get; set; } = [];
}