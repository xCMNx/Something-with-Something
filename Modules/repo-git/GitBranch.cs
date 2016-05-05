using core.BaseClasses.Repo;

namespace repo_git
{
	class GitBranch : BranchBase
	{
		public string FullName { get; set; }
		public override string ToString()
		{
			return FullName;
		}
	}
}
