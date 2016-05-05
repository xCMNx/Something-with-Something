using System.Collections.Generic;
using System.Threading.Tasks;
using core.Interfaces.Tracker;

namespace core.Interfaces.Repo
{
	public interface IRepo : IModule
	{
		IList<IBranch> Branches { get; }
		IBranch Master { get; }
		Task<IList<ICommit>> GetCommitsAsync(IBranch branch, ShowText showText);
		Task<IList<ICommit>> GetCommitsAsync(IIssue issue, ShowText showText);
		Task<bool> CreateBranch(IIssue issue, ParametersRequest parametersRequest, ShowText showText);
	}
}
