//
// ImmutableDictionary.cs
//
// Contains code from ACIS P2P Library (https://github.com/ptony82/brunet)
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
using System.Linq;

namespace System.Collections.Immutable
{
	/** Read-only immutable data structure for IComparable Keys
	 * Implemented as a readonly binary AVL tree, so most operations
	 * have 1.44 Log C complexity where C is the count.
	 *
	 * http://en.wikipedia.org/wiki/AVL_tree
	  
	 * To modify, use the InsertIntoNew and RemoveFromNew methods
	 * which return a new instance with minimal changes (about Log C),
	 * so this is an efficient way to make changes without having
	 * to copy the entire data structure.
	 * 
	 * Clearly this is a thread-safe class (because it is read-only),
	 * but note: if the K or V types are not immutable, you could have
	 * a problem: someone could modify the object without changing the 
	 * dictionary and not only would the Dictionary be incorrectly ordered
	 * you could have race conditions.  It is required that you only use
	 * immutable key types in the dictionary, and only thread-safe if
	 * both the keys and values are immutable.
	 */
	public class ImmutableDictionary<TKey, TValue> : IImmutableDictionary<TKey, TValue> where TKey : System.IComparable<TKey>
	{
		internal static readonly ImmutableDictionary<TKey, TValue> Empty = new ImmutableDictionary<TKey, TValue> ();

		AvlNode<KeyValuePair<TKey, TValue>> root = AvlNode<KeyValuePair<TKey, TValue>>.Empty;

		readonly IEqualityComparer<TKey> keyComparer;
		readonly IEqualityComparer<TValue> valueComparer;

		internal ImmutableDictionary ()
		{
		}

