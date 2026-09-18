using System.Windows.Media;

namespace WinCleaner.Design
{
    /// <summary>
    /// Semantic color palette - no hardcoded colors in views.
    /// All colors referenced by semantic name for theming support.
    /// </summary>
    public static class ColorPalette
    {
        // Primary brand
        public static readonly Color Primary = Color.FromRgb(0x00, 0x78, 0xD4);
        public static readonly Color PrimaryHover = Color.FromRgb(0x00, 0x5A, 0x9E);
        public static readonly Color PrimaryPressed = Color.FromRgb(0x00, 0x45, 0x78);
        public static readonly Color PrimaryLight = Color.FromRgb(0x40, 0x9C, 0xFF);

        // Semantic status colors
        public static readonly Color Success = Color.FromRgb(0x10, 0x7C, 0x10);
        public static readonly Color SuccessLight = Color.FromRgb(0xD4, 0xF4, 0xD4);
        public static readonly Color Warning = Color.FromRgb(0xBF, 0x8B, 0x00);
        public static readonly Color WarningLight = Color.FromRgb(0xFF, 0xF3, 0xCD);
        public static readonly Color Error = Color.FromRgb(0xD1, 0x34, 0x38);
        public static readonly Color ErrorLight = Color.FromRgb(0xFA, 0xDB, 0xDB);
        public static readonly Color Info = Color.FromRgb(0x00, 0x78, 0xD4);
        public static readonly Color InfoLight = Color.FromRgb(0xD4, 0xE6, 0xF1);

        // Neutral grays (light theme base)
        public static readonly Color Gray50 = Color.FromRgb(0xF3, 0xF3, 0xF3);
        public static readonly Color Gray100 = Color.FromRgb(0xE1, 0xE1, 0xE1);
        public static readonly Color Gray200 = Color.FromRgb(0xC8, 0xC8, 0xC8);
        public static readonly Color Gray300 = Color.FromRgb(0xA6, 0xA6, 0xA6);
        public static readonly Color Gray400 = Color.FromRgb(0x8A, 0x88, 0x86);
        public static readonly Color Gray500 = Color.FromRgb(0x60, 0x5E, 0x5C);
        public static readonly Color Gray600 = Color.FromRgb(0x48, 0x46, 0x44);
        public static readonly Color Gray700 = Color.FromRgb(0x32, 0x31, 0x30);
        public static readonly Color Gray800 = Color.FromRgb(0x20, 0x1F, 0x1E);
        public static readonly Color Gray900 = Color.FromRgb(0x1B, 0x1A, 0x19);

        // Surface colors (light theme)
        public static readonly Color Surface = Color.FromRgb(0xFF, 0xFF, 0xFF);
        public static readonly Color SurfaceHover = Color.FromRgb(0xF3, 0xF2, 0xF1);
        public static readonly Color SurfacePressed = Color.FromRgb(0xE1, 0xDF, 0xDD);
        public static readonly Color SurfaceVariant = Color.FromRgb(0xF3, 0xF2, 0xF1);

        // Text colors (light theme)
        public static readonly Color TextPrimary = Color.FromRgb(0x1B, 0x1A, 0x19);
        public static readonly Color TextSecondary = Color.FromRgb(0x32, 0x31, 0x30);
        public static readonly Color TextDisabled = Color.FromRgb(0x8A, 0x88, 0x86);
        public static readonly Color TextOnPrimary = Color.FromRgb(0xFF, 0xFF, 0xFF);

        // Border/divider
        public static readonly Color Border = Color.FromRgb(0xE1, 0xE1, 0xE1);
        public static readonly Color BorderStrong = Color.FromRgb(0xC8, 0xC8, 0xC8);
        public static readonly Color Divider = Color.FromRgb(0xED, 0xEB, 0xE9);

        // Shadow
        public static readonly Color Shadow = Color.FromArgb(0x1A, 0x00, 0x00, 0x00);
        public static readonly Color ShadowStrong = Color.FromArgb(0x33, 0x00, 0x00, 0x00);

        /// <summary>
        /// Get brush for light theme
        /// </summary>
        public static SolidColorBrush GetBrush(Color color) => new SolidColorBrush(color);

