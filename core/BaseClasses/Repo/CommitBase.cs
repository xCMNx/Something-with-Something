using System;
using core.Interfaces.Repo;

namespace core.BaseClasses.Repo
{
	public class CommitBase : EntityBase, ICommit
	{
		public string Author { get; set; }
		public DateTime Date { get; set; }
		public string Message { get; set; }
	}
}
