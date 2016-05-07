using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Ui.Controls
{
	public class Question: Control
	{
		static Question()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(Question), new FrameworkPropertyMetadata(typeof(Question)));
		}

		public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
			nameof(Title),
			typeof(string),
			typeof(Question)
		);

		public string Title
		{
			get { return (string)GetValue(TitleProperty); }
			set { SetValue(TitleProperty, value); }
		}

		public static readonly DependencyProperty MessagesProperty = DependencyProperty.Register(
			nameof(Messages),
			typeof(IList<string>),
			typeof(Question)
		);

		public IList<string> Messages
		{
			get { return (IList<string>)GetValue(MessagesProperty); }
			set { SetValue(MessagesProperty, value); }
		}

		public static readonly DependencyProperty ParamsProperty = DependencyProperty.Register(
			nameof(Params),
			typeof(IList<object>),
			typeof(Question)
		);

		public IList<object> Params
		{
			get { return (IList<object>)GetValue(ParamsProperty); }
			set { SetValue(ParamsProperty, value); }
		}

		public static readonly DependencyProperty OkCommandProperty = DependencyProperty.Register(
			nameof(OkCommand),
			typeof(ICommand),
			typeof(Question)
		);

		public ICommand OkCommand
		{
			get { return (ICommand)GetValue(OkCommandProperty); }
			set { SetValue(OkCommandProperty, value); }
		}

		public static readonly DependencyProperty CancelCommandProperty = DependencyProperty.Register(
			nameof(CancelCommand),
			typeof(ICommand),
			typeof(Question)
		);

		public ICommand CancelCommand
		{
			get { return (ICommand)GetValue(CancelCommandProperty); }
			set { SetValue(CancelCommandProperty, value); }
		}
	}
}
