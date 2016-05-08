using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using core;
using core.BaseClasses;
using core.Interfaces;
using core.Interfaces.Repo;
using core.Interfaces.Tracker;
using LibGit2Sharp;
using Palmmedia.GitHistory.Core.Model;

namespace repo_git
{
	public class Git : BindableBase, IRepo, IModuleConfigurable
	{
		protected const string PREFIX = "GIT_";
		protected const string USER = PREFIX + "USER";
		protected const string RUSER = USER + "_[{0}]";
		protected const string PASS = PREFIX + "PASS";
		protected const string RPASS = PASS + "_[{0}]";
		protected const string URL = PREFIX + "URL";
		protected const string MASTER = PREFIX + "MASTER_BRUNCH";
		protected const string COMMIT_MASK = PREFIX + "COMMIT_MASK";
		protected const string COMMIT_SEARCH_MASK = PREFIX + "COMMIT_SEARCH_MASK";
		protected const string COMMIT_START = PREFIX + "COMMIT_START_FLAG";
		protected const string BRANCH_TEMPLATE = PREFIX + "BRANCH_TEMPLATE";

		public string Path
		{
			get { return Helpers.ConfigRead(URL, string.Empty, true); }
			set { Helpers.ConfigWrite(URL, value); }
		}

		public string User
		{
			get { return Helpers.ConfigRead(USER, string.Empty, true); }
			set { Helpers.ConfigWrite(USER, value); }
		}

		public string Password
		{
			get { return Helpers.ConfigRead(PASS, string.Empty, true); }
			set { Helpers.ConfigWrite(PASS, value); }
		}

		public KeyValuePair<string, string> this[string repoUrl]
		{
			get { return new KeyValuePair<string, string>(GetUser(repoUrl), GetPass(repoUrl)); }
			set { SetUserPass(repoUrl, value.Key, value.Value); }
		}

		public string MasterIdentifier
		{
			get { return Helpers.ConfigRead(MASTER, string.Empty, true); }
			set { Helpers.ConfigWrite(MASTER, value); }
		}

		public string CommitMask
		{
			get { return Helpers.ConfigRead(COMMIT_MASK, "refs #%Identifier", true); }
			set { Helpers.ConfigWrite(COMMIT_MASK, value); }
		}

		public string CommitSearchMask
		{
			get { return Helpers.ConfigRead(COMMIT_SEARCH_MASK, "(?im)^refs (#|)%Identifier", true); }
			set { Helpers.ConfigWrite(COMMIT_SEARCH_MASK, value); }
		}

		public bool CommitStart
		{
			get { return Helpers.ConfigRead(COMMIT_START, true, true); }
			set { Helpers.ConfigWrite(COMMIT_START, value); }
		}

		public string BranchTemplate
		{
			get { return Helpers.ConfigRead(BRANCH_TEMPLATE, "refs %IDENTIFIER - %TITLE", true); }
			set { Helpers.ConfigWrite(BRANCH_TEMPLATE, value); }
		}

		IBranch _Master = null;
		public IBranch Master
		{
			get { return _Master; }
			private set
			{
				_Master = value;
				MasterIdentifier = (string)_Master?.Identifier;
				Helpers.Post(() => NotifyPropertyChanged(nameof(Master)));
			}
		}

		public IList<IBranch> Branches { get; private set; } = new IBranch[0];

		public string Name => "Git";

		static string UserCFGPath(string repoUrl)
		{
			return string.Format(RUSER, repoUrl);
		}

		static string PassCFGPath(string repoUrl)
		{
			return string.Format(RPASS, repoUrl);
		}

		protected string GetUser(string repoUrl)
		{
			return Helpers.ConfigRead(UserCFGPath(repoUrl), null, false);
		}

		protected string GetPass(string repoUrl)
		{
			return Helpers.ConfigRead(PassCFGPath(repoUrl), null, false);
		}

		protected void SetUserPass(string repoUrl, string user, string pass)
		{
			Helpers.ConfigWrite(UserCFGPath(repoUrl), user);
			Helpers.ConfigWrite(PassCFGPath(repoUrl), pass);
		}

