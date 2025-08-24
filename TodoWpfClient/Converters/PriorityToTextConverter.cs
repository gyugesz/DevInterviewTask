using System;
using System.Globalization;
using System.Windows.Data;

namespace TodoWpfClient.Converters
{
    public class PriorityToTextConverter : IValueConverter
    {
        public string LowText { get; set; } = "Low";
        public string MediumText { get; set; } = "Medium";
        public string HighText { get; set; } = "High";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int p)
            {
                return p switch
                {
                    <= 1 => LowText,
                    2 => MediumText,
                    >= 3 => HighText
                };
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // opcionális visszaalakítás
            return value?.ToString()?.ToLowerInvariant() switch
            {
                "low" => 1,
                "medium" => 2,
                "high" => 3,
                _ => 1
            };
        }
    }
}
