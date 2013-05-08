//
// AvlNode.cs
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
	class AvlNode<T>
	{
		public static readonly AvlNode<T> Empty = new NullNode ();

		class NullNode : AvlNode<T>
		{
			public override bool IsEmpty {
				get {
					return true;
				}
			}
		}

		public readonly T Value;
		internal readonly AvlNode<T> LTDict;
		internal readonly AvlNode<T> GTDict;
		readonly int _count;
		readonly int _depth;

		public virtual bool IsEmpty { get { return false; } }

		public int Count {
			get {
				return _count;
			}
		}

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

		public AvlNode ()
		{
		}

		public AvlNode (T val) : this(val, Empty, Empty)
		{
		}

		AvlNode (T val, AvlNode<T> lt, AvlNode<T> gt)
		{
			Value = val;
			LTDict = lt;
			GTDict = gt;
			_count = 1 + LTDict._count + GTDict._count;
			_depth = 1 + Math.Max (LTDict._depth, GTDict._depth);
		}

		class MaxToMinEnumerable<T1> : IEnumerable, IEnumerable<T1> where T1 : IComparable<T1>
		{
			readonly AvlNode<T1> _dict;

			public MaxToMinEnumerable (AvlNode<T1> dict)
			{
				_dict = dict;
			}

			public IEnumerator<T1> GetEnumerator ()
			{
				return _dict.GetMaxToMinEnumerator ();
			}

			IEnumerator IEnumerable.GetEnumerator ()
			{
				return _dict.GetMaxToMinEnumerator ();
			}
		}

		/// <summary>
		/// Return the subtree with the min value at the root, or Empty if Empty
		/// </summary>
		AvlNode<T> Min {
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


		/// <summary>
		/// Fix the root balance if LTDict and GTDict have good balance
		/// Used to keep the depth less than 1.44 log_2 N (AVL tree)
		/// </summary>
		AvlNode<T> FixRootBalance ()
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
					var newroot = new AvlNode<T> (Value, newlt, GTDict);
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
					var newroot = new AvlNode<T> (Value, LTDict, newgt);
					return newroot.RotateToLT ();
				}
				throw new Exception (String.Format ("LTDict too unbalanced: {0}", LTDict.Balance));
			}
			//In this case we can show: |bal| > 2
			//if( Math.Abs(bal) > 2 ) {
			throw new Exception (String.Format ("Tree too out of balance: {0}", Balance));
		}

		public AvlNode<T> GetKey (T value, Comparison<T> comparer)
		{
			var dict = this;
			while (dict != Empty) {
				int comp = comparer (dict.Value, value);
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
		public AvlNode<T> InsertIntoNew (T val, Comparison<T> comparer)
		{
			if (IsEmpty)
				return new AvlNode<T> (val);

			AvlNode<T> newlt = LTDict;
			AvlNode<T> newgt = GTDict;

			int comp = comparer (Value, val);
			T newv = Value;
			if (comp < 0) {
				//Let the GTDict put it in:
				newgt = GTDict.InsertIntoNew (val, comparer);
			} else if (comp > 0) {
				//Let the LTDict put it in:
				newlt = LTDict.InsertIntoNew (val, comparer);
			} else {
				//Replace the current value:
				newv = val;
			}
			var newroot = new AvlNode<T> (newv, newlt, newgt);
			return newroot.FixRootBalance ();
		}

		/// <summary>
		/// Merge two Dictionaries into one.
		/// </summary>
		AvlNode<T> Merge (AvlNode<T> one, AvlNode<T> two, Comparison<T> comparer)
		{
			if (two._count > one._count) {
				//Swap them so the sub-merge is on the smaller:
				var temp = two;
				two = one;
				one = temp;
			}
			AvlNode<T> min;
			/*
			 * A nice recursive algorithm is just return Merge,
			 * rather than loop, but I'm afraid O(N) recursions
			 * will cause .Net to explode EVEN THOUGH IT IS TAIL
			 * RECURSION!  (they should use tailcall).
			 */
			while (two._count > 0) {
				two = two.RemoveMin (out min);
				one = one.InsertIntoNew (min.Value, comparer);
			}
			return one;
		}

		/// <summary>
		/// Try to remove the key, and return the resulting Dict
		/// if the key is not found, old_node is Empty, else old_node is the Dict
		/// with matching Key
		/// </summary>
		public AvlNode<T> RemoveFromNew (T val, Comparison<T> comparer, out AvlNode<T> old_node)
		{
			if (IsEmpty) {
				old_node = Empty;
				return Empty;
			}
			int comp = comparer (Value, val);
			if (comp < 0) {
				var newgt = GTDict.RemoveFromNew (val, comparer, out old_node);
				if (old_node.IsEmpty) {
					//Not found, so nothing changed
					return this;
				}
				var newroot = new AvlNode<T> (Value, LTDict, newgt);
				return newroot.FixRootBalance ();
			}
			if (comp > 0) {
				var newlt = LTDict.RemoveFromNew (val, comparer, out old_node);
				if (old_node.IsEmpty) {
					//Not found, so nothing changed
					return this;
				}
				var newroot = new AvlNode<T> (Value, newlt, GTDict);
				return newroot.FixRootBalance ();
			}
			//found it
			old_node = this;
			return RemoveRoot ();
		}

		AvlNode<T> RemoveMax (out AvlNode<T> max)
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
				var newroot = new AvlNode<T> (Value, LTDict, newgt);
				return newroot.FixRootBalance ();
			}
		}

		AvlNode<T> RemoveMin (out AvlNode<T> min)
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
			var newroot = new AvlNode<T> (Value, newlt, GTDict);
			return newroot.FixRootBalance ();
		}

		/// <summary>
		/// Return a new dict with the root key-value pair removed
		/// </summary>
		AvlNode<T> RemoveRoot ()
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
				AvlNode<T> min;
				var newgt = GTDict.RemoveMin (out min);
				var newroot = new AvlNode<T> (min.Value, LTDict, newgt);
				return newroot.FixRootBalance ();
			} else {
				AvlNode<T> max;
				var newlt = LTDict.RemoveMax (out max);
				var newroot = new AvlNode<T> (max.Value, newlt, GTDict);
				return newroot.FixRootBalance ();
			}
		}

		/// <summary>
		/// Move the Root into the GTDict and promote LTDict node up
		/// If LTDict is empty, this operation returns this
		/// </summary>
		AvlNode<T> RotateToGT ()
		{
			if (LTDict.IsEmpty || IsEmpty) {
				return this;
			}
			var gLT = LTDict.LTDict;
			var gGT = LTDict.GTDict;
			var newgt = new AvlNode<T> (Value, gGT, GTDict);
			return new AvlNode<T> (LTDict.Value, gLT, newgt);
		}

		/// <summary>
		/// Move the Root into the LTDict and promote GTDict node up
		/// If GTDict is empty, this operation returns this
		/// </summary>
		AvlNode<T> RotateToLT ()
		{
			if (GTDict.IsEmpty || IsEmpty) {
				return this;
			}
			var gLT = GTDict.LTDict;
			var gGT = GTDict.GTDict;
			var newlt = new AvlNode<T> (Value, LTDict, gLT);
			return new AvlNode<T> (GTDict.Value, newlt, gGT);
		}

		/// <summary>
		/// Enumerate from largest to smallest key
		/// </summary>
		public IEnumerator<T> GetMaxToMinEnumerator ()
		{
			var to_visit = new Stack<AvlNode<T>> ();
			to_visit.Push (this);
			while (to_visit.Count > 0) {
				var this_d = to_visit.Pop ();
				if (this_d.IsEmpty) {
					continue;
				}
				if (this_d.GTDict.IsEmpty) {
					//This is the next biggest value in the Dict:
					yield return this_d.Value;
					to_visit.Push (this_d.LTDict);
				} else {
					//Break it up
					to_visit.Push (this_d.LTDict);
					to_visit.Push (new AvlNode<T> (this_d.Value));
					to_visit.Push (this_d.GTDict);
				}
			}
		}
	}
}

