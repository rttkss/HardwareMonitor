using System;
using System.Globalization;
using System.Windows.Data;

namespace HardwareMonitor
{
    public class BytesToMBConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                return (bytes / 1024.0 / 1024.0).ToString("N0") + " МБ";
            }
            return "0 МБ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BytesToGBConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                return (bytes / 1024.0 / 1024.0 / 1024.0).ToString("N2") + " ГБ";
            }
            return "0 ГБ";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PercentColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double pct)
            {
                if (pct > 80) return "#FF5555";
                if (pct > 50) return "#FFB347";
                return "#55FF55";
            }
            return "#55FF55";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}