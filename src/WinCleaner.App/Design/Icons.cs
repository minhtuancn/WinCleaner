using System.Windows;
using System.Windows.Media;

namespace WinCleaner.Design
{
    /// <summary>
    /// Semantic icon library - vector paths for all icons.
    /// No emoji, no font icons. Pure XAML Geometry for crisp scaling.
    /// Usage: <Path Data="{x:Static design:Icons.IconName}" Fill="{DynamicResource TextPrimaryBrush}" Width="16" Height="16" />
    /// </summary>
    public static class Icons
    {
        // Navigation / UI
        public static readonly Geometry ChevronRight = Parse("M9 18l6-6-6-6");
        public static readonly Geometry ChevronLeft = Parse("M15 18l-6-6 6-6");
        public static readonly Geometry ChevronUp = Parse("M18 15l-6-6-6 6");
        public static readonly Geometry ChevronDown = Parse("M6 9l6 6 6-6");
        public static readonly Geometry Expand = Parse("M15 3h6v6M9 21H3v-6M21 3l-7 7M3 21l7-7");
        public static readonly Geometry Collapse = Parse("M3 9h6V3M9 21V15h6M3 15l7-7 7 7");
        public static readonly Geometry ArrowRight = Parse("M5 12h14M12 5l7 7-7 7");
        public static readonly Geometry ArrowLeft = Parse("M19 12H5M12 19l-7-7 7-7");

        // Actions
        public static readonly Geometry Search = Parse("M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z");
        public static readonly Geometry Settings = Parse("M12.22 2h-.44a2 2 0 00-2 2v.18a2 2 0 01-1 1.73l-.43.25a2 2 0 01-2 0l-.15-.08a2 2 0 00-2.73.73l-.25.43a2 2 0 01-1.73 1H4a2 2 0 00-2 2v.44a2 2 0 002 2h.18a2 2 0 011 1.73l.25.43a2 2 0 00.73 2.73l-.43.15a2 2 0 01-1 1.73V20a2 2 0 002 2h.44a2 2 0 002-2v-.18a2 2 0 011-1.73l.43-.25a2 2 0 012 0l.15.08a2 2 0 002.73-.73l.25-.43a2 2 0 011.73-1h.18a2 2 0 002-2v-.44a2 2 0 00-2-2h-.18a2 2 0 01-1-1.73l-.25-.43a2 2 0 00-.73-2.73l.43-.15a2 2 0 011-1.73V4a2 2 0 00-2-2z");
        public static readonly Geometry Refresh = Parse("M23 4v6h-6M1 20v-6h6M3.51 9a9 9 0 0114.98 0");
        public static readonly Geometry Delete = Parse("M3 6h18M19 6v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2");
        public static readonly Geometry Edit = Parse("M11 4H4a2 2 0 00-2 2v14a2 2 0 002 2h14a2 2 0 002-2v-7M18.5 2.5a2.121 2.121 0 013 3L12 15l-4 1 1-4 9.5-9.5a2.121 2.121 0 013 3z");
        public static readonly Geometry Plus = Parse("M12 5v14M5 12h14");
        public static readonly Geometry Minus = Parse("M5 12h14");
        public static readonly Geometry Check = Parse("M20 6L9 17l-5-5");
        public static readonly Geometry X = Parse("M18 6L6 18M6 6l12 12");
        public static readonly Geometry MoreHorizontal = Parse("M12 12a1 1 0 011 1v4a1 1 0 11-2 0v-4a1 1 0 011-1zM12 4a1 1 0 011 1v4a1 1 0 11-2 0V5a1 1 0 011-1zM12 20a1 1 0 011 1v4a1 1 0 11-2 0v-4a1 1 0 011-1z");

        // Status
        public static readonly Geometry AlertCircle = Parse("M12 22C6.48 22 2 17.52 2 12S6.48 2 12 2s10 4.48 10 10-4.48 10-10 10zM12 6v6l4 2");
        public static readonly Geometry AlertTriangle = Parse("M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0zM12 9v4M12 17h.01");
        public static readonly Geometry Info = Parse("M13 16h-2v-4h2v4zm0-8h-2v8h2V8zM12 22C6.48 22 2 17.52 2 12S6.48 2 12 2s10 4.48 10 10-4.48 10-10 10z");
        public static readonly Geometry Success = Parse("M22 11.08V12a10 10 0 11-5.93-9.14M22 4L12 14.01l-3-3");

        // App specific
        public static readonly Geometry Broom = Parse("M12 2L4 10v8a2 2 0 002 2h12a2 2 0 002-2V10L12 2zM12 6v10M8 10h8");
        public static readonly Geometry Shield = Parse("M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z");
        public static readonly Geometry Monitor = Parse("M22 12V3a2 2 0 00-2-2H4a2 2 0 00-2 2v9M22 21H2M12 7v11");
        public static readonly Geometry Database = Parse("M4 7v11a2 2 0 002 2h12a2 2 0 002-2V7M4 7l8 4 8-4M4 15l8 4 8-4");
        public static readonly Geometry Folder = Parse("M22 19a2 2 0 01-2 2H4a2 2 0 01-2-2V5a2 2 0 012-2h5l2 3h9a2 2 0 012 2z");
        public static readonly Geometry File = Parse("M13 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V9zM13 2v8h8");
        public static readonly Geometry Drive = Parse("M12 2L4 10v8a2 2 0 002 2h12a2 2 0 002-2V10L12 2z");
        public static readonly Geometry Cloud = Parse("M18 10h-1.26A8 8 0 109 20h9a5 5 0 000-10z");
        public static readonly Geometry Download = Parse("M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M7 10l5 5 5-5M12 15V3");
        public static readonly Geometry Upload = Parse("M21 15v4a2 2 0 01-2 2H5a2 2 0 01-2-2v-4M17 8l-5-5-5 5M12 15v12");
        public static readonly Geometry Trash = Parse("M3 6h18M19 6v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2");

        // Categories
        public static readonly Geometry Windows = Parse("M12 2L2 7l10 5 10-5-10-5zM2 17l10 5 10-5M2 12l10 5 10-5");
        public static readonly Geometry Browser = Parse("M23 12l-10 7L1 12l10-7 10 7zM2 12l10 7 10-7");
        public static readonly Geometry Code = Parse("M16 18l6-6-6-6M8 6l-6 6 6 6");
        public static readonly Geometry Game = Parse("M12 2a10 10 0 1010 10A10 10 0 0012 2zM12 6v8M8 10h8");
        public static readonly Geometry TrashBin = Parse("M3 6h18M19 6v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2");

        // Hardware
        public static readonly Geometry Cpu = Parse("M12 2a10 10 0 1010 10A10 10 0 0012 2zm0 18a8 8 0 110-16 8 8 0 010 16z");
        public static readonly Geometry Memory = Parse("M6 4h16v16H6zM8 6h12v2H8zm0 4h12v2H8zm0 4h12v2H8z");
        public static readonly Geometry Gpu = Parse("M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h8a2 2 0 002-2V4a2 2 0 00-2-2zm0 18H6V4h8v16z");
        public static readonly Geometry Disk = Parse("M12 2a10 10 0 1010 10A10 10 0 0012 2zm0 18a8 8 0 110-16 8 8 0 010 16z");

        // Helpers
        private static Geometry Parse(string pathData)
        {
            var geometry = Geometry.Parse(pathData);
            geometry.Freeze(); // Freeze for performance/thread-safety
            return geometry;
        }
    }
}