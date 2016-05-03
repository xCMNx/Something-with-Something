using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using core;
using core.Interfaces;
using core.Interfaces.Repo;
using core.Interfaces.Tracker;
using repo_git;
using tracker_redmine;
using Ui;

namespace SwS
{
	public class MainVM : CommandSink, INotifyPropertyChanged
	{
		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;

		public void NotifyPropertyChanged(string propertyName = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		public void Route(object sender, PropertyChangedEventArgs e) => PropertyChanged?.Invoke(sender, e);

		public void NotifyPropertiesChanged(params string[] propertyNames)
		{
			if (PropertyChanged != null)
			{
				foreach (var prop in propertyNames)
					PropertyChanged(this, new PropertyChangedEventArgs(prop));
			}
		}
		#endregion

		static MainVM _instance;
		public static MainVM Instance => _instance ?? (_instance = new MainVM());

		QuestionBlock _Question = new QuestionBlock();
		public QuestionBlock Question => _Question;

		ToastBlock _Toast = new ToastBlock();
		public ToastBlock Toast => _Toast;

		#region Wait state properties
		int _MainWaitCnt = 0;
		object _MainWaitLock = new object();
		public bool MainWait { get; private set; } = false;

		void BeginWait()
		{
			lock (_MainWaitLock)
			{
				if (++_MainWaitCnt == 1)
				{
					MainWait = true;
					Helpers.Post(() => NotifyPropertyChanged(nameof(MainWait)));
				}
			}
		}

		void EndWait()
		{
			lock (_MainWaitLock)
			{
				if (_MainWaitCnt > 0 && --_MainWaitCnt == 0)
				{
					MainWait = false;
					Helpers.Post(() => NotifyPropertyChanged(nameof(MainWait)));
				}
			}
		}

		bool _TrackerWait = false;
		public bool TrackerWait
		{
			get { return _TrackerWait; }
			private set
			{
				_TrackerWait = value;
				NotifyPropertyChanged(nameof(TrackerWait));
			}
		}

		bool _RepoWait = false;
		public bool RepoWait
		{
			get { return _RepoWait; }
			private set
			{
				_RepoWait = value;
				NotifyPropertyChanged(nameof(RepoWait));
			}
		}

		#endregion

		public static readonly RoutedCommand SelectPathCommand = new RoutedCommand();

		public MainVM()
		{
			Helpers.mainCTX = System.Threading.SynchronizationContext.Current;
			RegisterCommand(
				SelectPathCommand,
				param => param is TextBox,
				SelectPathCommandExecute
			);
			InitTrackerCommands();
			InitRepoCommands();
			UpdateTracker();
			UpdateRepo();
		}

		void SelectPathCommandExecute(object parameter)
		{
			var textbox = parameter as TextBox;
			using (var od = new System.Windows.Forms.FolderBrowserDialog())
			{
				od.SelectedPath = textbox.Text;
				if (od.ShowDialog() == System.Windows.Forms.DialogResult.OK)
					textbox.SetCurrentValue(TextBox.TextProperty, od.SelectedPath);
			}
		}

		#region Repo

		IRepo Repo = new Git();

		public IList<ICommit> Commits { get; private set; } = new ICommit[0];
		public IList<ICommit> _commits = new ICommit[0];

		public IList<IBranch> Branches => Repo.Branches;

		IBranch _Branch = null;
		public IBranch Branch
		{
			get { return _Branch; }
			set
			{
				_Branch = value;
				NotifyPropertyChanged(nameof(Branch));
				UpdateCommits();
			}
		}

		public ICommand CommitsSortCommand { get; private set; }
		public Command ChangeRepo { get; private set; }
		public Command RepoAdvansed { get; private set; }

		protected void InitRepoCommands()
		{
			CommitsSortCommand = new Command(CommitsSortCommandExecute);
			ChangeRepo = new Command(a => UpdateRepo());
			RepoAdvansed = new Command(a => ConfigurateRepo(), o => Repo is IModuleConfigurable);
		}

		public async void ConfigurateRepo()
		{
			if (await (Repo as IModuleConfigurable).ConfigurateAsync(Question.ShowAsync, Toast.ShowAsync))
				UpdateCommits();
		}

		/// <summary>
		/// Для связи столбца и поля по которому сортируем
		/// </summary>
		Dictionary<object, Func<ICommit, object>> selectors = new Dictionary<object, Func<ICommit, object>>()
		{
			{ "Date", c => c.Date }
			,{ "Autor", c => c.Autor }
			,{ "Title", c => c.Title }
		};

		IList<TSource> SortBy<TSource, TKey>(IEnumerable<TSource> src, Func<TSource, TKey> selector, bool reverse) => (reverse ? src.OrderByDescending(selector) : src.OrderBy(selector)).ToArray();

		object lastSortedColumn = "Date";
		bool reverse = true;
		void UpdateCommitsList()
		{
				Commits = SortBy(_commits, selectors[lastSortedColumn], reverse);
				NotifyPropertyChanged(nameof(Commits));
		}

		void CommitsSortCommandExecute(object parameter)
		{
			reverse = Equals(lastSortedColumn, parameter) ? !reverse : false;
			lastSortedColumn = parameter;
			UpdateCommitsList();
		}

		async void UpdateCommits()
		{
			RepoWait = true;
			try
			{
				_commits = await Repo.GetCommitsAsync(_Branch, Toast.ShowAsync);
				UpdateCommitsList();
			}
			finally
			{
				RepoWait = false;
			}
		}

		public async void UpdateRepo()
		{
			BeginWait();
			try
			{
				if (await Repo.UpdateAsync(Question.ShowAsync, Toast.ShowAsync))
				{
					NotifyPropertyChanged(nameof(Branches));
					UpdateCommits();
				}
			}
			finally
			{
				EndWait();
			}
		}

		#endregion

		#region Tracker

		ITracker Tracker = new Redmine();
		public IList<IProject> Projects => Tracker.Projects;
		public IList<IIssue> Issues { get; private set; }

		IProject _Project;
		public IProject Project
		{
			get { return _Project; }
			set
			{
				_Project = value;
				NotifyPropertyChanged(nameof(Project));
				UpdateIssues();
			}
		}

		public void UpdateTracker()
		{
			BeginWait();
			try
			{
				Tracker.UpdateAsync(Question.ShowAsync, Toast.ShowAsync);
			}
			finally
			{
				EndWait();
			}
		}

		public async void UpdateIssues()
		{
			TrackerWait = true;
			try
			{
				Issues = await Tracker.GetIssuesAsync(_Project, Toast.ShowAsync);
				NotifyPropertiesChanged(nameof(Issues));
			}
			finally
			{
				TrackerWait = false;
			}
		}

		public async void ConfigurateTracker()
		{
			if (await (Tracker as IModuleConfigurable).ConfigurateAsync(Question.ShowAsync, Toast.ShowAsync))
				UpdateIssues();
		}

		public Command ChangeTracker { get; private set; }
		public Command TrackerAdvansed { get; private set; }

		void InitTrackerCommands()
		{
			ChangeTracker = new Command(a => UpdateTracker());
			TrackerAdvansed = new Command(a => ConfigurateTracker(), o => Tracker is IModuleConfigurable);
		}

		#endregion
	}
}