using core;
using System.Linq;

namespace SwS
{
	public class Bookmark
	{
		public string Name { get; private set; }
		public string Title { get; private set; }
		public Bookmark(string name)
		{
			Name = name.Trim();
			Title = new string(Name.Words().Take(2).Select(s => char.ToUpper(s[0])).ToArray());
			if (string.IsNullOrWhiteSpace(Title))
				Title = char.ToUpper(Name[0]).ToString();
		}
	}
}