		protected async Task<bool> AskAuthInfo(ParametersRequest parametersRequest, string message = null)
		{
			var dict = new IParametersRequestItem[] {
				new ParametersRequestItem(){ Title = "Path", Value = new PathValueItem(Path) }
				,new HeaderRequestItem() { Title = "Авторизация" }
				,new ParametersRequestItem(){ Title = "User", Value = new StringValueItem(User) }
				,new ParametersRequestItem(){ Title = "Password", Value = new PasswordValueItem(Password) }
			};

			if (await parametersRequest(dict, "Git: Выбор репозитория", message))
			{
				Path = (dict[0].Value as StringValueItem).String;
				User = (dict[2].Value as StringValueItem).String;
				Password = (dict[3].Value as StringValueItem).String;
				return true;
			}
			return false;
		}

		protected async Task<bool> AskRepoAuthInfo(string repoUrl, ParametersRequest parametersRequest)
		{
			var dict = new IParametersRequestItem[] {
				new ParametersRequestItem(){ Title = "User", Value = new StringValueItem(GetUser(repoUrl) ?? User) }
				,new ParametersRequestItem(){ Title = "Password", Value = new PasswordValueItem(GetPass(repoUrl) ?? Password) }
			};

			if (await parametersRequest(dict, repoUrl))
			{
				SetUserPass(repoUrl, (dict[0].Value as StringValueItem).String, (dict[1].Value as StringValueItem).String);
				return true;
			}
			return false;
		}

		protected void UpdateBranches(Repository repo)
		{
			Branches = repo.Branches.Select(b => new GitBranch()
			{
				FullName = b.FriendlyName,
				Title = System.IO.Path.GetFileName(b.FriendlyName),
				Identifier = b.CanonicalName
			}).ToArray();
		}

		public async Task FetchAll(Repository repo, ParametersRequest parametersRequest, ShowText showText)
		{
			foreach (Remote remote in repo.Network.Remotes)
			{
				FetchOptions options = new FetchOptions();
				UsernamePasswordCredentials creds = null;
				try
				{
					options.CredentialsProvider = (url, usernameFromUrl, types) =>
						{
							if (creds == null)
								creds = new UsernamePasswordCredentials() { Username = GetUser(remote.Url) ?? User, Password = GetPass(remote.Url) ?? Password };
							else
							{
								var r = AskRepoAuthInfo(url, parametersRequest);
								r.Wait();
								if(!r.Result)
									throw new RepoAuthException("Authentication cancelled");
								creds.Username = GetUser(remote.Url);
								creds.Password = GetPass(remote.Url);
							}
							return creds;
						};
					await Task.Factory.StartNew(() => repo.Network.Fetch(remote, options));
				}
				catch (Exception e)
				{
					var msg = string.Format("{0}:\r\n{1}", remote.Url, e.Message);
					await showText(msg);
					Helpers.ConsoleWrite(msg, ConsoleColor.Yellow);
				}
			}
		}

		protected void UpdateBranches()
		{
			using (var repo = new Repository(Path))
				UpdateBranches(repo);
		}

		public async Task<bool> UpdateAsync(ParametersRequest parametersRequest, ShowText showText)
		{
			try
			{
				string message = null;
				while (await AskAuthInfo(parametersRequest, message))
				{
					try
					{
						using (var repo = new Repository(Path))
						{
							await FetchAll(repo, parametersRequest, showText);
							UpdateBranches();
						}
						var mName = MasterIdentifier;
						Master = string.IsNullOrWhiteSpace(mName) ? null : Branches.FirstOrDefault(b => Equals(b.Identifier, mName));
						return true;
					}
					catch (Exception e)
					{
						message = e.Message;
						Helpers.ConsoleWrite(message, ConsoleColor.Red);
					}
				}
			}
			catch (Exception e)
			{
				await showText(e.Message);
				Helpers.ConsoleWrite(e.Message, ConsoleColor.Yellow);
			}
			return false;
		}

		IList<ICommit> ConvertCommits(IEnumerable<Commit> commits)
		{
			return commits.Select(c => new GitCommit(c.Sha, c.MessageShort, c.Message, string.Format("{0} <{1}>", c.Author.Name, c.Author.Email), c.Author.When.DateTime)).ToArray();
		}

