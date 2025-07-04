using MudBlazor;

namespace DevOpsAssistant.Utils;

public static class ThemeExtensions
{
    public static MudTheme WithPrimaryColor(this MudTheme theme, string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            return theme;

        return new MudTheme
        {
            LayoutProperties = theme.LayoutProperties,
            Typography = theme.Typography,
            Shadows = theme.Shadows,
            ZIndex = theme.ZIndex,
            PaletteLight = new PaletteLight
            {
                Primary = color,
                Secondary = theme.PaletteLight.Secondary,
                Tertiary = theme.PaletteLight.Tertiary,
                Background = theme.PaletteLight.Background,
                Surface = theme.PaletteLight.Surface,
                Info = theme.PaletteLight.Info,
                Success = theme.PaletteLight.Success,
                Warning = theme.PaletteLight.Warning,
                Error = theme.PaletteLight.Error,
                TextPrimary = theme.PaletteLight.TextPrimary,
                TextSecondary = theme.PaletteLight.TextSecondary,
                AppbarText = theme.PaletteLight.AppbarText
            },
            PaletteDark = new PaletteDark
            {
                Primary = color,
                Secondary = theme.PaletteDark.Secondary,
                Tertiary = theme.PaletteDark.Tertiary,
                Background = theme.PaletteDark.Background,
                Surface = theme.PaletteDark.Surface,
                Info = theme.PaletteDark.Info,
                Success = theme.PaletteDark.Success,
                Warning = theme.PaletteDark.Warning,
                Error = theme.PaletteDark.Error,
                LinesDefault = theme.PaletteDark.LinesDefault,
                AppbarBackground = theme.PaletteDark.AppbarBackground,
                TextPrimary = theme.PaletteDark.TextPrimary,
                TextSecondary = theme.PaletteDark.TextSecondary,
                DrawerText = theme.PaletteDark.DrawerText,
                DrawerIcon = theme.PaletteDark.DrawerIcon,
                Dark = theme.PaletteDark.Dark
            }
        };
    }
}
