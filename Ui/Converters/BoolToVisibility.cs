using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ui.Converters
{
	public class BoolToVisibility : IValueConverter
	{
		public bool Inverted { get; set; } = false;
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (Inverted ? !(bool)value : (bool)value) ? Visibility.Visible : ((string)parameter == "C" ? Visibility.Collapsed : Visibility.Hidden);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
