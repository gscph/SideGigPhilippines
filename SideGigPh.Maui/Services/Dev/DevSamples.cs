namespace SideGigPh.Maui.Services.Dev;

/// <summary>
/// Whether the stubs add SAMPLE inquiries and reviews so the Pro home can be seen filled in.
/// On in Debug builds only: a Release build never shows a real pro invented activity.
/// </summary>
public static class DevSamples
{
#if DEBUG
    public static readonly bool Enabled = true;
#else
    public static readonly bool Enabled = false;
#endif
}
