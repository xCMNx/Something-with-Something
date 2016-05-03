using System;
using System.Windows.Input;

namespace Ui
{
	public class Command : ICommand
	{
		private Action<object> _Execute;

		private Predicate<object> _CanExecute;

		private event EventHandler CanExecuteChangedInternal;

		public Command(Action<object> execute) : this(execute, DefaultCanExecute) { }

		public Command(Action<object> execute, Predicate<object> canExecute)
		{
			if (execute == null)
				throw new ArgumentNullException("execute");

			if (canExecute == null)
				throw new ArgumentNullException("canExecute");

			_Execute = execute;
			_CanExecute = canExecute;
		}

		public event EventHandler CanExecuteChanged
		{
			add
			{
				CommandManager.RequerySuggested += value;
				CanExecuteChangedInternal += value;
			}

			remove
			{
				CommandManager.RequerySuggested -= value;
				CanExecuteChangedInternal -= value;
			}
		}

		public bool CanExecute(object parameter) => _CanExecute != null && _CanExecute(parameter);

		public void Execute(object parameter) => _Execute(parameter);

		private static bool DefaultCanExecute(object parameter) => true;

		public void OnCanExecuteChanged() => CanExecuteChangedInternal?.Invoke(this, EventArgs.Empty);
	}
}
