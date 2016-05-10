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
		public string ConfigName { get; set; }
		public string Name => "Something";

		public event PropertyChangedEventHandler PropertyChanged;

		public async Task<bool> UpdateAsync(ParametersRequest parametersRequest, ShowText showText)
		{
			await Task.Yield();
			return false;
		}

		public async Task<bool> UpdateSettingsAsync(ParametersRequest parametersRequest, ShowText showText)
		{
			await Task.Yield();
			return false;
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
		public async Task<bool> CreateBranch(IIssue issue, ParametersRequest parametersRequest, ShowText showText)
		{
			await Task.Yield();
			return false;
		}

		public async Task<IBranch> GetBranch(IIssue issue, ParametersRequest parametersRequest, ShowText showText)
		{
			await Task.Yield();
			return null;
		}

		public async Task<IList<ICommit>> GetCommitsAsync(IIssue issue, ShowText showText)
		{
			await Task.Yield();
			return new ICommit[0];
		}

		public async Task<IList<ICommit>> GetCommitsAsync(IBranch branch, ShowText showText)
		{
			await Task.Yield();
			return new ICommit[0];
		}
	}
}
