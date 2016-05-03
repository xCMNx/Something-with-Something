using System.Windows;
using System.Windows.Controls;

namespace SwS
{
	public static class PasswordHelper
	{
		public static readonly DependencyProperty PasswordProperty = DependencyProperty.RegisterAttached(
			"Password",
			typeof(string), 
			typeof(PasswordHelper),
			new FrameworkPropertyMetadata(string.Empty, OnPasswordPropertyChanged)
		);

		public static readonly DependencyProperty AttachProperty = DependencyProperty.RegisterAttached(
			"Attach",
			typeof(bool), 
			typeof(PasswordHelper), 
			new PropertyMetadata(false, Attach)
		);

		private static readonly DependencyProperty IsUpdatingProperty = DependencyProperty.RegisterAttached(
			"IsUpdating", 
			typeof(bool),
			typeof(PasswordHelper)
		);

		public static bool GetAttach(DependencyObject dp) => (bool)dp.GetValue(AttachProperty);
		public static void SetAttach(DependencyObject dp, bool value) => dp.SetCurrentValue(AttachProperty, value);

		public static string GetPassword(DependencyObject dp) => (string)dp.GetValue(PasswordProperty);
		public static void SetPassword(DependencyObject dp, string value) => dp.SetCurrentValue(PasswordProperty, value);

		private static bool GetIsUpdating(DependencyObject dp) => (bool)dp.GetValue(IsUpdatingProperty);
		private static void SetIsUpdating(DependencyObject dp, bool value) => dp.SetCurrentValue(IsUpdatingProperty, value);

		private static void OnPasswordPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			PasswordBox passwordBox = sender as PasswordBox;
			passwordBox.PasswordChanged -= PasswordChanged;

			if (!(bool)GetIsUpdating(passwordBox))
			{
				passwordBox.Password = (string)e.NewValue;
			}
			passwordBox.PasswordChanged += PasswordChanged;
		}

		private static void Attach(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			PasswordBox passwordBox = sender as PasswordBox;

			if (passwordBox == null)
				return;

			if ((bool)e.OldValue)
			{
				passwordBox.PasswordChanged -= PasswordChanged;
			}

			if ((bool)e.NewValue)
			{
				passwordBox.PasswordChanged += PasswordChanged;
			}
		}

		private static void PasswordChanged(object sender, RoutedEventArgs e)
		{
			PasswordBox passwordBox = sender as PasswordBox;
			SetIsUpdating(passwordBox, true);
			SetPassword(passwordBox, passwordBox.Password);
			SetIsUpdating(passwordBox, false);
		}
	}
}