        /// <summary>
        /// Get brush with opacity
        /// </summary>
        public static SolidColorBrush GetBrush(Color color, byte opacity)
        {
            var c = color;
            c.A = opacity;
            return new SolidColorBrush(c);
        }
    }

    /// <summary>
    /// Dark theme color overrides
    /// </summary>
    public static class DarkColorPalette
    {
        // Primary brand (same)
        public static readonly Color Primary = Color.FromRgb(0x40, 0x9C, 0xFF);
        public static readonly Color PrimaryHover = Color.FromRgb(0x6A, 0xB5, 0xFF);
        public static readonly Color PrimaryPressed = Color.FromRgb(0x26, 0x85, 0xFF);
        public static readonly Color PrimaryLight = Color.FromRgb(0x80, 0xC0, 0xFF);

        // Semantic status (adjusted for dark)
        public static readonly Color Success = Color.FromRgb(0x4C, 0xAF, 0x50);
        public static readonly Color SuccessLight = Color.FromRgb(0x1B, 0x3A, 0x1B);
        public static readonly Color Warning = Color.FromRgb(0xFF, 0xB3, 0x00);
        public static readonly Color WarningLight = Color.FromRgb(0x3D, 0x2F, 0x00);
        public static readonly Color Error = Color.FromRgb(0xEF, 0x53, 0x50);
        public static readonly Color ErrorLight = Color.FromRgb(0x3D, 0x1A, 0x1A);
        public static readonly Color Info = Color.FromRgb(0x40, 0x9C, 0xFF);
        public static readonly Color InfoLight = Color.FromRgb(0x1A, 0x2A, 0x3D);

        // Neutral grays (dark theme)
        public static readonly Color Gray50 = Color.FromRgb(0x1B, 0x1A, 0x19);
        public static readonly Color Gray100 = Color.FromRgb(0x20, 0x1F, 0x1E);
        public static readonly Color Gray200 = Color.FromRgb(0x32, 0x31, 0x30);
        public static readonly Color Gray300 = Color.FromRgb(0x48, 0x46, 0x44);
        public static readonly Color Gray400 = Color.FromRgb(0x60, 0x5E, 0x5C);
        public static readonly Color Gray500 = Color.FromRgb(0x8A, 0x88, 0x86);
        public static readonly Color Gray600 = Color.FromRgb(0xA6, 0xA6, 0xA6);
        public static readonly Color Gray700 = Color.FromRgb(0xC8, 0xC8, 0xC8);
        public static readonly Color Gray800 = Color.FromRgb(0xE1, 0xE1, 0xE1);
        public static readonly Color Gray900 = Color.FromRgb(0xF3, 0xF3, 0xF3);

        // Surface colors (dark theme)
        public static readonly Color Surface = Color.FromRgb(0x1B, 0x1A, 0x19);
        public static readonly Color SurfaceHover = Color.FromRgb(0x20, 0x1F, 0x1E);
        public static readonly Color SurfacePressed = Color.FromRgb(0x32, 0x31, 0x30);
        public static readonly Color SurfaceVariant = Color.FromRgb(0x20, 0x1F, 0x1E);

        // Text colors (dark theme)
        public static readonly Color TextPrimary = Color.FromRgb(0xF3, 0xF3, 0xF3);
        public static readonly Color TextSecondary = Color.FromRgb(0xE1, 0xE1, 0xE1);
        public static readonly Color TextDisabled = Color.FromRgb(0x60, 0x5E, 0x5C);
        public static readonly Color TextOnPrimary = Color.FromRgb(0x1B, 0x1A, 0x19);

        // Border/divider
        public static readonly Color Border = Color.FromRgb(0x32, 0x31, 0x30);
        public static readonly Color BorderStrong = Color.FromRgb(0x48, 0x46, 0x44);
        public static readonly Color Divider = Color.FromRgb(0x2A, 0x29, 0x28);

        // Shadow
        public static readonly Color Shadow = Color.FromArgb(0x33, 0x00, 0x00, 0x00);
        public static readonly Color ShadowStrong = Color.FromArgb(0x66, 0x00, 0x00, 0x00);
    }
}