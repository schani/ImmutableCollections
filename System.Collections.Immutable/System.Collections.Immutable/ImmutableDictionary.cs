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
	public class ImmutableDictionary<TKey, TVal> : IImmutableDictionary<TKey, TVal> where TKey : System.IComparable<TKey>
	{
		public static readonly ImmutableDictionary<TKey, TVal> Empty = new ImmutableDictionary<TKey, TVal> ();
		public readonly TKey Key;
		public readonly TVal Value;
		public readonly ImmutableDictionary<TKey,TVal> LTDict;
		public readonly ImmutableDictionary<TKey,TVal> GTDict;
		readonly int _count;
		readonly int _depth;

		public ImmutableDictionary ()
		{
			Key = default(TKey);
			Value = default(TVal);
			LTDict = null;
			GTDict = null; 
			_count = 0;
			_depth = 0;
		}

		ImmutableDictionary (TKey key, TVal val) : this(key, val, Empty, Empty)
		{
		}

		ImmutableDictionary (TKey key, TVal val, ImmutableDictionary<TKey,TVal> lt, ImmutableDictionary<TKey,TVal> gt)
		{
			Key = key;
			Value = val;
			LTDict = lt;
			GTDict = gt;
			_count = 1 + LTDict._count + GTDict._count;
			_depth = 1 + Math.Max (LTDict._depth, GTDict._depth);
		}

		ImmutableDictionary (List<KeyValuePair<TKey,TVal>> kvs, int start,
		                    int upbound, bool sort)
		{
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
			LTDict = (mid > start) ? new ImmutableDictionary<TKey,TVal> (kvs, start, mid, false) : Empty;
			GTDict = (upbound > (mid + 1)) ?
				new ImmutableDictionary<TKey,TVal> (kvs, mid + 1, upbound, false)
					: Empty;
			_count = count;
			_depth = 1 + Math.Max (LTDict._depth, GTDict._depth);
		}

		public ImmutableDictionary (IList<KeyValuePair<TKey,TVal>> kvs) : this(new List<KeyValuePair<TKey,TVal>>(kvs), 0, kvs.Count, true)
		{
		}

		protected class MaxToMinEnumerable<K1,V1> : IEnumerable, IEnumerable<KeyValuePair<K1,V1>> where K1 : IComparable<K1>
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

		public IImmutableDictionary<TKey, TVal> Add (TKey key, TVal value)
		{
			return InsertIntoNew (key, value);
		}

		public IImmutableDictionary<TKey, TVal> AddRange (IEnumerable<KeyValuePair<TKey, TVal>> pairs)
		{
			IImmutableDictionary<TKey, TVal> result = this;
			foreach (var kv in pairs)
				result = result.Add (kv.Key, kv.Value);
			return result;
		}

		public IImmutableDictionary<TKey, TVal> Clear ()
		{
			return Empty;
		}

		public bool Contains (KeyValuePair<TKey, TVal> kv)
		{
			var node = GetKey (kv.Key);
			return (node != Empty) && object.Equals (node.Value, kv.Value);
		}

		public IImmutableDictionary<TKey, TVal> Remove (TKey key)
		{
			var old = this;
			return RemoveFromNew (key, out old);
		}

		public IImmutableDictionary<TKey, TVal> RemoveRange (IEnumerable<TKey> keys)
		{
			IImmutableDictionary<TKey, TVal> result = this;
			foreach (var key in keys) {
				result = result.Remove (key);
			}
			return result;
		}

		public IImmutableDictionary<TKey, TVal> SetItem (TKey key, TVal value)
		{
			IImmutableDictionary<TKey, TVal> result = this;
			if (result.ContainsKey (key))
				result = result.Remove (key);
			result.Add (key, value);
			return result;
		}

		public IImmutableDictionary<TKey, TVal> SetItems (IEnumerable<KeyValuePair<TKey, TVal>> items)
		{
			IImmutableDictionary<TKey, TVal> result = this;
			foreach (var kv in items) {
				result = result.SetItem (kv.Key, kv.Value);
			}
			return result;
		}

		public IEqualityComparer<TVal> ValueComparer {
			get {
				throw new NotImplementedException ();
			}
		}
		#endregion

		#region IReadOnlyDictionary implementation

		public bool ContainsKey (TKey key)
		{
			return GetKey (key) != Empty;
		}

		public bool TryGetValue (TKey key, out TVal value)
		{
			var node = GetKey (key);
			if (node.IsEmpty) {
				value = default (TVal);
				return false;
			}
			value = node.Value;
			return true;
		}

		public TVal this [TKey key] {
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

		public IEnumerable<TVal> Values {
			get {
				foreach (var kv in this) {
					yield return kv.Value;
				}
			}
		}
		#endregion

		#region IEnumerable implementation

		public IEnumerator<KeyValuePair<TKey, TVal>> GetEnumerator ()
		{
			var to_visit = new Stack<ImmutableDictionary<TKey,TVal>> ();
			to_visit.Push (this);
			while (to_visit.Count > 0) {
				var this_d = to_visit.Pop ();
				if (this_d.IsEmpty) {
					continue;
				}
				if (this_d.LTDict.IsEmpty) {
					//This is the next smallest value in the Dict:
					yield return new KeyValuePair<TKey,TVal> (this_d.Key, this_d.Value);
					to_visit.Push (this_d.GTDict);
				} else {
					//Break it up
					to_visit.Push (this_d.GTDict);
					to_visit.Push (new ImmutableDictionary<TKey,TVal> (this_d.Key, this_d.Value));
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

		protected int Balance {
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
		ImmutableDictionary<TKey,TVal> Min {
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

		int CompareKV (KeyValuePair<TKey,TVal> kv0, KeyValuePair<TKey,TVal> kv1)
		{
			return kv0.Key.CompareTo (kv1.Key);
		}

		/// <summary>
		/// Fix the root balance if LTDict and GTDict have good balance
		/// Used to keep the depth less than 1.44 log_2 N (AVL tree)
		/// </summary>
		ImmutableDictionary<TKey,TVal> FixRootBalance ()
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
					var newroot = new ImmutableDictionary<TKey,TVal> (Key, Value, newlt, GTDict);
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
					var newroot = new ImmutableDictionary<TKey,TVal> (Key, Value, LTDict, newgt);
					return newroot.RotateToLT ();
				}
				throw new Exception (String.Format ("LTDict too unbalanced: {0}", LTDict.Balance));
			}
			//In this case we can show: |bal| > 2
			//if( Math.Abs(bal) > 2 ) {
			throw new Exception (String.Format ("Tree too out of balance: {0}", Balance));
		}

		ImmutableDictionary<TKey,TVal> GetKey (TKey key)
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
		ImmutableDictionary<TKey,TVal> InsertIntoNew (TKey key, TVal val)
		{
			if (IsEmpty)
				return new ImmutableDictionary<TKey,TVal> (key, val);
			TKey newk = Key;
			TVal newv = Value;
			ImmutableDictionary<TKey,TVal> newlt = LTDict;
			ImmutableDictionary<TKey,TVal> newgt = GTDict;

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
			var newroot = new ImmutableDictionary<TKey,TVal> (newk, newv, newlt, newgt);
			return newroot.FixRootBalance ();
		}

		/// <summary>
		/// Merge two Dictionaries into one.
		/// </summary>
		ImmutableDictionary<TKey,TVal> Merge (ImmutableDictionary<TKey,TVal> one,
		                               ImmutableDictionary<TKey,TVal> two)
		{
			if (two._count > one._count) {
				//Swap them so the sub-merge is on the smaller:
				var temp = two;
				two = one;
				one = temp;
			}
			ImmutableDictionary<TKey,TVal> min;
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
		ImmutableDictionary<TKey,TVal> RemoveFromNew (TKey key, out ImmutableDictionary<TKey,TVal> old_node)
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
				var newroot = new ImmutableDictionary<TKey,TVal> (Key, Value, LTDict, newgt);
				return newroot.FixRootBalance ();
			}
			if (comp > 0) {
				var newlt = LTDict.RemoveFromNew (key, out old_node);
				if (old_node.IsEmpty) {
					//Not found, so nothing changed
					return this;
				}
				var newroot = new ImmutableDictionary<TKey,TVal> (Key, Value, newlt, GTDict);
				return newroot.FixRootBalance ();
			}
			//found it
			old_node = this;
			return RemoveRoot ();
		}

		ImmutableDictionary<TKey,TVal> RemoveMax (out ImmutableDictionary<TKey,TVal> max)
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
				var newroot = new ImmutableDictionary<TKey,TVal> (Key, Value, LTDict, newgt);
				return newroot.FixRootBalance ();
			}
		}

		ImmutableDictionary<TKey,TVal> RemoveMin (out ImmutableDictionary<TKey,TVal> min)
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
			var newroot = new ImmutableDictionary<TKey,TVal> (Key, Value, newlt, GTDict);
			return newroot.FixRootBalance ();
		}

		/// <summary>
		/// Return a new dict with the root key-value pair removed
		/// </summary>
		ImmutableDictionary<TKey,TVal> RemoveRoot ()
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
				ImmutableDictionary<TKey,TVal> min;
				var newgt = GTDict.RemoveMin (out min);
				var newroot = new ImmutableDictionary<TKey,TVal> (min.Key, min.Value, LTDict, newgt);
				return newroot.FixRootBalance ();
			} else {
				ImmutableDictionary<TKey,TVal> max;
				var newlt = LTDict.RemoveMax (out max);
				var newroot = new ImmutableDictionary<TKey,TVal> (max.Key, max.Value, newlt, GTDict);
				return newroot.FixRootBalance ();
			}
		}

		/// <summary>
		/// Move the Root into the GTDict and promote LTDict node up
		/// If LTDict is empty, this operation returns this
		/// </summary>
		ImmutableDictionary<TKey,TVal> RotateToGT ()
		{
			if (LTDict.IsEmpty || IsEmpty) {
				return this;
			}
			var gLT = LTDict.LTDict;
			var gGT = LTDict.GTDict;
			var newgt = new ImmutableDictionary<TKey,TVal> (Key, Value, gGT, GTDict);
			return new ImmutableDictionary<TKey,TVal> (LTDict.Key, LTDict.Value, gLT, newgt);
		}

		/// <summary>
		/// Move the Root into the LTDict and promote GTDict node up
		/// If GTDict is empty, this operation returns this
		/// </summary>
		ImmutableDictionary<TKey,TVal> RotateToLT ()
		{
			if (GTDict.IsEmpty || IsEmpty) {
				return this;
			}
			var gLT = GTDict.LTDict;
			var gGT = GTDict.GTDict;
			var newlt = new ImmutableDictionary<TKey,TVal> (Key, Value, LTDict, gLT);
			return new ImmutableDictionary<TKey,TVal> (GTDict.Key, GTDict.Value, newlt, gGT);
		}

		/// <summary>
		/// Enumerate from largest to smallest key
		/// </summary>
		IEnumerator<KeyValuePair<TKey,TVal>> GetMaxToMinEnumerator ()
		{
			var to_visit = new Stack<ImmutableDictionary<TKey,TVal>> ();
			to_visit.Push (this);
			while (to_visit.Count > 0) {
				var this_d = to_visit.Pop ();
				if (this_d.IsEmpty) {
					continue;
				}
				if (this_d.GTDict.IsEmpty) {
					//This is the next biggest value in the Dict:
					yield return new KeyValuePair<TKey,TVal> (this_d.Key, this_d.Value);
					to_visit.Push (this_d.LTDict);
				} else {
					//Break it up
					to_visit.Push (this_d.LTDict);
					to_visit.Push (new ImmutableDictionary<TKey,TVal> (this_d.Key, this_d.Value));
					to_visit.Push (this_d.GTDict);
				}
			}
		}

		public override bool Equals (object o)
		{
			if (object.ReferenceEquals (this, o)) {
				return true;
			}
			var other = o as ImmutableDictionary<TKey, TVal>;
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
}

