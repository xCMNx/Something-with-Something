using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace core
{
	public interface IParametersValueItem
	{
		object Value { get; set; }
	}

	public interface IParametersRequestItem
	{
		string Title { get; }
		IParametersValueItem Value { get; }
		string Hint { get; }
	}

	public class ParametersRequestItem : IParametersRequestItem
	{
		public string Title { get; set; }
		public IParametersValueItem Value { get; set; }
		public string Hint { get; set; }
	}

	public class CheckValueItem : IParametersValueItem
	{
		public string Title { get; set; }
		public bool IsChecked { get; set; }
		public object Value
		{
			get { return IsChecked; }
			set { IsChecked = (bool)value; }
		}
		public string Hint { get; set; }
		public CheckValueItem(bool isChecked, string title, string hint = null)
		{
			IsChecked = isChecked;
			Title = title;
			Hint = hint;
		}
	}

	public class ListValueItem : IParametersValueItem
	{
		public IList<IParametersValueItem> List { get; set; }
		public object Value
		{
			get { return List; }
			set { List = (IList<IParametersValueItem>)value; }
		}
		public ListValueItem(IList<IParametersValueItem> list)
		{
			List = list;
		}
	}

	public class ParametersValueItem : IParametersValueItem
	{
		public object Value { get; set; }
		public ParametersValueItem(object value)
		{
			Value = value;
		}
	}

	public class ComboListValueItem : ParametersValueItem
	{
		public IList<object> List { get; set; }
		public ComboListValueItem(IList<object> list, object value) : base(value)
		{
			List = list;
		}
	}

	public class StringValueItem : IParametersValueItem
	{
		public string String { get; set; }
		public object Value
		{
			get { return String; }
			set { String = (string)value; }
		}
		public StringValueItem(string val)
		{
			String = val;
		}
	}

	public class PasswordValueItem : StringValueItem
	{
		public PasswordValueItem(string val) : base(val) { }
	}

	public class PathValueItem : StringValueItem
	{
		public PathValueItem(string path) : base(path) { }
	}

	public delegate Task<bool> ParametersRequest(IList<IParametersRequestItem> requestFields, string title = null, params string[] messages);
	public delegate Task ShowTextRequest(string text, int delay = -1);

}
