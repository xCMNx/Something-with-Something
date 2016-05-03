using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.Interfaces;
using core.Interfaces.Repo;
using LibGit2Sharp;
using Palmmedia.GitHistory.Core;
using Palmmedia.GitHistory.Core.Model;

namespace repo_git
{
	public class Git : BindableBase, IRepo, IModuleConfigurable
	{
		protected const string PREFIX = "GIT_";
		protected const string USER = PREFIX + "USER";
		protected const string PASS = PREFIX + "PASS";
		protected const string URL = PREFIX + "URL";
		protected const string MASTER = PREFIX + "MASTER_BRUNCH";

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

		public string MasterIdentifier
		{
			get { return Helpers.ConfigRead(MASTER, string.Empty, true); }
			set { Helpers.ConfigWrite(MASTER, value); }
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

		protected async Task<bool> AskAuthInfo(ParametersRequest onParametersRequest, string message = null)
		{
			var dict = new ParametersRequestItem[] {
				new ParametersRequestItem(){ Title = "Path", Value = new PathValueItem(Path) }
				,new ParametersRequestItem(){ Title = "User", Value = new StringValueItem(User) }
				,new ParametersRequestItem(){ Title = "Password", Value = new PasswordValueItem(Password) }
			};

			if (await onParametersRequest(dict, "Git: Выбор репозитория", message))
			{
				Path = (dict[0].Value as StringValueItem).String;
				User = (dict[1].Value as StringValueItem).String;
				Password = (dict[2].Value as StringValueItem).String;
				return true;
			}
			return false;
		}

		public async Task<bool> UpdateAsync(ParametersRequest onParametersRequest, ShowTextRequest showTextRequest)
		{
			try
			{
				string message = null;
				while (await AskAuthInfo(onParametersRequest, message))
				{
					try
					{
						using (var repo = new Repository(Path))
						{
							Branches = repo.Branches.Select(b => new GitBranch()
							{
								Title = System.IO.Path.GetFileName(b.FriendlyName),
								Identifier = b.CanonicalName
							}).ToArray();
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
				await showTextRequest(e.Message);
				Helpers.ConsoleWrite(e.Message, ConsoleColor.Yellow);
			}
			return false;
		}

		IList<ICommit> SelectCommits(IEnumerable<Commit> commits)
		{
			return commits.Select(c => new GitCommit(c.Sha, c.MessageShort, c.Message, string.Format("{0} <{1}>", c.Author.Name, c.Author.Email), c.Author.When.DateTime)).ToArray();
		}

		public IList<ICommit> GetCommits(IBranch branch, ShowTextRequest showTextRequest)
		{
			if (branch != null)
			{
				try
				{
					if (Master == null)
						showTextRequest("Не выбрана основная ветка.");
					else
					{
						using (var repo = new Repository(Path))
						{
							var mbr = repo.Branches.FirstOrDefault(b => Equals(b.CanonicalName, Master.Identifier));
							if (mbr == null)
								showTextRequest(string.Format("Ветка {0} не найдена, обновите список веток.", Master));
							else
							{
								var br = repo.Branches.FirstOrDefault(b => Equals(b.CanonicalName, branch.Identifier));
								if (br == null)
									showTextRequest(string.Format("Ветка {0} не найдена, обновите список веток.", branch));
								else
								{
									return SelectCommits(br.Commits.Except(mbr.Commits));
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
					showTextRequest(e.Message);
					Helpers.ConsoleWrite(e.Message, ConsoleColor.Yellow);
				}
			}
			return new ICommit[0];
		}

		public Task<IList<ICommit>> GetCommitsAsync(IBranch branch, ShowTextRequest showTextRequest)
		{
			return Task<IList<ICommit>>.Factory.StartNew(() => GetCommits(branch, showTextRequest));
		}

		public async Task<bool> ConfigurateAsync(ParametersRequest onParametersRequest, ShowTextRequest showTextRequest)
		{
			var dict = new ParametersRequestItem[] {
				new ParametersRequestItem(){
					Title = "Основная ветка",
					Value = new ComboListValueItem(Branches.Select(b => (GitBranch)b).ToArray(), Master)
				}
			};

			if (await onParametersRequest(dict, "Git: Настройки"))
			{
				Master = (GitBranch)(dict[0].Value as ComboListValueItem).Value;
				return true;
			}
			return false;
		}
	}
}
