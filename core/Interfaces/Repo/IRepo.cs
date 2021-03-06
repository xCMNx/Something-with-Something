﻿using System.Collections.Generic;
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
		Task<IBranch> CreateBranch(IIssue issue, ParametersRequest parametersRequest, ShowText showText);
		Task<IBranch> GetBranch(IIssue issue, ParametersRequest parametersRequest, ShowText showText);
		Task UpToDate(IBranch branch, ParametersRequest parametersRequest, ShowText showText);
		Task<bool> Switch(IBranch branch, ShowText showText);
	}
}
