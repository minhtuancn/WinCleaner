using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WinCleaner.Models;

namespace WinCleaner.Models
{
    public class BytesToStringConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is long bytes)
            {
                string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
                int i = 0;
                double dblBytes = bytes;
                while (dblBytes >= 1024 && i < suffixes.Length - 1)
                {
                    dblBytes /= 1024;
                    i++;
                }
                return $"{dblBytes:0.##} {suffixes[i]}";
            }
            return "0 B";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class IntToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int intVal)
            {
                string? param = parameter as string;
                if (param == "gt0")
                    return intVal > 0 ? Visibility.Visible : Visibility.Collapsed;
                if (param == "eq0")
                    return intVal == 0 ? Visibility.Visible : Visibility.Collapsed;
                return intVal > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RiskLevelToBrushConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ItemRiskLevel risk)
            {
                return risk switch
                {
                    ItemRiskLevel.Safe => Application.Current.FindResource("RiskSafeBrush"),
                    ItemRiskLevel.Low => Application.Current.FindResource("RiskLowBrush"),
                    ItemRiskLevel.Medium => Application.Current.FindResource("RiskMediumBrush"),
                    ItemRiskLevel.High => Application.Current.FindResource("RiskHighBrush"),
                    ItemRiskLevel.Critical => Application.Current.FindResource("RiskCriticalBrush"),
                    _ => Application.Current.FindResource("TextPrimaryBrush")
                };
            }
            return Application.Current.FindResource("TextPrimaryBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CleanStatusToBrushConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is CleanStatus status)
            {
                return status switch
                {
                    CleanStatus.Cleaned => Application.Current.FindResource("SuccessBrush"),
                    CleanStatus.Cleaning => Application.Current.FindResource("PrimaryBrush"),
                    CleanStatus.Failed => Application.Current.FindResource("ErrorBrush"),
                    CleanStatus.Skipped => Application.Current.FindResource("WarningBrush"),
                    CleanStatus.Scanned => Application.Current.FindResource("TextSecondaryBrush"),
                    _ => Application.Current.FindResource("TextSecondaryBrush")
                };
            }
            return Application.Current.FindResource("TextSecondaryBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBoolToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RiskLevelToTooltipConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is ItemRiskLevel risk)
            {
                return risk switch
                {
                    ItemRiskLevel.Safe => "An toàn - Cache tạm, log, file rác hệ thống",
                    ItemRiskLevel.Low => "Thấp - Cache trình duyệt, dev tools",
                    ItemRiskLevel.Medium => "Trung bình - Dữ liệu game, app user",
                    ItemRiskLevel.High => "Cao - Driver, app quan trọng, user khác",
                    ItemRiskLevel.Critical => "Nguy hiểm - Hệ thống, compact OS, hibernation",
                    _ => ""
                };
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CleanStatusToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is CleanStatus status)
            {
                return status == CleanStatus.Cleaning ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LogLevelToBrushConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is LogLevel level)
            {
                return level switch
                {
                    LogLevel.Debug => Application.Current.FindResource("TextDisabledBrush"),
                    LogLevel.Info => Application.Current.FindResource("TextPrimaryBrush"),
                    LogLevel.Warning => Application.Current.FindResource("WarningBrush"),
                    LogLevel.Error => Application.Current.FindResource("ErrorBrush"),
                    LogLevel.Success => Application.Current.FindResource("SuccessBrush"),
                    _ => Application.Current.FindResource("TextPrimaryBrush")
                };
            }
            return Application.Current.FindResource("TextPrimaryBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class LogLevelToBackgroundConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is LogLevel level)
            {
                return level switch
                {
                    LogLevel.Debug => Application.Current.FindResource("BackgroundBrush"),
                    LogLevel.Info => Application.Current.FindResource("SurfaceBrush"),
                    LogLevel.Warning => new SolidColorBrush(Color.FromArgb(30, 255, 140, 0)),
                    LogLevel.Error => new SolidColorBrush(Color.FromArgb(30, 209, 52, 56)),
                    LogLevel.Success => new SolidColorBrush(Color.FromArgb(30, 16, 124, 16)),
                    _ => Application.Current.FindResource("SurfaceBrush")
                };
            }
            return Application.Current.FindResource("SurfaceBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToAdminTextConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isAdmin)
                return isAdmin ? "Administrator" : "Standard User";
            return "Unknown";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class CleanProfileToStringConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is CleanProfile profile)
            {
                return profile switch
                {
                    CleanProfile.Safe => "An toàn (Khuyến nghị)",
                    CleanProfile.Deep => "Sâu (Nâng cao)",
                    CleanProfile.Custom => "Tùy chỉnh",
                    CleanProfile.Nuclear => "Cực đại (Chuyên gia)",
                    _ => profile.ToString()
                };
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EnumToCollectionConverter : System.Windows.Data.IValueConverter
    {
        public static readonly EnumToCollectionConverter Instance = new();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is Enum enumValue)
            {
                return Enum.GetValues(enumValue.GetType());
            }
            if (value is Type type && type.IsEnum)
            {
                return Enum.GetValues(type);
            }
            return Array.Empty<object>();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DriveUsageToBrushConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double percent)
            {
                if (percent >= 90) return Application.Current.FindResource("ErrorBrush");
                if (percent >= 75) return Application.Current.FindResource("WarningBrush");
                return Application.Current.FindResource("SuccessBrush");
            }
            return Application.Current.FindResource("PrimaryBrush");
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class EventTypeToIconConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is TimelineEventType type)
            {
                return type switch
                {
                    TimelineEventType.Success => "M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41L9 16.17z",
                    TimelineEventType.Error => "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z",
                    TimelineEventType.Warning => "M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z",
                    TimelineEventType.Info => "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 17h-2v-2h2v2zm0-4h-2V7h2v6z",
                    TimelineEventType.Cancel => "M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z",
                    TimelineEventType.Debug => "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 17h-2v-2h2v2zm0-4h-2V7h2v6z",
                    _ => "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 17h-2v-2h2v2zm0-4h-2V7h2v6z"
                };
            }
            return "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 17h-2v-2h2v2zm0-4h-2V7h2v6z";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}