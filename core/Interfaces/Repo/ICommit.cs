using System;

namespace core.Interfaces.Repo
{
	public interface ICommit : IEntity
	{
		DateTime Date { get; }
		string Author { get; }
		string Message { get; }
	}
}
