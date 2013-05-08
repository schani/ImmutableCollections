//
// IndexedAvlNode.cs
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
	class IndexedAvlNode<T>
	{
		public static readonly IndexedAvlNode<T> Empty = new NullNode ();

		sealed class NullNode : IndexedAvlNode<T>
		{
			public override bool IsEmpty {
				get {
					return true;
				}
			}
		}

		public readonly T Value;
		internal readonly IndexedAvlNode<T> left;
		internal readonly IndexedAvlNode<T> right;
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
				return left._depth - right._depth;
			}
		}

		int Depth {
			get {
				return _depth;
			}
		}

		public IndexedAvlNode ()
		{
			right = Empty;
			left = Empty;
		}

		public IndexedAvlNode (T val) : this(val, Empty, Empty)
		{
		}

		IndexedAvlNode (T val, IndexedAvlNode<T> lt, IndexedAvlNode<T> gt)
		{
			Value = val;
			left = lt;
			right = gt;
			_count = 1 + left._count + right._count;
			_depth = 1 + Math.Max (left._depth, right._depth);
		}

		class MaxToMinEnumerable<T1> : IEnumerable, IEnumerable<T1> where T1 : IComparable<T1>
		{
			readonly IndexedAvlNode<T1> _dict;

			public MaxToMinEnumerable (IndexedAvlNode<T1> dict)
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
		IndexedAvlNode<T> Min {
			get {
				if (IsEmpty)
					return Empty;
				var dict = this;
				var next = dict.left;
				while (next != Empty) {
					dict = next;
					next = dict.left;
				}
				return dict;
			}
		}


		/// <summary>
		/// Fix the root balance if LTDict and GTDict have good balance
		/// Used to keep the depth less than 1.44 log_2 N (AVL tree)
		/// </summary>
		IndexedAvlNode<T> FixRootBalance ()
		{
			int bal = Balance;
			if (Math.Abs (bal) < 2)
				return this;

			if (bal == 2) {
				if (left.Balance == 1 || left.Balance == 0) {
					//Easy case:
					return this.RotateToGT ();
				}
				if (left.Balance == -1) {
					//Rotate LTDict:
					var newlt = left.RotateToLT ();
					var newroot = new IndexedAvlNode<T> (Value, newlt, right);
					return newroot.RotateToGT ();
				}
				throw new Exception (String.Format ("LTDict too unbalanced: {0}", left.Balance));
			}
			if (bal == -2) {
				if (right.Balance == -1 || right.Balance == 0) {
					//Easy case:
					return this.RotateToLT ();
				}
				if (right.Balance == 1) {
					//Rotate GTDict:
					var newgt = right.RotateToGT ();
					var newroot = new IndexedAvlNode<T> (Value, left, newgt);
					return newroot.RotateToLT ();
				}
				throw new Exception (String.Format ("LTDict too unbalanced: {0}", left.Balance));
			}
			//In this case we can show: |bal| > 2
			//if( Math.Abs(bal) > 2 ) {
			throw new Exception (String.Format ("Tree too out of balance: {0}", Balance));
		}



		/// <summary>
		/// Return a new tree with the key-value pair inserted
		/// If the key is already present, it replaces the value
		/// This operation is O(Log N) where N is the number of keys
		/// </summary>
		public IndexedAvlNode<T> InsertIntoNew (int index, T val)
		{
			if (IsEmpty)
				return new IndexedAvlNode<T> (val);

			IndexedAvlNode<T> newlt = left;
			IndexedAvlNode<T> newgt = right;

			if (index <= left._count) {
				newlt = left.InsertIntoNew (index, val);
			} else {
				newgt = right.InsertIntoNew (index - left._count - 1, val);
			}

			var newroot = new IndexedAvlNode<T> (Value, newlt, newgt);
			return newroot.FixRootBalance ();
		}

		public IndexedAvlNode<T> SetItem (int index, T val)
		{
			IndexedAvlNode<T> newlt = left;
			IndexedAvlNode<T> newgt = right;

			if (index < left._count) {
				newlt = left.SetItem (index, val);
			} else if (index > left._count) {
				newgt = right.SetItem (index - left._count - 1, val);
			} else {
				return new IndexedAvlNode<T> (val, newlt, newgt);
			}
			return new IndexedAvlNode<T> (Value, newlt, newgt);
		}

		public IndexedAvlNode<T> GetNodeAt (int index)
		{
			if (index < left._count) 
				return left.GetNodeAt (index);
			if (index > left._count) 
				return right.GetNodeAt (index - left._count - 1);
			return this;
		}

		/// <summary>
		/// Merge two Dictionaries into one.
		/// </summary>
		IndexedAvlNode<T> Merge (IndexedAvlNode<T> one, IndexedAvlNode<T> two, Comparison<T> comparer)
		{
			if (two._count > one._count) {
				//Swap them so the sub-merge is on the smaller:
				var temp = two;
				two = one;
				one = temp;
			}
			IndexedAvlNode<T> min;
			/*
			 * A nice recursive algorithm is just return Merge,
			 * rather than loop, but I'm afraid O(N) recursions
			 * will cause .Net to explode EVEN THOUGH IT IS TAIL
			 * RECURSION!  (they should use tailcall).
			 */
			while (two._count > 0) {
				two = two.RemoveMin (out min);
				one = one.InsertIntoNew (0, min.Value);
			}
			return one;
		}

		/// <summary>
		/// Try to remove the key, and return the resulting Dict
		/// if the key is not found, old_node is Empty, else old_node is the Dict
		/// with matching Key
		/// </summary>
		public IndexedAvlNode<T> RemoveFromNew (int index, out IndexedAvlNode<T> old_node)
		{
			if (IsEmpty) {
				old_node = Empty;
				return Empty;
			}

			if (index < left._count) {
				var newlt = left.RemoveFromNew (index, out old_node);
				if (old_node.IsEmpty) {
					//Not found, so nothing changed
					return this;
				}
				var newroot = new IndexedAvlNode<T> (Value, newlt, right);
				return newroot.FixRootBalance ();
			}

			if (index > left._count) {
				var newgt = right.RemoveFromNew (index - left._count - 1, out old_node);
				if (old_node.IsEmpty) {
					//Not found, so nothing changed
					return this;
				}
				var newroot = new IndexedAvlNode<T> (Value, left, newgt);
				return newroot.FixRootBalance ();
			}

			//found it
			old_node = this;
			return RemoveRoot ();
		}

		IndexedAvlNode<T> RemoveMax (out IndexedAvlNode<T> max)
		{
			if (IsEmpty) {
				max = Empty;
				return Empty;
			}
			if (right.IsEmpty) {
				//We are the max:
				max = this;
				return left;
			} else {
				//Go down:
				var newgt = right.RemoveMax (out max);
				var newroot = new IndexedAvlNode<T> (Value, left, newgt);
				return newroot.FixRootBalance ();
			}
		}

		IndexedAvlNode<T> RemoveMin (out IndexedAvlNode<T> min)
		{
			if (IsEmpty) {
				min = Empty;
				return Empty;
			}
			if (left.IsEmpty) {
				//We are the minimum:
				min = this;
				return right;
			}
			//Go down:
			var newlt = left.RemoveMin (out min);
			var newroot = new IndexedAvlNode<T> (Value, newlt, right);
			return newroot.FixRootBalance ();
		}

		/// <summary>
		/// Return a new dict with the root key-value pair removed
		/// </summary>
		IndexedAvlNode<T> RemoveRoot ()
		{
			if (IsEmpty) {
				return this;
			}
			if (left.IsEmpty) {
				return right;
			}
			if (right.IsEmpty) {
				return left;
			}
			//Neither are empty:
			if (left._count < right._count) {
				//LTDict has fewer, so promote from GTDict to minimize depth
				IndexedAvlNode<T> min;
				var newgt = right.RemoveMin (out min);
				var newroot = new IndexedAvlNode<T> (min.Value, left, newgt);
				return newroot.FixRootBalance ();
			} else {
				IndexedAvlNode<T> max;
				var newlt = left.RemoveMax (out max);
				var newroot = new IndexedAvlNode<T> (max.Value, newlt, right);
				return newroot.FixRootBalance ();
			}
		}

		/// <summary>
		/// Move the Root into the GTDict and promote LTDict node up
		/// If LTDict is empty, this operation returns this
		/// </summary>
		IndexedAvlNode<T> RotateToGT ()
		{
			if (left.IsEmpty || IsEmpty) {
				return this;
			}
			var gLT = left.left;
			var gGT = left.right;
			var newgt = new IndexedAvlNode<T> (Value, gGT, right);
			return new IndexedAvlNode<T> (left.Value, gLT, newgt);
		}

		/// <summary>
		/// Move the Root into the LTDict and promote GTDict node up
		/// If GTDict is empty, this operation returns this
		/// </summary>
		IndexedAvlNode<T> RotateToLT ()
		{
			if (right.IsEmpty || IsEmpty) {
				return this;
			}
			var gLT = right.left;
			var gGT = right.right;
			var newlt = new IndexedAvlNode<T> (Value, left, gLT);
			return new IndexedAvlNode<T> (right.Value, newlt, gGT);
		}

		/// <summary>
		/// Enumerate from largest to smallest key
		/// </summary>
		public IEnumerator<T> GetMaxToMinEnumerator ()
		{
			var to_visit = new Stack<IndexedAvlNode<T>> ();
			to_visit.Push (this);
			while (to_visit.Count > 0) {
				var this_d = to_visit.Pop ();
				if (this_d.IsEmpty) {
					continue;
				}
				if (this_d.right.IsEmpty) {
					//This is the next biggest value in the Dict:
					yield return this_d.Value;
					to_visit.Push (this_d.left);
				} else {
					//Break it up
					to_visit.Push (this_d.left);
					to_visit.Push (new IndexedAvlNode<T> (this_d.Value));
					to_visit.Push (this_d.right);
				}
			}
		}
	}
}

