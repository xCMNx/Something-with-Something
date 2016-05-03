using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace core
{

	/// <summary> 
	/// Represents a dynamic data collection that provides notifications when items get added, removed, or when the whole list is refreshed. 
	/// </summary> 
	/// <typeparam name="T"></typeparam> 
	public class ObservableCollectionEx<T> : ObservableCollection<T>
	{
		/// <summary> 
		/// Adds the elements of the specified collection to the end of the ObservableCollection(Of T). 
		/// </summary> 
		public virtual void AddRange(IEnumerable<T> collection)
		{
			if (collection == null) return;

			foreach (var i in collection) Items.Add(i);
			var newItems = collection.ToArray();
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems));
		}

		/// <summary> 
		/// Removes the first occurence of each item in the specified collection from ObservableCollection(Of T). 
		/// </summary> 
		public virtual void RemoveRange(IEnumerable<T> collection)
		{
			if (collection == null) return;

			foreach (var i in collection) Items.Remove(i);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, collection.ToArray()));
		}

		/// <summary> 
		/// Clears the current collection and reset it with the specified item. 
		/// </summary> 
		public virtual void Reset(T item)
		{
			Reset(new T[] { item });
		}

		/// <summary> 
		/// Clears the current collection and reset it with the specified collection. 
		/// </summary> 
		public virtual void Reset(IEnumerable<T> collection)
		{
			Items.Clear();
			if (collection != null)
				foreach (var i in collection) Items.Add(i);
			OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		/// <summary> 
		/// Initializes a new instance of the System.Collections.ObjectModel.ObservableCollection(Of T) class. 
		/// </summary> 
		public ObservableCollectionEx() : base() { }

		/// <summary> 
		/// Initializes a new instance of the System.Collections.ObjectModel.ObservableCollection(Of T) class that contains elements copied from the specified collection. 
		/// </summary> 
		/// <param name="collection">collection: The collection from which the elements are copied.</param> 
		/// <exception cref="System.ArgumentNullException">The collection parameter cannot be null.</exception> 
		public ObservableCollectionEx(IEnumerable<T> collection) : base(collection) { }
	}
}