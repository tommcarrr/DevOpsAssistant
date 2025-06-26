using MudBlazor;

namespace DevOpsAssistant.Utils;

public static class DoomTheme
{
    public static MudTheme Theme { get; } = new MudTheme
    {
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "0px",
        },
        PaletteLight = new PaletteLight
        {
            Primary = "#E53935",
            Secondary = "#FB8C00",
            Tertiary = "#A84300",
            Background = "#F5F5F5",
            Surface = "#EEEEEE",
            Info = "#C2185B",
            Success = "#43A047",
            Warning = "#FFB300",
            Error = "#D32F2F",
            TextPrimary = "#212121",
            TextSecondary = "#616161",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#D32F2F",
            Secondary = "#FF8F00",
            Tertiary = "#A84300",
            Background = "#1B1B1B",
            Surface = "#2E2E2E",
            Info = "#C2185B",
            Success = "#388E3C",
            Warning = "#FBC02D",
            Error = "#B71C1C",
            Dark = "#000000",
            TextPrimary = "#E0E0E0",
            TextSecondary = "#9E9E9E",
            LinesDefault = "#4F4F4F",
            AppbarBackground = "#000000",
            AppbarText = "#FFD600",
            DrawerBackground = "#1F1F1F",
            DrawerText = "#E0E0E0",
            DrawerIcon = "#9E9E9E"
        },
        Shadows = new Shadow
        {
            Elevation = new[]
            {
                "none",
                "0px 4px 2px -2px rgba(0,0,0,0.5)",
                "0px 6px 3px -3px rgba(0,0,0,0.5)",
            }
        }
    };
}
