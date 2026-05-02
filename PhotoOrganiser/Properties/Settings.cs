using System.Configuration;

namespace PhotoOrganiser.Properties;

internal sealed class Settings : ApplicationSettingsBase
{
    private static readonly Settings _default = (Settings)Synchronized(new Settings());

    public static Settings Default => _default;

    [UserScopedSetting]
    [DefaultSettingValue("")]
    public string SourceFolder
    {
        get => (string)this[nameof(SourceFolder)];
        set => this[nameof(SourceFolder)] = value;
    }

    [UserScopedSetting]
    [DefaultSettingValue("")]
    public string DestinationFolder
    {
        get => (string)this[nameof(DestinationFolder)];
        set => this[nameof(DestinationFolder)] = value;
    }

    [UserScopedSetting]
    [DefaultSettingValue("")]
    public string WindowBounds
    {
        get => (string)this[nameof(WindowBounds)];
        set => this[nameof(WindowBounds)] = value;
    }

    [UserScopedSetting]
    [DefaultSettingValue("false")]
    public bool WindowMaximised
    {
        get => (bool)this[nameof(WindowMaximised)];
        set => this[nameof(WindowMaximised)] = value;
    }
}
