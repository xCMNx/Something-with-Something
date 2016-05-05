using core.BaseClasses.Tracker;

namespace tracker_redmine
{
	public class RedmineIssue : IssueBase
	{
		public string Tracker { get; set; }
		public override string ToString()
		{
			return string.Format("{{{2}}}[{1}] #{3} {0}", Title, Tracker, Priority, Identifier);
		}
	}
}
