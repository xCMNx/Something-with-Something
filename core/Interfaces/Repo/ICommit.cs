using System;

namespace core.Interfaces.Repo
{
	public interface ICommit : IEntity
	{
		DateTime Date { get; }
		string Autor { get; }
		string Message { get; }
	}
}
