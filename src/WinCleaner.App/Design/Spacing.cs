namespace WinCleaner.Design
{
    /// <summary>
    /// Spacing scale - consistent spacing using 4px base unit.
    /// All margins, paddings, gaps derived from this scale.
    /// </summary>
    public static class Spacing
    {
        public const double Base = 4;      // 4px
        public const double Xs = 4;        // 4px
        public const double Sm = 8;        // 8px
        public const double Md = 16;       // 16px
        public const double Lg = 24;       // 24px
        public const double Xl = 32;       // 32px
        public const double Xxl = 48;      // 48px
        public const double Xxxl = 64;     // 64px

        // Component-specific
        public const double ControlPadding = 12;
        public const double ControlPaddingSm = 8;
        public const double CardPadding = 16;
        public const double SectionGap = 24;
        public const double InlineGap = 8;
        public const double StackGap = 12;
        public const double GridGap = 16;

        // Border radius
        public const double RadiusSm = 4;
        public const double RadiusMd = 6;
        public const double RadiusLg = 8;
        public const double RadiusXl = 12;
        public const double RadiusFull = 9999;

        // Shadows
        public const double ShadowSm = 1;
        public const double ShadowMd = 4;
        public const double ShadowLg = 8;
        public const double ShadowXl = 16;
    }
}