using core.BaseClasses.Repo;

namespace repo_git
{
	class GitBranch : BranchBase
	{
		public override string ToString()
		{
			return Title;
		}
	}
}
