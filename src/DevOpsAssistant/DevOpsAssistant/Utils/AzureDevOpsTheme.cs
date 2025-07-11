using MudBlazor;

namespace DevOpsAssistant.Utils;

public static class AzureDevOpsTheme
{
    public static MudTheme Theme { get; } = new MudTheme
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0078D4",
            Secondary = "#005A9E",
            Success = "#107C10",
            Warning = "#FFD400",
            Error = "#D13438",
            AppbarText = Colors.Shades.White
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#3AA0F3",
            Secondary = "#2899F5",
            Background = "#1F1F1F",
            Surface = "#252526",
            AppbarBackground = "#333333",
            TextPrimary = Colors.Shades.White,
            DrawerText = Colors.Shades.White,
            DrawerIcon = Colors.Shades.White
        }
    };
}
