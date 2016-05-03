using System.ComponentModel;

namespace Ui
{
	public class BindableBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public void NotifyPropertyChanged(string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

		public void NotifyPropertiesChanged(params string[] propertyNames)
		{
			if (PropertyChanged != null)
			{
				foreach (var prop in propertyNames)
					PropertyChanged(this, new PropertyChangedEventArgs(prop));
			}
		}

		public void Route(object sender, PropertyChangedEventArgs e) => PropertyChanged?.Invoke(sender, e);
	}
}
