using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ui
{
	public class ObjectToVisibility : IValueConverter
	{
		public Visibility Default { get; set; } = Visibility.Hidden;
		public bool Inverted { get; set; } = false;

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool exist = value == null;
			if (Inverted) exist = !exist;
			return exist ? Default : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
