using MudBlazor;

namespace HAMS.WebHost;

/// <summary>
/// One deliberately-designed palette (deep navy primary, teal accent — a professional, institutional
/// feel suited to a government school system) shared by both halves of the UI: MudBlazor's
/// interactive admin shell reads this directly, and the plain-HTML static login pages mirror the
/// same hex values in <c>wwwroot/css/hams.css</c>'s custom properties, so the app reads as one
/// consistent product rather than two different-looking surfaces.
/// </summary>
public static class HamsTheme
{
    public static readonly MudTheme Default = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#1E3A5F",
            PrimaryDarken = "#152943",
            PrimaryLighten = "#2E5480",
            Secondary = "#0F766E",
            Tertiary = "#0F766E",
            AppbarBackground = "#1E3A5F",
            AppbarText = "#FFFFFF",
            Background = "#F4F6F8",
            Surface = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#1F2937",
            DrawerIcon = "#1E3A5F",
            TextPrimary = "#1F2937",
            TextSecondary = "#5B6472",
            Success = "#15803D",
            Error = "#B91C1C",
            Warning = "#B45309",
            Info = "#1D4ED8",
            LinesDefault = "#E2E5E9",
            TableLines = "#E2E5E9",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = ["Segoe UI", "Roboto", "Helvetica Neue", "Arial", "sans-serif"] },
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
        },
    };
}
