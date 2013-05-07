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

		readonly TKey Key;
		readonly TValue Value;
		readonly ImmutableDictionary<TKey,TValue> LTDict;
		readonly ImmutableDictionary<TKey,TValue> GTDict;
		readonly int _count;
		readonly int _depth;
		readonly IEqualityComparer<TKey> keyComparer;
		readonly IEqualityComparer<TValue> valueComparer;

		internal ImmutableDictionary ()
		{
			Key = default(TKey);
			Value = default(TValue);
			LTDict = null;
			GTDict = null; 
			_count = 0;
			_depth = 0;
		}

		ImmutableDictionary (TKey key, TValue val) : this(key, val, Empty, Empty)
		{
		}

		ImmutableDictionary (TKey key, TValue val, ImmutableDictionary<TKey,TValue> lt, ImmutableDictionary<TKey,TValue> gt)
		{
			keyComparer = EqualityComparer<TKey>.Default;
			valueComparer = EqualityComparer<TValue>.Default;
			Key = key;
			Value = val;
			LTDict = lt;
			GTDict = gt;
			_count = 1 + LTDict._count + GTDict._count;
			_depth = 1 + Math.Max (LTDict._depth, GTDict._depth);
		}

		ImmutableDictionary (List<KeyValuePair<TKey,TValue>> kvs, int start,
		                    int upbound, bool sort)
		{
			keyComparer = EqualityComparer<TKey>.Default;
			valueComparer = EqualityComparer<TValue>.Default;

			int count = upbound - start;
			if (count == 0) {
				//Can't handle this case
				throw new Exception ("Can't create an Empty ImmutableDictionary this way, use Empty");
			}
			if (sort) {
				kvs.Sort (this.CompareKV);
			}
			int mid = start + (count / 2);
			Key = kvs [mid].Key;
			Value = kvs [mid].Value;
			LTDict = (mid > start) ? new ImmutableDictionary<TKey,TValue> (kvs, start, mid, false) : Empty;
			GTDict = (upbound > (mid + 1)) ?
				new ImmutableDictionary<TKey,TValue> (kvs, mid + 1, upbound, false)
					: Empty;
			_count = count;
			_depth = 1 + Math.Max (LTDict._depth, GTDict._depth);
		}

		internal ImmutableDictionary (IList<KeyValuePair<TKey,TValue>> kvs) : this(new List<KeyValuePair<TKey,TValue>>(kvs), 0, kvs.Count, true)
		{
			keyComparer = EqualityComparer<TKey>.Default;
			valueComparer = EqualityComparer<TValue>.Default;
		}

		ImmutableDictionary (TKey key, TValue value, ImmutableDictionary<TKey, TValue> lTDict, ImmutableDictionary<TKey, TValue> gTDict, int _count, int _depth, IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
		{
			this.Key = key;
			this.Value = value;
			this.LTDict = lTDict;
			this.GTDict = gTDict;
			this._count = _count;
			this._depth = _depth;
			this.keyComparer = keyComparer;
			this.valueComparer = valueComparer;
		}

		public ImmutableDictionary<TKey, TValue> WithComparers (IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
		{
			return new ImmutableDictionary<TKey, TValue> (Key, Value, LTDict, GTDict, _count, _depth, keyComparer, valueComparer);
		}

		public ImmutableDictionary<TKey, TValue> WithComparers (IEqualityComparer<TKey> keyComparer)
		{
			return WithComparers (keyComparer, valueComparer);
		}

		class MaxToMinEnumerable<K1,V1> : IEnumerable, IEnumerable<KeyValuePair<K1,V1>> where K1 : IComparable<K1>
		{
			readonly ImmutableDictionary<K1,V1> _dict;

			public MaxToMinEnumerable (ImmutableDictionary<K1,V1> dict)
			{
				_dict = dict;
			}

			public IEnumerator<KeyValuePair<K1,V1>> GetEnumerator ()
			{
				return _dict.GetMaxToMinEnumerator ();
			}

			IEnumerator IEnumerable.GetEnumerator ()
			{
				return _dict.GetMaxToMinEnumerator ();
			}
		}

		#region IImmutableDictionary implementation

		public ImmutableDictionary<TKey, TValue> Add (TKey key, TValue value)
		{
			return InsertIntoNew (key, value);
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

		public bool Contains (KeyValuePair<TKey, TValue> kv)
		{
			var node = GetKey (kv.Key);
			return (node != Empty) && object.Equals (node.Value, kv.Value);
		}

		public ImmutableDictionary<TKey, TValue> Remove (TKey key)
		{
			var old = this;
			return RemoveFromNew (key, out old);
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
			result.Add (key, value);
			return result;
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
			return GetKey (key) != Empty;
		}

		public bool TryGetValue (TKey key, out TValue value)
		{
			var node = GetKey (key);
			if (node.IsEmpty) {
				value = default (TValue);
				return false;
			}
			value = node.Value;
			return true;
		}

		public TValue this [TKey key] {
			get {
				var node = GetKey (key);
				if (!node.IsEmpty)
					return node.Value;
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
			var to_visit = new Stack<ImmutableDictionary<TKey,TValue>> ();
			to_visit.Push (this);
			while (to_visit.Count > 0) {
				var this_d = to_visit.Pop ();
				if (this_d.IsEmpty) {
					continue;
				}
				if (this_d.LTDict.IsEmpty) {
					//This is the next smallest value in the Dict:
					yield return new KeyValuePair<TKey,TValue> (this_d.Key, this_d.Value);
					to_visit.Push (this_d.GTDict);
				} else {
					//Break it up
					to_visit.Push (this_d.GTDict);
					to_visit.Push (new ImmutableDictionary<TKey,TValue> (this_d.Key, this_d.Value));
					to_visit.Push (this_d.LTDict);
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
				return _count;
			}
		}

		#endregion

		int Balance {
			get {
				if (IsEmpty) {
					return 0;
				}
				return LTDict._depth - GTDict._depth;
			}
		}

		int Depth {
			get {
				return _depth;
			}
		}

		public bool IsEmpty { get { return _count == 0; } }

		/// <summary>
		/// Return the subtree with the min value at the root, or Empty if Empty
		/// </summary>
		ImmutableDictionary<TKey,TValue> Min {
			get {
				if (IsEmpty)
					return Empty;
				var dict = this;
				var next = dict.LTDict;
				while (next != Empty) {
					dict = next;
					next = dict.LTDict;
				}
				return dict;
			}
		}

		int CompareKV (KeyValuePair<TKey,TValue> kv0, KeyValuePair<TKey,TValue> kv1)
		{
			return kv0.Key.CompareTo (kv1.Key);
		}

		/// <summary>
		/// Fix the root balance if LTDict and GTDict have good balance
		/// Used to keep the depth less than 1.44 log_2 N (AVL tree)
		/// </summary>
		ImmutableDictionary<TKey,TValue> FixRootBalance ()
		{
			int bal = Balance;
			if (Math.Abs (bal) < 2)
				return this;

			if (bal == 2) {
				if (LTDict.Balance == 1 || LTDict.Balance == 0) {
					//Easy case:
					return this.RotateToGT ();
				}
				if (LTDict.Balance == -1) {
					//Rotate LTDict:
					var newlt = LTDict.RotateToLT ();
					var newroot = new ImmutableDictionary<TKey,TValue> (Key, Value, newlt, GTDict);
					return newroot.RotateToGT ();
				}
				throw new Exception (String.Format ("LTDict too unbalanced: {0}", LTDict.Balance));
			}
			if (bal == -2) {
				if (GTDict.Balance == -1 || GTDict.Balance == 0) {
					//Easy case:
					return this.RotateToLT ();
				}
				if (GTDict.Balance == 1) {
					//Rotate GTDict:
					var newgt = GTDict.RotateToGT ();
					var newroot = new ImmutableDictionary<TKey,TValue> (Key, Value, LTDict, newgt);
					return newroot.RotateToLT ();
				}
				throw new Exception (String.Format ("LTDict too unbalanced: {0}", LTDict.Balance));
			}
			//In this case we can show: |bal| > 2
			//if( Math.Abs(bal) > 2 ) {
			throw new Exception (String.Format ("Tree too out of balance: {0}", Balance));
		}

		ImmutableDictionary<TKey,TValue> GetKey (TKey key)
		{
			var dict = this;
			while (dict != Empty) {
				int comp = dict.Key.CompareTo (key);
				if (comp < 0) {
					dict = dict.GTDict;
				} else if (comp > 0) {
					dict = dict.LTDict;
				} else {
					//Awesome:
					return dict;
				}
			}
			return Empty;
		}

		/// <summary>
		/// Return a new tree with the key-value pair inserted
		/// If the key is already present, it replaces the value
		/// This operation is O(Log N) where N is the number of keys
		/// </summary>
		ImmutableDictionary<TKey,TValue> InsertIntoNew (TKey key, TValue val)
		{
			if (IsEmpty)
				return new ImmutableDictionary<TKey,TValue> (key, val);
			TKey newk = Key;
			TValue newv = Value;
			ImmutableDictionary<TKey,TValue> newlt = LTDict;
			ImmutableDictionary<TKey,TValue> newgt = GTDict;

			int comp = Key.CompareTo (key);
			if (comp < 0) {
				//Let the GTDict put it in:
				newgt = GTDict.InsertIntoNew (key, val);
			} else if (comp > 0) {
				//Let the LTDict put it in:
				newlt = LTDict.InsertIntoNew (key, val);
			} else {
				//Replace the current value:
				newk = key;
				newv = val;
			}
			var newroot = new ImmutableDictionary<TKey,TValue> (newk, newv, newlt, newgt);
			return newroot.FixRootBalance ();
		}

		/// <summary>
		/// Merge two Dictionaries into one.
		/// </summary>
		ImmutableDictionary<TKey,TValue> Merge (ImmutableDictionary<TKey,TValue> one,
		                               ImmutableDictionary<TKey,TValue> two)
		{
			if (two._count > one._count) {
				//Swap them so the sub-merge is on the smaller:
				var temp = two;
				two = one;
				one = temp;
			}
			ImmutableDictionary<TKey,TValue> min;
			/*
			 * A nice recursive algorithm is just return Merge,
			 * rather than loop, but I'm afraid O(N) recursions
			 * will cause .Net to explode EVEN THOUGH IT IS TAIL
			 * RECURSION!  (they should use tailcall).
			 */
			while (two._count > 0) {
				two = two.RemoveMin (out min);
				one = one.InsertIntoNew (min.Key, min.Value);
			}
			return one;
		}

		/// <summary>
		/// Try to remove the key, and return the resulting Dict
		/// if the key is not found, old_node is Empty, else old_node is the Dict
		/// with matching Key
		/// </summary>
		ImmutableDictionary<TKey,TValue> RemoveFromNew (TKey key, out ImmutableDictionary<TKey,TValue> old_node)
		{
			if (IsEmpty) {
				old_node = Empty;
				return Empty;
			}
			int comp = Key.CompareTo (key);
			if (comp < 0) {
				var newgt = GTDict.RemoveFromNew (key, out old_node);
				if (old_node.IsEmpty) {
					//Not found, so nothing changed
					return this;
				}
				var newroot = new ImmutableDictionary<TKey,TValue> (Key, Value, LTDict, newgt);
				return newroot.FixRootBalance ();
			}
			if (comp > 0) {
				var newlt = LTDict.RemoveFromNew (key, out old_node);
				if (old_node.IsEmpty) {
					//Not found, so nothing changed
					return this;
				}
				var newroot = new ImmutableDictionary<TKey,TValue> (Key, Value, newlt, GTDict);
				return newroot.FixRootBalance ();
			}
			//found it
			old_node = this;
			return RemoveRoot ();
		}

		ImmutableDictionary<TKey,TValue> RemoveMax (out ImmutableDictionary<TKey,TValue> max)
		{
			if (IsEmpty) {
				max = Empty;
				return Empty;
			}
			if (GTDict.IsEmpty) {
				//We are the max:
				max = this;
				return LTDict;
			} else {
				//Go down:
				var newgt = GTDict.RemoveMax (out max);
				var newroot = new ImmutableDictionary<TKey,TValue> (Key, Value, LTDict, newgt);
				return newroot.FixRootBalance ();
			}
		}

		ImmutableDictionary<TKey,TValue> RemoveMin (out ImmutableDictionary<TKey,TValue> min)
		{
			if (IsEmpty) {
				min = Empty;
				return Empty;
			}
			if (LTDict.IsEmpty) {
				//We are the minimum:
				min = this;
				return GTDict;
			}
			//Go down:
			var newlt = LTDict.RemoveMin (out min);
			var newroot = new ImmutableDictionary<TKey,TValue> (Key, Value, newlt, GTDict);
			return newroot.FixRootBalance ();
		}

		/// <summary>
		/// Return a new dict with the root key-value pair removed
		/// </summary>
		ImmutableDictionary<TKey,TValue> RemoveRoot ()
		{
			if (IsEmpty) {
				return this;
			}
			if (LTDict.IsEmpty) {
				return GTDict;
			}
			if (GTDict.IsEmpty) {
				return LTDict;
			}
			//Neither are empty:
			if (LTDict._count < GTDict._count) {
				//LTDict has fewer, so promote from GTDict to minimize depth
				ImmutableDictionary<TKey,TValue> min;
				var newgt = GTDict.RemoveMin (out min);
				var newroot = new ImmutableDictionary<TKey,TValue> (min.Key, min.Value, LTDict, newgt);
				return newroot.FixRootBalance ();
			} else {
				ImmutableDictionary<TKey,TValue> max;
				var newlt = LTDict.RemoveMax (out max);
				var newroot = new ImmutableDictionary<TKey,TValue> (max.Key, max.Value, newlt, GTDict);
				return newroot.FixRootBalance ();
			}
		}

		/// <summary>
		/// Move the Root into the GTDict and promote LTDict node up
		/// If LTDict is empty, this operation returns this
		/// </summary>
		ImmutableDictionary<TKey,TValue> RotateToGT ()
		{
			if (LTDict.IsEmpty || IsEmpty) {
				return this;
			}
			var gLT = LTDict.LTDict;
			var gGT = LTDict.GTDict;
			var newgt = new ImmutableDictionary<TKey,TValue> (Key, Value, gGT, GTDict);
			return new ImmutableDictionary<TKey,TValue> (LTDict.Key, LTDict.Value, gLT, newgt);
		}

		/// <summary>
		/// Move the Root into the LTDict and promote GTDict node up
		/// If GTDict is empty, this operation returns this
		/// </summary>
		ImmutableDictionary<TKey,TValue> RotateToLT ()
		{
			if (GTDict.IsEmpty || IsEmpty) {
				return this;
			}
			var gLT = GTDict.LTDict;
			var gGT = GTDict.GTDict;
			var newlt = new ImmutableDictionary<TKey,TValue> (Key, Value, LTDict, gLT);
			return new ImmutableDictionary<TKey,TValue> (GTDict.Key, GTDict.Value, newlt, gGT);
		}

		/// <summary>
		/// Enumerate from largest to smallest key
		/// </summary>
		IEnumerator<KeyValuePair<TKey,TValue>> GetMaxToMinEnumerator ()
		{
			var to_visit = new Stack<ImmutableDictionary<TKey,TValue>> ();
			to_visit.Push (this);
			while (to_visit.Count > 0) {
				var this_d = to_visit.Pop ();
				if (this_d.IsEmpty) {
					continue;
				}
				if (this_d.GTDict.IsEmpty) {
					//This is the next biggest value in the Dict:
					yield return new KeyValuePair<TKey,TValue> (this_d.Key, this_d.Value);
					to_visit.Push (this_d.LTDict);
				} else {
					//Break it up
					to_visit.Push (this_d.LTDict);
					to_visit.Push (new ImmutableDictionary<TKey,TValue> (this_d.Key, this_d.Value));
					to_visit.Push (this_d.GTDict);
				}
			}
		}

		public override bool Equals (object o)
		{
			if (object.ReferenceEquals (this, o)) {
				return true;
			}
			var other = o as ImmutableDictionary<TKey, TValue>;
			if (other != null) {
				//Equivalent must have same count:
				if (other._count != _count) {
					return false;
				}
				//Now go element by element:
				bool all_equal = true;
				//Enumeration goes in a sorted order:
				var this_enum = GetEnumerator ();
				var o_enum = other.GetEnumerator ();
				while (all_equal) {
					this_enum.MoveNext ();
					//Since we have the same count, this must return same as above
					//Both are finished, but were equal to this point:
					if (!o_enum.MoveNext ()) {
						return true;
					}
					var tkv = this_enum.Current;
					var okv = o_enum.Current;
					all_equal = tkv.Key.Equals (okv.Key) &&
					//Handle case of null values:
						(null != tkv.Value ? tkv.Value.Equals (okv.Value)
						 : null == okv.Value);
				}
				return all_equal;
			}
			return false;

		}	

		public override int GetHashCode ()
		{
			var imd = Min;
			if (imd != Empty)
				return imd.Key.GetHashCode () ^ (imd.Value != null ? imd.Value.GetHashCode () : 0);
			return 0;
		}
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

