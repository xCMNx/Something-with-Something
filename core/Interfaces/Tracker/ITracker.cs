using System.Collections.Generic;
using System.Threading.Tasks;

namespace core.Interfaces.Tracker
{
	public interface ITracker : IModule
	{
		IList<IProject> Projects { get; }
		Task<IList<IIssue>> GetIssuesAsync(IProject project, ShowTextRequest showTextRequest);
	}
}
