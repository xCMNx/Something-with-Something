using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using core.Interfaces;
using core.Interfaces.Repo;
using core.Interfaces.Tracker;

namespace core.BaseClasses
{
	public class DummyModule : IModule
	{
		public string Name => "Something";

		public event PropertyChangedEventHandler PropertyChanged;

		public Task<bool> UpdateAsync(ParametersRequest parametersRequest, ShowText showText)
		{
			throw new NotImplementedException();
		}
	}
	public class DummyTracker : DummyModule, ITracker
	{
		public IList<IProject> Projects => new IProject[0];

		public Task<IList<IIssue>> GetIssuesAsync(IProject project, ShowText showText)
		{
			throw new NotImplementedException();
		}
	}
	public class DummyRepo : DummyModule, IRepo
	{
		public IList<IBranch> Branches => new IBranch[0];
		public IBranch Master => null;
		public Task<bool> CreateBranch(IIssue issue, ParametersRequest parametersRequest, ShowText showText)
		{
			throw new NotImplementedException();
		}

		public Task<IList<ICommit>> GetCommitsAsync(IIssue issue, ShowText showText)
		{
			throw new NotImplementedException();
		}

		public Task<IList<ICommit>> GetCommitsAsync(IBranch branch, ShowText showText)
		{
			throw new NotImplementedException();
		}
	}
}
