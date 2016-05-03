using System.Collections.Generic;
using System.Threading.Tasks;

namespace core.Interfaces.Repo
{
	public interface IRepo : IModule
	{
		IList<IBranch> Branches { get; }
		IBranch Master { get; }
		Task<IList<ICommit>> GetCommitsAsync(IBranch branch, ShowTextRequest showTextRequest);
	}
}
