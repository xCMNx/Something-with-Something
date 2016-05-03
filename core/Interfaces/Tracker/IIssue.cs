namespace core.Interfaces.Tracker
{
	public interface IIssue : IEntity
	{
		string Description { get; }
		int Priority { get; }
	}
}
