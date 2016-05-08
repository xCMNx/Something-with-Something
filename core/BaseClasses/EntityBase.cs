using core.Interfaces;

namespace core.BaseClasses
{
	public class EntityBase : IEntity
	{
		public object Identifier { get; set; }
		public string Title { get; set; }

		public override string ToString()
		{
			return string.Format("{0} #{1}", Title, Identifier);
		}
	}
}
