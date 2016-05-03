using core.Interfaces.Tracker;

namespace core.BaseClasses.Tracker
{
	public class IssueBase : EntityBase, IIssue
	{
		public string Description { get; set; }
		public int Priority { get; set; }
	}
}
