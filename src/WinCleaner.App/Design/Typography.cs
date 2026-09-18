using System;
using System.Windows;
using System.Windows.Media;

namespace WinCleaner.Design
{
    /// <summary>
    /// Typography scale - consistent text sizing across the app.
    /// Uses Segoe UI Variable / Segoe UI with semantic size names.
    /// </summary>
    public static class Typography
    {
        public const string FontFamily = "Segoe UI Variable, Segoe UI, system-ui";
        public const string FontFamilyMono = "Cascadia Code, Consolas, monospace";

        // Display sizes
        public const double DisplayLarge = 48;
        public const double DisplayMedium = 36;
        public const double DisplaySmall = 28;

        // Heading sizes
        public const double HeadingLarge = 24;
        public const double HeadingMedium = 20;
        public const double HeadingSmall = 18;

        // Title sizes
        public const double TitleLarge = 16;
        public const double TitleMedium = 14;
        public const double TitleSmall = 13;

        // Body sizes
        public const double BodyLarge = 14;
        public const double BodyMedium = 13;
        public const double BodySmall = 12;

        // Label sizes
        public const double LabelLarge = 12;
        public const double LabelMedium = 11;
        public const double LabelSmall = 10;

        // Font weights (static readonly since FontWeight is a struct, not const-able)
        public static readonly FontWeight WeightLight = FontWeights.Light;
        public static readonly FontWeight WeightRegular = FontWeights.Regular;
        public static readonly FontWeight WeightMedium = FontWeights.Medium;
        public static readonly FontWeight WeightSemiBold = FontWeights.SemiBold;
        public static readonly FontWeight WeightBold = FontWeights.Bold;

        // Line heights (multipliers)
        public const double LineHeightTight = 1.2;
        public const double LineHeightNormal = 1.5;
        public const double LineHeightRelaxed = 1.6;

        /// <summary>
        /// Create a TextBlock style with semantic typography
        /// </summary>
        public static Style CreateTextStyle(double size, FontWeight weight = default, double lineHeight = LineHeightNormal, Color? color = null)
        {
            if (weight == default)
                weight = WeightRegular;

            var style = new Style(typeof(System.Windows.Controls.TextBlock));
            style.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontFamilyProperty, new FontFamily(FontFamily)));
            style.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontSizeProperty, size));
            style.Setters.Add(new Setter(System.Windows.Controls.TextBlock.FontWeightProperty, weight));
            style.Setters.Add(new Setter(System.Windows.Controls.TextBlock.LineHeightProperty, size * lineHeight));
            if (color.HasValue)
                style.Setters.Add(new Setter(System.Windows.Controls.TextBlock.ForegroundProperty, new SolidColorBrush(color.Value)));
            return style;
        }
    }
}