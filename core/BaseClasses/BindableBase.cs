using System;
using System.ComponentModel;

namespace core.BaseClasses
{
	public class BindableBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public void NotifyPropertyChanged(String propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public void NotifyPropertiesChanged(params String[] propertyNames)
		{
			if (PropertyChanged != null)
			{
				foreach (var prop in propertyNames)
					PropertyChanged(this, new PropertyChangedEventArgs(prop));
			}
		}

		public void Route(object sender, PropertyChangedEventArgs e)
		{
			if (PropertyChanged != null)
				PropertyChanged(sender, e);
		}
	}
}