		public IList<ICommit> GetCommits(IBranch branch, ShowText showText)
		{
			if (branch != null)
			{
				try
				{
					//if (Master == null)
					//	showText("Не выбрана основная ветка.");
					//else
					{
						using (var repo = new Repository(Path))
						{
							var mbr = Master == null ? repo.Head : repo.Branches.FirstOrDefault(b => Equals(b.CanonicalName, Master.Identifier)) ?? repo.Head;
							//if (mbr == null)
							//	showText(string.Format("Ветка {0} не найдена, обновите список веток.", Master));
							//else
							{
								var br = repo.Branches.FirstOrDefault(b => Equals(b.CanonicalName, branch.Identifier));
								if (br == null)
									showText(string.Format("Ветка {0} не найдена, обновите список веток.", branch));
								else
								{
									return ConvertCommits(br.Commits.Except(mbr.Commits));
									//var history = GitHistory.GetCommits(repo).Reverse();
									//var commits = new List<ICommit>();
									//var commit = history.FirstOrDefault(c => c.Id == br.Tip.Sha);
									//while (commit.Parents.Count() < 2)
									//{
									//	commits.Add(commit);
									//	if (commit.Parents.Count == 0)
									//		break;
									//	commit = commit.Parents.ElementAt(0) as GitCommit;
									//}
									//return commits;
								}
							}
						}
					}
				}
				catch (Exception e)
				{
					showText(e.Message);
					Helpers.ConsoleWrite(e.Message, ConsoleColor.Yellow);
				}
			}
			return new ICommit[0];
		}

		public Task<IList<ICommit>> GetCommitsAsync(IBranch branch, ShowText showText)
		{
			return Task<IList<ICommit>>.Factory.StartNew(() => GetCommits(branch, showText));
		}

		public async Task<bool> ConfigurateAsync(ParametersRequest parametersRequest, ShowText showText)
		{
			var dict = new IParametersRequestItem[] {
				new ParametersRequestItem(){
					Title = "Основная ветка",
					Value = new ComboListValueItem(Branches.OfType<GitBranch>().ToArray(), Master)
				}
				,new ParametersRequestItem(){
					Title = "Шаблон создания веток",
					Value = new StringValueItem(BranchTemplate),
					Hint = "Например \"refs %Identifier - %Title\" где %Identifier это идентификатор задачи"
				}
				,new ParametersRequestItem(){
					Title = "Шаблон поиска коммита",
					Value = new StringValueItem(CommitMask),
					Hint = "Например \"refs #%Identifier:\" где %Identifier это идентификатор задачи"
				}
				,new BoolRequestItem(){
					Title = "Сопоставлять начало сообщения",
					Value = CommitStart,
					Hint = "Игнорируется при указании регулярки"
				}
				,new ParametersRequestItem(){
					Title = "Regex",
					Value = new StringValueItem(CommitSearchMask),
					Hint = "Если указана регулярка, то поиск будет осуществлен с её использованием, параметры такие же, как и у шаблона\r\nНапример: (?im)^refs (#|)%Identifier"
				}
			};

			if (await parametersRequest(dict, "Git: Настройки"))
			{
				Master = (GitBranch)(dict[0].Value as ComboListValueItem).Value;
				BranchTemplate = (dict[1].Value as StringValueItem).String;
				CommitMask = (dict[2].Value as StringValueItem).String;
				CommitStart = (bool)dict[3].Value;
				CommitSearchMask = (dict[4].Value as StringValueItem).String;
				return true;
			}
			return false;
		}

		static Regex filterBuild = new Regex(@"%([\w].*?)([^\w]|$)", RegexOptions.IgnoreCase);

		string GetMaskValue(string mask, object source)
		{
			var props = source.GetType().GetProperties().ToDictionary(p => p.Name);
			return filterBuild.Replace(mask, m =>
			{
				var propName = m.Groups[1].Value;
				PropertyInfo prp = null;
				if (!props.TryGetValue(propName, out prp))
				{
					foreach (var pair in props)
						if (pair.Key.Equals(propName, StringComparison.InvariantCultureIgnoreCase))
						{
							prp = pair.Value;
							break;
						}
				}
				if (prp == null)
					throw new Exception(string.Format("Параметр {0} заданый в шаблоне коммита не найден.", propName));
				return prp.GetValue(source)?.ToString() + m.Groups[2].Value;
			});
		}

