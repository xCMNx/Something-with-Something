using System.Collections.Generic;
using System.Linq;
using Palmmedia.GitHistory.Core.Model;

namespace Palmmedia.GitHistory.Core
{
	/// <summary>
	/// Retrieves commits from GIT repositories.
	/// </summary>
	internal static class GitHistory
	{
		/// <summary>
		/// Gets all commits from the given GIT repository.
		/// The commits also contain parent and child relationships.
		/// </summary>
		/// <param name="repo">The git Repository.</param>
		/// <returns>The commits.</returns>
		public static IEnumerable<GitCommit> GetCommits(LibGit2Sharp.Repository repo)
		{
			var commits = new Dictionary<string, GitCommit>();
			var parentsIdsByCommit = new Dictionary<GitCommit, IEnumerable<string>>();

			// Collect all commits from all branches
			foreach (var branch in repo.Branches)
			{
				foreach (var commit in branch.Commits)
				{
					GitCommit commitModel = null;

					if (!commits.TryGetValue(commit.Sha, out commitModel))
					{
						commitModel = new GitCommit(commit.Sha, commit.MessageShort, commit.Message, string.Format("{0} <{1}>", commit.Author.Name, commit.Author.Email), commit.Author.When.DateTime);
						commits.Add(commit.Sha, commitModel);
						parentsIdsByCommit.Add(commitModel, commit.Parents.Select(p => p.Sha).ToArray());
					}

					if (commit.Sha == branch.Tip.Sha)
					{
						// Branch name found
						commitModel.BranchName = branch.FriendlyName;
					}
				}
			}

			// Set parent and child relationships
			foreach (var kv in parentsIdsByCommit)
			{
				foreach (var parentId in kv.Value)
				{
					var customParent = commits[parentId];

					kv.Key.Parents.Add(customParent);
					customParent.Children.Add(kv.Key);
				}
			}

			return commits.Values.OrderBy(c => c.Date);
		}

		/// <summary>
		/// Gets all commits from the given GIT repository.
		/// The commits also contain parent and child relationships.
		/// </summary>
		/// <param name="directory">The directory.</param>
		/// <returns>The commits.</returns>
		public static IEnumerable<GitCommit> GetCommits(string directory)
		{
			// Collect all commits from all branches
			using (var repo = new LibGit2Sharp.Repository(directory))
			{
				return GetCommits(repo);
			}
		}

		/// <summary>
		/// Merges the given commits. Linear commits are merged to a single element.
		/// When a commit has several child commits merging stops (this means a branch was created).
		/// When one child commit has several parents merging stops as well (this means a merge of two branches was performed).
		/// </summary>
		/// <param name="commits">The commits.</param>
		/// <returns>The merged commits.</returns>
		public static IEnumerable<MergedCommit> GetMergedCommits(IEnumerable<GitCommit> commits)
		{
			var result = new List<MergedCommit>();

			HashSet<GitCommit> toProcess = new HashSet<GitCommit>(commits);

			while (toProcess.Count > 0)
			{
				var commit = toProcess.First(c => c.Parents.All(p => !toProcess.Contains(p)));

				var correspondingCommits = GetCorrespondingCommits(commit);
				var mergedCommit = new MergedCommit(correspondingCommits);

				foreach (var correspondingCommit in correspondingCommits)
				{
					correspondingCommit.MergedCommit = mergedCommit;
					toProcess.Remove(correspondingCommit);
				}

				result.Add(mergedCommit);
			}

			return result;
		}

		/// <summary>
		/// Gets the corresponding commits to the given commit.
		/// These commits could be merged.
		/// </summary>
		/// <param name="commit">The commit.</param>
		/// <returns>All corresponding commits.</returns>
		private static List<GitCommit> GetCorrespondingCommits(GitCommit commit)
		{
			var result = new List<GitCommit>();

			while (true)
			{
				result.Add(commit);

				// Condition 1: Stop before merge commits
				// Condition 2: No further children (last commit in a branch) or more than 1 child (new branch)
				if (commit.Children.Any(c => c.Parents.Count > 1)
					|| commit.Children.Count != 1)
				{
					break;
				}

				commit = commit.Children.First();
			}

			return result;
		}
	}
}
