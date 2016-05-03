using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Ui
{
	public class Toast: UserControl
	{
		static Toast()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(Toast), new FrameworkPropertyMetadata(typeof(Toast)));
			VisibilityProperty.OverrideMetadata(
				typeof(Toast),
				new FrameworkPropertyMetadata(OnVisibilityPropertyChanged)
			);
		}

		private static void OnVisibilityPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if(((Visibility)e.NewValue) == Visibility.Visible)
				(d as Toast).StartAnimation();
		}

		void StartAnimation()
		{
			DoubleAnimation animation = new DoubleAnimation() {
				BeginTime = TimeSpan.FromMilliseconds(Delay),
				Duration = TimeSpan.FromMilliseconds(Duration),
				From = 1d,
				To = 0,
			};
			Storyboard sb = new Storyboard();
			sb.Children.Add(animation);
			Storyboard.SetTarget(animation, this);
			Storyboard.SetTargetProperty(animation, new PropertyPath(OpacityProperty.Name));
			animation.Completed += Animation_Completed;
			sb.Begin();
		}

		private void Animation_Completed(object sender, EventArgs e)
		{
			SetCurrentValue(VisibilityProperty, Visibility.Hidden);
			SetCurrentValue(OpacityProperty, 1d);
		}

		public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
			nameof(Text),
			typeof(string),
			typeof(Toast)
		);

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
			set { SetValue(TextProperty, value); }
		}

		public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
			nameof(Duration),
			typeof(int),
			typeof(Toast),
			new PropertyMetadata(1000)
		);

		public int Duration
		{
			get { return (int)GetValue(DurationProperty); }
			set { SetValue(DurationProperty, value); }
		}

		public static readonly DependencyProperty DelayProperty = DependencyProperty.Register(
			nameof(Delay),
			typeof(int),
			typeof(Toast),
			new PropertyMetadata(2000)
		);

		public int Delay
		{
			get { return (int)GetValue(DelayProperty); }
			set { SetValue(DelayProperty, value); }
		}
	}
}
