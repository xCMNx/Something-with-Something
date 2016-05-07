using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ui.Converters
{
	public class VisibilityToBool : IValueConverter
	{
		public bool Inverted { get; set; } = false;
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool val = (Visibility)value == Visibility.Visible;
			return Inverted ? !val : val;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
