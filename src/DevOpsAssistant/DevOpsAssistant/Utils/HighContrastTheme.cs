using MudBlazor;

namespace DevOpsAssistant.Utils;

public static class HighContrastTheme
{
    public static MudTheme Theme { get; } = new MudTheme
    {
        PaletteLight = new PaletteLight
        {
            Primary = Colors.Shades.Black,
            Secondary = Colors.Shades.White,
            Background = Colors.Shades.White,
            Surface = Colors.Shades.White,
            AppbarBackground = Colors.Shades.White,
            AppbarText = Colors.Shades.Black,
            TextPrimary = Colors.Shades.Black,
            TextSecondary = Colors.Shades.Black
        },
        PaletteDark = new PaletteDark
        {
            Primary = Colors.Shades.White,
            Secondary = Colors.Shades.Black,
            Background = Colors.Shades.Black,
            Surface = Colors.Shades.Black,
            AppbarBackground = Colors.Shades.Black,
            TextPrimary = Colors.Shades.White,
            TextSecondary = Colors.Shades.White,
            DrawerBackground = Colors.Shades.Black,
            DrawerText = Colors.Shades.White,
            DrawerIcon = Colors.Shades.White
        }
    };
}
