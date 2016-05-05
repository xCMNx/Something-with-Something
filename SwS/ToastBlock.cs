using System.Threading.Tasks;
using System.Windows;
using core;

namespace SwS
{
	public class ToastBlock : BindableBase
	{
		public Visibility Visibility { get; private set; } = Visibility.Hidden;
		public string Text { get; private set; }
		const int DEFAULT_DELAY = 3000;
		public int Delay { get; private set; } = DEFAULT_DELAY;

		public void Show(string text, int delay = -1)
		{
			Text = text;
			Visibility = Visibility.Visible;
			delay = delay > 0 ? delay : DEFAULT_DELAY;
			if (delay != Delay)
			{
				Delay = delay;
				NotifyPropertyChanged(nameof(Delay));
			}
			NotifyPropertiesChanged(nameof(Text), nameof(Visibility));
		}

		public Task ShowAsync(string text, int delay = -1)
		{
			Helpers.Post(() => Show(text, delay));
			return Task.Factory.StartNew(() => { });
		}
	}
}