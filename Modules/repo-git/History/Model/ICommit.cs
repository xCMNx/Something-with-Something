using System.Collections.Generic;

namespace Palmmedia.GitHistory.Core.Model
{
	/// <summary>
	/// Interface for commits.
	/// </summary>
	internal interface IGitCommit : core.Interfaces.Repo.ICommit
	{
		/// <summary>
		/// Gets the short identifier.
		/// </summary>
		/// <value>
		/// The short identifier.
		/// </value>
		string ShortId { get; }

		/// <summary>
		/// Gets the name of the branch (if available).
		/// </summary>
		/// <value>
		/// The name of the branch.
		/// </value>
		string BranchName { get; }

		/// <summary>
		/// Gets the parents of the commit.
		/// </summary>
		/// <value>
		/// The parents.
		/// </value>
		ICollection<IGitCommit> Parents { get; }
	}
}
