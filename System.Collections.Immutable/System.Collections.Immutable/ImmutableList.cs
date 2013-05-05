//
// ImmutableList.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;

namespace System.Collections.Immutable
{
	public class ImmutableList<T> : IImmutableList<T>
	{
		const int DefaultCapacity = 4;

		public static readonly ImmutableList<T> Empty = new ImmutableList<T> ();

		readonly T[] items;
		readonly int count;
		readonly IEqualityComparer<T> valueComparer;

		public ImmutableList ()
		{
			this.valueComparer = EqualityComparer<T>.Default;
			this.items = new T[DefaultCapacity];
		}

		ImmutableList (T[] items, int count, IEqualityComparer<T> equalityComparer)
		{
			this.items = items;
			this.count = count;
			this.valueComparer = equalityComparer;
		}

		#region IImmutableList implementation
		T[] GrowIfNeeded (int newCount)
		{
			var result = items;
			int minimumSize = count + newCount;
			if (minimumSize > result.Length) {
				int capacity = Math.Max (Math.Max (items.Length * 2, DefaultCapacity), minimumSize);
				result = new T[capacity];
				Array.Copy (items, result, count);
			}
			return result;
		}

		public IImmutableList<T> Add (T value)
		{
			var newItems = GrowIfNeeded (1);
			int newSize  = count;
			newItems [newSize++] = value;
			return new ImmutableList<T> (newItems, newSize, valueComparer);
		}

		public IImmutableList<T> AddRange (IEnumerable<T> items)
		{
			IImmutableList<T> result = this;
			foreach (var item in items)
				result = result.Add (item);
			return result;
		}

		public IImmutableList<T> Clear ()
		{
			return Empty;
		}

		public bool Contains (T value)
		{
			return IndexOf (value) != -1;
		}

		public int IndexOf (T value)
		{
			for (int i = 0; i < count; i++) {
				if (valueComparer.Equals (value, items [i]))
					return i;
			}
			return -1;
		}

		public IImmutableList<T> Insert (int index, T element)
		{
			if (index >= count)
				throw new ArgumentOutOfRangeException ("index");

			int capacity = Math.Max (Math.Max (items.Length * 2, DefaultCapacity), items.Length + 1);
			var newItems = new T[capacity];

			Array.Copy (items, newItems, index);
			Array.Copy (items, index, newItems, index + 1, this.count - index);
			newItems [index] = element;
			return new ImmutableList<T> (newItems, this.count + 1, valueComparer);
		}

		IImmutableList<T> InsertCollection (int index, ICollection<T> collection)
		{
			if (index >= count)
				throw new ArgumentOutOfRangeException ("index");

			int capacity = Math.Max (Math.Max (items.Length * 2, DefaultCapacity), items.Length + collection.Count);
			var newItems = new T[capacity];

			Array.Copy (items, newItems, index);
			Array.Copy (items, index, newItems, index + collection.Count, this.count - index);
			collection.CopyTo (newItems, index);
			return new ImmutableList<T> (newItems, this.count + collection.Count, valueComparer);
		}

		public IImmutableList<T> InsertRange (int index, IEnumerable<T> items)
		{
			var collection = items as ICollection<T>;
			if (collection != null) 
				return InsertCollection (index, collection);

			IImmutableList<T> result = this;
			foreach (T t in items)
				result = result.Insert (index++, t);		
			return result;
		}

		public IImmutableList<T> Remove (T value)
		{
			int loc = IndexOf (value);
			if (loc != -1)
				return RemoveAt (loc);

			return this;
		}

		public IImmutableList<T> RemoveAll (Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException ("match");
			IImmutableList<T> result = this;
			for (int i = 0; i < result.Count; i++) {
				if (match(result[i])) {
					result = result.RemoveAt (i);
					i--;
					continue;
				}
			}
			return result;
		}

		public IImmutableList<T> RemoveAt (int index)
		{
			return RemoveRange (index, 1);
		}

		void CheckRange (int idx, int count)
		{
			if (idx < 0)
				throw new ArgumentOutOfRangeException ("index");

			if (count < 0)
				throw new ArgumentOutOfRangeException ("count");

			if ((uint) idx + (uint) count > (uint) this.count)
				throw new ArgumentException ("index and count exceed length of list");
		}

		public IImmutableList<T> RemoveRange (int index, int count)
		{
			CheckRange (index, count);
			int capacity = Math.Max (Math.Max (items.Length * 2, DefaultCapacity), items.Length);
			var newItems = new T[capacity];
			Array.Copy (items, newItems, index);
			Array.Copy (items, index + count, newItems, index, this.count - index - count);
			return new ImmutableList<T> (newItems, this.count - count, valueComparer);
		}

		public IImmutableList<T> RemoveRange (IEnumerable<T> items)
		{
			IImmutableList<T> result = this;
			foreach (var item in items) {
				result = result.Remove (item);
			}
			return result;
		}

		public IImmutableList<T> Replace (T oldValue, T newValue)
		{
			var idx = IndexOf (oldValue);
			if (idx < 0)
				return this;
			return SetItem (idx, newValue);
		}

		public IImmutableList<T> SetItem (int index, T value)
		{
			if (index > count)
				throw new ArgumentOutOfRangeException ("index");

			var newItems = GrowIfNeeded (0);
			newItems [index] = value;
			return new ImmutableList<T> (newItems, count, valueComparer);
		}

		public IImmutableList<T> WithComparer (IEqualityComparer<T> equalityComparer)
		{
			return new ImmutableList<T> (items, count, equalityComparer);
		}

		public IEqualityComparer<T> ValueComparer {
			get {
				return valueComparer;
			}
		}
		#endregion

		#region IEnumerable implementation

		public IEnumerator<T> GetEnumerator ()
		{
			for (int i = 0; i < count; i++) {
				yield return items [i];
			}
		}

		#endregion

		#region IEnumerable implementation

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}

		#endregion

		#region IReadOnlyList implementation

		public T this [int index] {
			get {
				if (index >= count)
					throw new ArgumentOutOfRangeException ("index");
				return items [index];
			}
		}

		#endregion

		#region IReadOnlyCollection implementation

		public int Count {
			get {
				return count;
			}
		}

		#endregion
	}
}