		internal ImmutableDictionary (AvlNode<KeyValuePair<TKey, TValue>> root, IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
		{
			this.root = root;
			this.keyComparer = keyComparer;
			this.valueComparer = valueComparer;
		}

		public ImmutableDictionary<TKey, TValue> WithComparers (IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
		{
			return new ImmutableDictionary<TKey, TValue> (root, keyComparer, valueComparer);
		}

		public ImmutableDictionary<TKey, TValue> WithComparers (IEqualityComparer<TKey> keyComparer)
		{
			return WithComparers (keyComparer, valueComparer);
		}

		#region IImmutableDictionary implementation

		public ImmutableDictionary<TKey, TValue> Add (TKey key, TValue value)
		{
			var pair = new KeyValuePair<TKey, TValue> (key, value);
			return new ImmutableDictionary<TKey, TValue> (root.InsertIntoNew (pair, CompareKV), keyComparer, valueComparer);
		}

		IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Add (TKey key, TValue value)
		{
			return Add (key, value);
		}

		public ImmutableDictionary<TKey, TValue> AddRange (IEnumerable<KeyValuePair<TKey, TValue>> pairs)
		{
			var result = this;
			foreach (var kv in pairs)
				result = result.Add (kv.Key, kv.Value);
			return result;
		}

		IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.AddRange (IEnumerable<KeyValuePair<TKey, TValue>> pairs)
		{
			return AddRange (pairs);
		}

		public ImmutableDictionary<TKey, TValue> Clear ()
		{
			return Empty;
		}

		IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Clear ()
		{
			return Clear ();
		}

		static int CompareKV (KeyValuePair<TKey, TValue> left, KeyValuePair<TKey, TValue> right)
		{
			return left.Key.CompareTo (right.Key);
		}

		public bool Contains (KeyValuePair<TKey, TValue> kv)
		{
			var node = root.SearchNode (kv, CompareKV);
			return !node.IsEmpty && valueComparer.Equals (node.Value.Value, kv.Value);
		}

		public ImmutableDictionary<TKey, TValue> Remove (TKey key)
		{
			var old = root;
			var pair = new KeyValuePair<TKey,TValue> (key, default (TValue));
			return new ImmutableDictionary<TKey, TValue> (root.RemoveFromNew (pair, CompareKV, out old), keyComparer, valueComparer);
		}

		IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Remove (TKey key)
		{
			return Remove (key);
		}

		public IImmutableDictionary<TKey, TValue> RemoveRange (IEnumerable<TKey> keys)
		{
			IImmutableDictionary<TKey, TValue> result = this;
			foreach (var key in keys) {
				result = result.Remove (key);
			}
			return result;
		}

		IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.RemoveRange (IEnumerable<TKey> keys)
		{
			return RemoveRange (keys);
		}

		public ImmutableDictionary<TKey, TValue> SetItem (TKey key, TValue value)
		{
			var result = this;
			if (result.ContainsKey (key))
				result = result.Remove (key);
			return result.Add (key, value);
		}

		IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItem (TKey key, TValue value)
		{
			return SetItem (key, value);
		}

		public IImmutableDictionary<TKey, TValue> SetItems (IEnumerable<KeyValuePair<TKey, TValue>> items)
		{
			var result = this;
			foreach (var kv in items) {
				result = result.SetItem (kv.Key, kv.Value);
			}
			return result;
		}

		IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItems (IEnumerable<KeyValuePair<TKey, TValue>> items)
		{
			return SetItems (items);
		}

		public IEqualityComparer<TKey> KeyComparer {
			get {
				return keyComparer;
			}
		}

		public IEqualityComparer<TValue> ValueComparer {
			get {
				return valueComparer;
			}
		}
	
		#endregion

		#region IReadOnlyDictionary implementation

		public bool ContainsKey (TKey key)
		{
			return !root.SearchNode (new KeyValuePair<TKey, TValue> (key, default(TValue)), CompareKV).IsEmpty;
		}

		public bool TryGetValue (TKey key, out TValue value)
		{
			var node = root.SearchNode (new KeyValuePair<TKey, TValue> (key, default(TValue)), CompareKV);
			if (node.IsEmpty) {
				value = default (TValue);
				return false;
			}
			value = node.Value.Value;
			return true;
		}

		public TValue this [TKey key] {
			get {
				var node = root.SearchNode (new KeyValuePair<TKey, TValue> (key, default(TValue)), CompareKV);
				if (!node.IsEmpty)
					return node.Value.Value;
				throw new KeyNotFoundException (String.Format ("Key: {0}", key));
			}
		}

		public IEnumerable<TKey> Keys {
			get {
				foreach (var kv in this) {
					yield return kv.Key;
				}
			}
		}

		public IEnumerable<TValue> Values {
			get {
				foreach (var kv in this) {
					yield return kv.Value;
				}
			}
		}
		#endregion

		#region IEnumerable implementation

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator ()
		{
			var to_visit = new Stack<AvlNode<KeyValuePair<TKey,TValue>>> ();
			to_visit.Push (root);
			while (to_visit.Count > 0) {
				var this_d = to_visit.Pop ();
				if (this_d.IsEmpty) {
					continue;
				}
				if (this_d.left.IsEmpty) {
					//This is the next smallest value in the Dict:
					yield return this_d.Value;
					to_visit.Push (this_d.right);
				} else {
					//Break it up
					to_visit.Push (this_d.right);
					to_visit.Push (new AvlNode<KeyValuePair<TKey, TValue>> (this_d.Value));
					to_visit.Push (this_d.left);
				}
			}
		}
		#endregion

		#region IEnumerable implementation

		IEnumerator IEnumerable.GetEnumerator ()
		{
			return GetEnumerator ();
		}
		#endregion

		#region IReadOnlyCollection implementation

		public int Count {
			get {
				return root.Count;
			}
		}

		#endregion

//		public override bool Equals (object o)
//		{
//			if (object.ReferenceEquals (this, o)) {
//				return true;
//			}
//			var other = o as ImmutableDictionary<TKey, TValue>;
//			if (other != null) {
//				//Equivalent must have same count:
//				if (other._count != _count) {
//					return false;
//				}
//				//Now go element by element:
//				bool all_equal = true;
//				//Enumeration goes in a sorted order:
//				var this_enum = GetEnumerator ();
//				var o_enum = other.GetEnumerator ();
//				while (all_equal) {
//					this_enum.MoveNext ();
//					//Since we have the same count, this must return same as above
//					//Both are finished, but were equal to this point:
//					if (!o_enum.MoveNext ()) {
//						return true;
//					}
//					var tkv = this_enum.Current;
//					var okv = o_enum.Current;
//					all_equal = tkv.Key.Equals (okv.Key) &&
//					//Handle case of null values:
//						(null != tkv.Value ? tkv.Value.Equals (okv.Value)
//						 : null == okv.Value);
//				}
//				return all_equal;
//			}
//			return false;
//
//		}	
//
//		public override int GetHashCode ()
//		{
//			var imd = Min;
//			if (imd != Empty)
//				return imd.Key.GetHashCode () ^ (imd.Value != null ? imd.Value.GetHashCode () : 0);
//			return 0;
//		}
	}

	public static class ImmutableDictionary
	{
		public static bool Contains<TKey, TValue> (this IImmutableDictionary<TKey, TValue> dictionary, TKey key, TValue value)
		{
			if (dictionary == null)
				throw new ArgumentNullException ("dictionary");
			return dictionary.Contains (new KeyValuePair<TKey, TValue> (key, value));
		}

		public static ImmutableDictionary<TKey, TValue> Create<TKey, TValue> () where TKey : System.IComparable<TKey>
		{
			return ImmutableDictionary<TKey, TValue>.Empty;
		}

		public static ImmutableDictionary<TKey, TValue> Create<TKey, TValue> (IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer, IEnumerable<KeyValuePair<TKey, TValue>> items) where TKey : System.IComparable<TKey>
		{
			return Create<TKey, TValue> (keyComparer, valueComparer).AddRange (items);
		}

		public static ImmutableDictionary<TKey, TValue> Create<TKey, TValue> (IEqualityComparer<TKey> keyComparer, IEnumerable<KeyValuePair<TKey, TValue>> items) where TKey : System.IComparable<TKey>
		{
			return Create<TKey, TValue> (keyComparer).AddRange (items);
		}

		public static ImmutableDictionary<TKey, TValue> Create<TKey, TValue> (IEnumerable<KeyValuePair<TKey, TValue>> items) where TKey : System.IComparable<TKey>
		{
			return Create<TKey, TValue> ().AddRange (items);
		}
		public static ImmutableDictionary<TKey, TValue> Create<TKey, TValue> (IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer) where TKey : System.IComparable<TKey>
		{
			return Create<TKey, TValue> ().WithComparers (keyComparer, valueComparer);
		}

		public static ImmutableDictionary<TKey, TValue> Create<TKey, TValue> (IEqualityComparer<TKey> keyComparer) where TKey : System.IComparable<TKey>
		{
			return Create<TKey, TValue> ().WithComparers (keyComparer);
		}

		public static TValue GetValueOrDefault<TKey, TValue> (this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key) where TKey : System.IComparable<TKey>
		{
			return dictionary.GetValueOrDefault<TKey, TValue> (key, default (TValue));
		}

		public static TValue GetValueOrDefault<TKey, TValue> (this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) where TKey : System.IComparable<TKey>
		{
			if (dictionary == null)
				throw new ArgumentNullException ("dictionary");
			TValue result;
			if (dictionary.TryGetValue (key, out result))
				return result;
			return defaultValue;
		}

		public static TValue GetValueOrDefault<TKey, TValue> (this IDictionary<TKey, TValue> dictionary, TKey key) where TKey : System.IComparable<TKey>
		{
			return dictionary.GetValueOrDefault<TKey, TValue> (key, default (TValue));
		}

		public static TValue GetValueOrDefault<TKey, TValue> (this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue) where TKey : System.IComparable<TKey>
		{
			if (dictionary == null)
				throw new ArgumentNullException ("dictionary");
			TValue result;
			if (dictionary.TryGetValue (key, out result))
				return result;
			return defaultValue;
		}

		public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue> (this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> keyComparer) where TKey : System.IComparable<TKey>
		{
			return source.ToImmutableDictionary (keyComparer, null);
		}

		public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue> (this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : System.IComparable<TKey>
		{
			return source.ToImmutableDictionary (null, null);
		}

		public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue> (this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer) where TKey : System.IComparable<TKey>
		{
			if (source == null)
				throw new ArgumentNullException ("dictionary");
			return Create<TKey, TValue> (keyComparer, valueComparer).AddRange (source);
		}

		public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TSource, TKey, TValue> (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector, IEqualityComparer<TKey> keyComparer) where TKey : System.IComparable<TKey>
		{
			return source.ToImmutableDictionary (keySelector, elementSelector, keyComparer, null);
		}

		public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TSource, TKey, TValue> (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector, IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer) where TKey : System.IComparable<TKey>
		{
			if (source == null)
				throw new ArgumentNullException ("dictionary");
			return Create<TKey, TValue> (keyComparer, valueComparer).AddRange (source.Select (x => new KeyValuePair<TKey, TValue>(keySelector(x), elementSelector(x))));
		}

		public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TSource, TKey, TValue> (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> elementSelector) where TKey : System.IComparable<TKey>
		{
			return source.ToImmutableDictionary (keySelector, elementSelector, null, null);
		}
	}
}

