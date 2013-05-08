////
//// ImmutableList.cs
////
//// Author:
////       Mike Krüger <mkrueger@xamarin.com>
////
//// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
////
//// Permission is hereby granted, free of charge, to any person obtaining a copy
//// of this software and associated documentation files (the "Software"), to deal
//// in the Software without restriction, including without limitation the rights
//// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//// copies of the Software, and to permit persons to whom the Software is
//// furnished to do so, subject to the following conditions:
////
//// The above copyright notice and this permission notice shall be included in
//// all copies or substantial portions of the Software.
////
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//// THE SOFTWARE.
//using System;
//using System.Collections.Generic;
//using System.Linq;
//
//namespace System.Collections.Immutable
//{
//	public class ImmutableList<T> : IImmutableList<T>
//	{
//		internal static readonly ImmutableList<T> Empty = new ImmutableList<T> ();
//
//		readonly AvlNode<T> root = AvlNode<T>.Empty;
//		readonly IEqualityComparer<T> valueComparer;
//
//		internal ImmutableList ()
//		{
//			this.valueComparer = EqualityComparer<T>.Default;
//		}
//
//		internal ImmutableList (AvlNode<T> root, IEqualityComparer<T> equalityComparer)
//		{
//			this.root = root;
//			this.valueComparer = equalityComparer;
//		}
//
//		#region IImmutableList implementation
//
//		public ImmutableList<T> Add (T value)
//		{
//			return new ImmutableList (root.InsertIntoNew (value, null), valueComparer);
//		}
//
//		IImmutableList<T> IImmutableList<T>.Add (T value)
//		{
//			return Add (value);
//		}
//
//		public ImmutableList<T> AddRange (IEnumerable<T> items)
//		{
//			return InsertRange (Count, items);
//		}
//
//		IImmutableList<T> IImmutableList<T>.AddRange (IEnumerable<T> items)
//		{
//			return AddRange (items);
//		}
//
//		public ImmutableList<T> Clear ()
//		{
//			return Empty;
//		}
//
//		IImmutableList<T> IImmutableList<T>.Clear ()
//		{
//			return Clear ();
//		}
//
//		public bool Contains (T value)
//		{
//			return IndexOf (value) != -1;
//		}
//
//		public int IndexOf (T value)
//		{
//			// TODO
//			return -1;
//		}
//
//		public ImmutableList<T> Insert (int index, T element)
//		{
//			if (index >= Count)
//				throw new ArgumentOutOfRangeException ("index");
//			// TODO
//		}
//
//		IImmutableList<T> IImmutableList<T>.Insert (int index, T element)
//		{
//			return Insert (index, element);
//		}
//
//		ImmutableList<T> InsertCollection (int index, ICollection<T> collection)
//		{
//			if (index > Count)
//				throw new ArgumentOutOfRangeException ("index");
//			// TODO
//		}
//
//		public ImmutableList<T> InsertRange (int index, IEnumerable<T> items)
//		{
//			var collection = items as ICollection<T>;
//			if (collection != null) 
//				return InsertCollection (index, collection);
//			return InsertCollection (index, items.ToArray ());
//		}
//
//		IImmutableList<T> IImmutableList<T>.InsertRange (int index, IEnumerable<T> items)
//		{
//			return InsertRange (index, items);
//		}
//
//		public ImmutableList<T> Remove (T value)
//		{
//			int loc = IndexOf (value);
//			if (loc != -1)
//				return RemoveAt (loc);
//
//			return this;
//		}
//
//		IImmutableList<T> IImmutableList<T>.Remove (T value)
//		{
//			return Remove (value);
//		}
//
//		public ImmutableList<T> RemoveAll (Predicate<T> match)
//		{
//			if (match == null)
//				throw new ArgumentNullException ("match");
//			var result = this;
//			for (int i = 0; i < result.Count; i++) {
//				if (match(result[i])) {
//					result = result.RemoveAt (i);
//					i--;
//					continue;
//				}
//			}
//			return result;
//		}
//
//		IImmutableList<T> IImmutableList<T>.RemoveAll (Predicate<T> match)
//		{
//			return RemoveAll (match);
//		}
//
//		public ImmutableList<T> RemoveAt (int index)
//		{
//			return RemoveRange (index, 1);
//		}
//
//		IImmutableList<T> IImmutableList<T>.RemoveAt (int index)
//		{
//			return RemoveAt (index);
//		}
//
//		void CheckRange (int idx, int count)
//		{
//			if (idx < 0)
//				throw new ArgumentOutOfRangeException ("index");
//
//			if (count < 0)
//				throw new ArgumentOutOfRangeException ("count");
//
//			if ((uint) idx + (uint)count > (uint)this.Count)
//				throw new ArgumentException ("index and count exceed length of list");
//		}
//
//		public ImmutableList<T> RemoveRange (int index, int count)
//		{
//			CheckRange (index, count);
//			// TODO
//		}
//
//		IImmutableList<T> IImmutableList<T>.RemoveRange (int index, int count)
//		{
//			return RemoveRange (index, count);
//		}
//
//		public ImmutableList<T> RemoveRange (IEnumerable<T> items)
//		{
//			var result = this;
//			foreach (var item in items) {
//				result = result.Remove (item);
//			}
//			return result;
//		}
//
//		IImmutableList<T> IImmutableList<T>.RemoveRange (IEnumerable<T> items)
//		{
//			return RemoveRange (items);
//		}
//
//		public ImmutableList<T> Replace (T oldValue, T newValue)
//		{
//			var idx = IndexOf (oldValue);
//			if (idx < 0)
//				return this;
//			return SetItem (idx, newValue);
//		}
//
//		IImmutableList<T> IImmutableList<T>.Replace (T oldValue, T newValue)
//		{
//			return Replace (oldValue, newValue);
//		}
//
//
//		public ImmutableList<T> SetItem (int index, T value)
//		{
//			if (index > Count)
//				throw new ArgumentOutOfRangeException ("index");
//			// TODO
//		}
//
//		IImmutableList<T> IImmutableList<T>.SetItem (int index, T value)
//		{
//			return SetItem (index, value);
//		}
//
//		public ImmutableList<T> WithComparer (IEqualityComparer<T> equalityComparer)
//		{
//			return new ImmutableList<T> (items, equalityComparer);
//		}
//
//		IImmutableList<T> IImmutableList<T>.WithComparer (IEqualityComparer<T> equalityComparer)
//		{
//			return WithComparer (equalityComparer);
//		}
//
//		public IEqualityComparer<T> ValueComparer {
//			get {
//				return valueComparer;
//			}
//		}
//		#endregion
//
//		#region IEnumerable implementation
//
//		public IEnumerator<T> GetEnumerator ()
//		{
//			foreach (var item in items) {
//				yield return item;
//			}
//		}
//
//		#endregion
//
//		#region IEnumerable implementation
//
//		IEnumerator IEnumerable.GetEnumerator ()
//		{
//			return GetEnumerator ();
//		}
//
//		#endregion
//
//		#region IReadOnlyList implementation
//
//		public T this [int index] {
//			get {
//				if (index >= Count)
//					throw new ArgumentOutOfRangeException ("index");
//				return items [index];
//			}
//		}
//
//		#endregion
//
//		#region IReadOnlyCollection implementation
//
//		public int Count {
//			get {
//				// TODO
////				return items.Length;
//			}
//		}
//
//		#endregion
//	}
//
//	public static class ImmutableList
//	{
//		public static ImmutableList<T> Create<T> ()
//		{
//			return ImmutableList<T>.Empty;
//		}
//
//		public static ImmutableList<T> Create<T> (IEqualityComparer<T> equalityComparer, params T[] items)
//		{
//			return new ImmutableList<T> (items, equalityComparer);
//		}
//
//		public static ImmutableList<T> Create<T> (params T[] items)
//		{
//			return Create (EqualityComparer<T>.Default, items);
//		}
//
//		public static ImmutableList<T> Create<T> (IEqualityComparer<T> equalityComparer, IEnumerable<T> items)
//		{
//			return Create (equalityComparer, items.ToArray ());
//		}
//
//		public static ImmutableList<T> Create<T> (IEnumerable<T> items)
//		{
//			return Create (items.ToArray ());
//		}
//
//		public static ImmutableList<T> Create<T> (IEqualityComparer<T> equalityComparer, T item)
//		{
//			return new ImmutableList<T> (new T[] { item }, equalityComparer);;
//		}
//
//		public static ImmutableList<T> Create<T> (T item)
//		{
//			return Create (EqualityComparer<T>.Default, item);
//		}
//
//		public static ImmutableList<T> Create<T> (IEqualityComparer<T> equalityComparer)
//		{
//			return Create<T> ().WithComparer (equalityComparer);
//		}
//
//		public static ImmutableList<T> ToImmutableList<T> (this IEnumerable<T> source)
//		{
//			if (source == null)
//				throw new ArgumentNullException ("source");
//			return Create<T> ().AddRange (source);
//		}
//
//		public static ImmutableList<T> ToImmutableList<T> (this IEnumerable<T> source, IEqualityComparer<T> equalityComparer)
//		{
//			if (source == null)
//				throw new ArgumentNullException ("source");
//			return Create<T> ().WithComparer (equalityComparer).AddRange (source);
//		}
//	}
//}
