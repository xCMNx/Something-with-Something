using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using core;
using core.BaseClasses;
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

		public string Title => string.Format("{0} with {1}", Tracker?.Name, Repo?.Name);

		QuestionBlock _Question = new QuestionBlock();
		public QuestionBlock Question => _Question;

		ToastBlock _Toast = new ToastBlock();
		public ToastBlock Toast => _Toast;

		string ConfigName
		{
			get { return Helpers.ConfigRead("LastConfig", string.Empty); }
			set { Helpers.ConfigWrite("LastConfig", value); }
		}

		public ObservableCollectionEx<Bookmark> Bookmarks { get; } = new ObservableCollectionEx<Bookmark>() { new Bookmark("+") };

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
		public static readonly RoutedCommand ChangePassCommand = new RoutedCommand();
		public static readonly RoutedCommand BookmarkCommand = new RoutedCommand();

		public MainVM()
		{
			Helpers.mainCTX = System.Threading.SynchronizationContext.Current;
			RegisterCommand(
				SelectPathCommand,
				param => param is TextBox,
				SelectPathCommandExecute
			);
			RegisterCommand(
				ChangePassCommand,
				param => true,
				param => ChangePass()
			);
			RegisterCommand(
				BookmarkCommand,
				param => true,
				SetBookmark
			);
			InitTrackerCommands();
			InitRepoCommands();
			init();
		}

		async Task<bool> CheckSettingsPass()
		{
			var dict = new IParametersRequestItem[] {
				new ParametersRequestItem(){ Title = "Password", Value = new PasswordValueItem(string.Empty) }
			};

			while (await Question.ShowAsync(dict))
				if (Helpers.SetEncryptionKey((dict[0].Value as StringValueItem).String))
					return true;
			return false;
		}

		void init()
		{
			Task.Factory.StartNew(async () =>
			{
				if (!await CheckSettingsPass())
					Helpers.Post(Application.Current.Shutdown);
				else
				{
					LoadBookmarks();
					if (!string.IsNullOrWhiteSpace(ConfigName))
						SetConfig(Bookmarks.FirstOrDefault(b => b.Name == ConfigName));
				}
			});
		}

		void LoadBookmarks()
		{
			var bms = Helpers.ConfigRead("Bookmarks", string.Empty).Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
			if(bms.Length > 0)
				Helpers.Post(() => Bookmarks.Reset(bms.Select(n => new Bookmark(n))));
		}

		void SaveBookmarks()
		{
			Helpers.ConfigWrite("Bookmarks", string.Join("\t", Bookmarks.Select(b => b.Name).ToArray()));
		}

		void SetBookmark(object param)
		{
			var bm = param as Bookmark;
			if (bm.Name == "+")
				NewBookmark();
			else
				SetConfig(bm);
		}

		async Task NewBookmark()
		{
			var dict = new IParametersRequestItem[] {
					new ParametersRequestItem(){ Title = "Название", Value = new StringValueItem(string.Empty) }
				};

			var msg = string.Empty;
			while (await Question.ShowAsync(dict, "Новая закладка", msg))
			{
				var name = (dict[0].Value as StringValueItem).String.Replace('\t', ' ').Trim();
				if (string.IsNullOrWhiteSpace(name))
					msg = "Название не может быть пустым";
				else if (Bookmarks.FirstOrDefault(b => b.Name.Equals(name)) != null)
					msg = "Закладка с таким названием уже существует";
				else
				{
					var bm = new Bookmark(name);
					Bookmarks.Insert(Bookmarks.Count - 1, bm);
					SetConfig(bm);
					SaveBookmarks();
					return;
				}
			}
		}

		void SetConfig(Bookmark bookmark)
		{
			if (bookmark != null)
			{
				ConfigName = bookmark.Name;
				initTracker();
				initRepo();
			}
		}

		public void ChangePass()
		{
			Task.Factory.StartNew(async () =>
			{
				if (await CheckSettingsPass())
				{
					var dict = new IParametersRequestItem[] {
						new ParametersRequestItem(){ Title = "New password", Value = new PasswordValueItem(string.Empty) }
					};

					if (await Question.ShowAsync(dict))
						Helpers.ChangeKey((dict[0].Value as StringValueItem).String);
				}
			});
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

		IRepo Repo = new DummyRepo();

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
		public static readonly RoutedCommand RepoSearchCommit = new RoutedCommand();
		public static readonly RoutedCommand RepoCreateBranch = new RoutedCommand();
		public static readonly RoutedCommand RepoSelectBranch = new RoutedCommand();

		protected void InitRepoCommands()
		{
			CommitsSortCommand = new Command(CommitsSortCommandExecute);
			ChangeRepo = new Command(a => UpdateSettingsRepo());
			RepoAdvansed = new Command(a => ConfigurateRepo(), o => Repo is IModuleConfigurable);
			RegisterCommand(
				RepoSearchCommit,
				param => true,
				SearchCommit
			);
			RegisterCommand(
				RepoCreateBranch,
				param => true,
				CreateBranch
			);
			RegisterCommand(
				RepoSelectBranch,
				param => true,
				SelectBranch
			);
		}

		void initRepo()
		{
			Repo = new Git();
			Repo.ConfigName = ConfigName;
			UpdateRepo();
			Helpers.Post(() => NotifyPropertyChanged(nameof(Title)));
		}

		public async void ConfigurateRepo()
		{
			if (await (Repo as IModuleConfigurable).ConfigurateAsync(Question.ShowAsync, Toast.ShowAsync))
				UpdateCommits();
		}

		public async void SearchCommit(object prop)
		{
			RepoWait = true;
			try
			{
				_commits = await Repo.GetCommitsAsync(prop as IIssue, Toast.ShowAsync);
				UpdateCommitsList();
			}
			finally
			{
				RepoWait = false;
			}
		}

		public async void CreateBranch(object prop)
		{
			if (await Repo.CreateBranch(prop as IIssue, Question.ShowAsync, Toast.ShowAsync))
				NotifyPropertyChanged(nameof(Branches));
		}

		public async void SelectBranch(object prop)
		{
			Branch = await Repo.GetBranch(prop as IIssue, Question.ShowAsync, Toast.ShowAsync);
			NotifyPropertyChanged(nameof(Branches));
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

		public async void UpdateSettingsRepo()
		{
			if (await Repo.UpdateSettingsAsync(Question.ShowAsync, Toast.ShowAsync))
				UpdateRepo();
		}

		#endregion

		#region Tracker

		ITracker Tracker = new DummyTracker();
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
				Helpers.ConfigWrite(string.Format("LastProjectId[{0}]", ConfigName), value?.Identifier);
				UpdateIssues();
			}
		}

		void initTracker()
		{
			Tracker = new Redmine();
			Tracker.ConfigName = ConfigName;
			UpdateTracker();
			var lpiId = Helpers.ReadFromConfig(string.Format("LastProjectId[{0}]", ConfigName), null);
			Helpers.Post(()=>
			{
				NotifyPropertyChanged(nameof(Title));
				Project = Projects.FirstOrDefault(p => Equals(p.Identifier.ToString(), lpiId));
			});
		}

		public async void UpdateTracker()
		{
			BeginWait();
			try
			{
				await Tracker.UpdateAsync(Question.ShowAsync, Toast.ShowAsync);
				NotifyPropertyChanged(nameof(Projects));
			}
			finally
			{
				EndWait();
			}
		}

		public async void UpdateSettingsTracker()
		{
			if(await Tracker.UpdateSettingsAsync(Question.ShowAsync, Toast.ShowAsync))
				UpdateTracker();
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
			ChangeTracker = new Command(a => UpdateSettingsTracker());
			TrackerAdvansed = new Command(a => ConfigurateTracker(), o => Tracker is IModuleConfigurable);
		}

		#endregion
	}
}