		protected IList<Commit> GetAllCommits(Repository repo)
		{
			var lst = new HashSet<Commit>();
			foreach (var brunch in repo.Branches)
				lst.UnionWith(brunch.Commits);
			return lst.ToArray();
		}

		protected IList<Commit> RegexSearch(Regex regex, IList<Commit> commits)
		{
			return commits.Where(x => regex.IsMatch(x.Message)).ToArray();
		}

		protected IList<Commit> RegexSearch(string regex, IList<Commit> commits)
		{
			return RegexSearch(new Regex(regex), commits);
		}

		protected IList<Commit> Search(string pattern, IList<Commit> commits, bool starts, bool caseSensitive = false)
		{
			var cmpr = caseSensitive ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;
			var cmprer = starts ? (Func<Commit, bool>)
				(c => c.Message.StartsWith(pattern, cmpr)) :
				(c => c.Message.IndexOf(pattern, cmpr) >= 0);
			return commits.Where(cmprer).ToList();
		}

		public IList<ICommit> GetCommits(IIssue issue, ShowText showText)
		{
			try
			{
				var regex = !string.IsNullOrWhiteSpace(CommitSearchMask);
				var mask = regex ? CommitSearchMask : CommitMask;
				if (string.IsNullOrWhiteSpace(mask))
					showText("Не задан шаблон поиска коммита.");
				{
					if (issue == null)
						showText("Не выбрана задача.");
					{
						var maskVal = GetMaskValue(mask, issue);
						if (maskVal.Equals(mask, StringComparison.InvariantCultureIgnoreCase))
							showText("Шаблон поиска коммита не содержит параметров.");
						{
							IList<Commit> commits;
							using (var repo = new Repository(Path))
							{
								commits = GetAllCommits(repo);
								commits = regex ? RegexSearch(maskVal, commits) : Search(maskVal, commits, CommitStart);
								return ConvertCommits(commits);
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				showText(e.Message);
				Helpers.ConsoleWrite(e.Message, ConsoleColor.Yellow);
			}
			return new ICommit[0];
		}

		public Task<IList<ICommit>> GetCommitsAsync(IIssue issue, ShowText showText)
		{
			return Task<IList<ICommit>>.Factory.StartNew(() => GetCommits(issue, showText));
		}

		protected string GetNewBranchName(IIssue issue, string mask)
		{
			//todo: autofix
			return GetMaskValue(mask, issue).Replace(' ', '_');
		}

		public async Task<bool> CreateBranch(IIssue issue, ParametersRequest parametersRequest, ShowText showText)
		{
			try
			{
				using (var repo = new Repository(Path))
				{
					var sources = repo.Branches.Select(b => (object)new EntityBase() { Title = b.FriendlyName, Identifier = b.Tip.Sha }).ToList();
					var headBranch = repo.Branches.FirstOrDefault(b => b.CanonicalName == repo.Head.CanonicalName);
					var current = new EntityBase() { Title = repo.Head.Tip.MessageShort, Identifier = repo.Head.Tip.Sha };
					if (headBranch == null)
						sources.Insert(0, current);
					var selected = sources.FirstOrDefault(s => (s as EntityBase).Title == (Master as GitBranch).Title) ?? sources.FirstOrDefault(s => (s as EntityBase).Title == repo.Head.FriendlyName) ?? current;
					var dict = new IParametersRequestItem[] {
					new ParametersRequestItem(){
						Title = "Источник",
						Value = new ComboListValueItem(sources, selected)
					}
					,new ParametersRequestItem(){
						Title = "Название ветки",
						Value = new StringValueItem(GetNewBranchName(issue, BranchTemplate))
					}
					};

					if (await parametersRequest(dict, "Git: Создание ветки"))
					{
						var commit = (EntityBase)(dict[0].Value as ComboListValueItem).Value;
						var branchName = (dict[1].Value as StringValueItem).String;
						repo.CreateBranch(branchName, repo.Lookup<Commit>((string)commit.Identifier));
						UpdateBranches(repo);
						return true;
					}
				}
			}
			catch (Exception e)
			{
				await showText(e.Message, 2000 + e.Message.Length * 25);
				Helpers.ConsoleWrite(e.Message, ConsoleColor.Yellow);
			}
			return false;
		}
	}
}
