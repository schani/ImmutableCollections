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

		sealed class NullNode : AvlNode<T>
		{
			public override bool IsEmpty {
				get {
					return true;
				}
			}
		}

		public readonly T Value;
		internal readonly AvlNode<T> left;
		internal readonly AvlNode<T> right;
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

		public AvlNode ()
		{
			right = Empty;
			left = Empty;
		}

		public AvlNode (T val) : this(val, Empty, Empty)
		{
		}

		AvlNode (T val, AvlNode<T> lt, AvlNode<T> gt)
		{
			Value = val;
			left = lt;
			right = gt;
			_count = 1 + left._count + right._count;
			_depth = 1 + Math.Max (left._depth, right._depth);
		}

		/// <summary>
		/// Return the subtree with the min value at the root, or Empty if Empty
		/// </summary>
		AvlNode<T> Min {
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
		AvlNode<T> FixRootBalance ()
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
					var newroot = new AvlNode<T> (Value, newlt, right);
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
					var newroot = new AvlNode<T> (Value, left, newgt);
					return newroot.RotateToLT ();
				}
				throw new Exception (String.Format ("LTDict too unbalanced: {0}", left.Balance));
			}
			//In this case we can show: |bal| > 2
			//if( Math.Abs(bal) > 2 ) {
			throw new Exception (String.Format ("Tree too out of balance: {0}", Balance));
		}

		public AvlNode<T> SearchNode (T value, Comparison<T> comparer)
		{
			var dict = this;
			while (dict != Empty) {
				int comp = comparer (dict.Value, value);
				if (comp < 0) {
					dict = dict.right;
				} else if (comp > 0) {
					dict = dict.left;
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
		public AvlNode<T> InsertIntoNew (int index, T val)
		{
			if (IsEmpty)
				return new AvlNode<T> (val);

			AvlNode<T> newlt = left;
			AvlNode<T> newgt = right;

			if (index <= left._count) {
				newlt = left.InsertIntoNew (index, val);
			} else {
				newgt = right.InsertIntoNew (index - left._count - 1, val);
			}

			var newroot = new AvlNode<T> (Value, newlt, newgt);
			return newroot.FixRootBalance ();
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
			
			AvlNode<T> newlt = left;
			AvlNode<T> newgt = right;
			
			int comp = comparer (Value, val);
			T newv = Value;
			if (comp < 0) {
				//Let the GTDict put it in:
				newgt = right.InsertIntoNew (val, comparer);
			} else if (comp > 0) {
				//Let the LTDict put it in:
				newlt = left.InsertIntoNew (val, comparer);
			} else {
				//Replace the current value:
				newv = val;
			}
			var newroot = new AvlNode<T> (newv, newlt, newgt);
			return newroot.FixRootBalance ();
		}

		public AvlNode<T> SetItem (int index, T val)
		{
			AvlNode<T> newlt = left;
			AvlNode<T> newgt = right;

			if (index < left._count) {
				newlt = left.SetItem (index, val);
			} else if (index > left._count) {
				newgt = right.SetItem (index - left._count - 1, val);
			} else {
				return new AvlNode<T> (val, newlt, newgt);
			}
			return new AvlNode<T> (Value, newlt, newgt);
		}

		public AvlNode<T> GetNodeAt (int index)
		{
			if (index < left._count) 
				return left.GetNodeAt (index);
			if (index > left._count) 
				return right.GetNodeAt (index - left._count - 1);
			return this;
		}

		/// <summary>
		/// Try to remove the key, and return the resulting Dict
		/// if the key is not found, old_node is Empty, else old_node is the Dict
		/// with matching Key
		/// </summary>
		public AvlNode<T> RemoveFromNew (int index, out AvlNode<T> old_node)
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
				var newroot = new AvlNode<T> (Value, newlt, right);
				return newroot.FixRootBalance ();
			}

			if (index > left._count) {
				var newgt = right.RemoveFromNew (index - left._count - 1, out old_node);
				if (old_node.IsEmpty) {
					//Not found, so nothing changed
					return this;
				}
				var newroot = new AvlNode<T> (Value, left, newgt);
				return newroot.FixRootBalance ();
			}

			//found it
			old_node = this;
			return RemoveRoot ();
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
				var newgt = right.RemoveFromNew (val, comparer, out old_node);
				if (old_node.IsEmpty) {
					//Not found, so nothing changed
					return this;
				}
				var newroot = new AvlNode<T> (Value, left, newgt);
				return newroot.FixRootBalance ();
			}
			if (comp > 0) {
				var newlt = left.RemoveFromNew (val, comparer, out old_node);
				if (old_node.IsEmpty) {
					//Not found, so nothing changed
					return this;
				}
				var newroot = new AvlNode<T> (Value, newlt, right);
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
			if (right.IsEmpty) {
				//We are the max:
				max = this;
				return left;
			} else {
				//Go down:
				var newgt = right.RemoveMax (out max);
				var newroot = new AvlNode<T> (Value, left, newgt);
				return newroot.FixRootBalance ();
			}
		}

		AvlNode<T> RemoveMin (out AvlNode<T> min)
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
			var newroot = new AvlNode<T> (Value, newlt, right);
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
			if (left.IsEmpty) {
				return right;
			}
			if (right.IsEmpty) {
				return left;
			}
			//Neither are empty:
			if (left._count < right._count) {
				//LTDict has fewer, so promote from GTDict to minimize depth
				AvlNode<T> min;
				var newgt = right.RemoveMin (out min);
				var newroot = new AvlNode<T> (min.Value, left, newgt);
				return newroot.FixRootBalance ();
			} else {
				AvlNode<T> max;
				var newlt = left.RemoveMax (out max);
				var newroot = new AvlNode<T> (max.Value, newlt, right);
				return newroot.FixRootBalance ();
			}
		}

		/// <summary>
		/// Move the Root into the GTDict and promote LTDict node up
		/// If LTDict is empty, this operation returns this
		/// </summary>
		AvlNode<T> RotateToGT ()
		{
			if (left.IsEmpty || IsEmpty) {
				return this;
			}
			var gLT = left.left;
			var gGT = left.right;
			var newgt = new AvlNode<T> (Value, gGT, right);
			return new AvlNode<T> (left.Value, gLT, newgt);
		}

		/// <summary>
		/// Move the Root into the LTDict and promote GTDict node up
		/// If GTDict is empty, this operation returns this
		/// </summary>
		AvlNode<T> RotateToLT ()
		{
			if (right.IsEmpty || IsEmpty) {
				return this;
			}
			var gLT = right.left;
			var gGT = right.right;
			var newlt = new AvlNode<T> (Value, left, gLT);
			return new AvlNode<T> (right.Value, newlt, gGT);
		}

		/// <summary>
		/// Enumerate from largest to smallest key
		/// </summary>
		public IEnumerator<T> GetEnumerator (bool reverse)
		{
			var to_visit = new Stack<AvlNode<T>> ();
			to_visit.Push (this);
			while (to_visit.Count > 0) {
				var this_d = to_visit.Pop ();
			continue_without_pop:
				if (this_d.IsEmpty) {
					continue;
				}
				if (reverse) {
					if (this_d.right.IsEmpty) {
						//This is the next biggest value in the Dict:
						yield return this_d.Value;
						this_d = this_d.left;
						goto continue_without_pop;
					} else {
						//Break it up
						to_visit.Push (this_d.left);
						to_visit.Push (new AvlNode<T> (this_d.Value));
						this_d = this_d.right;
						goto continue_without_pop;
					}
				} else {
					if (this_d.left.IsEmpty) {
						//This is the next biggest value in the Dict:
						yield return this_d.Value;
						this_d = this_d.right;
						goto continue_without_pop;
					} else {
						//Break it up
						if (!this_d.right.IsEmpty)
							to_visit.Push (this_d.right);
						to_visit.Push (new AvlNode<T> (this_d.Value));
						this_d = this_d.left;
						goto continue_without_pop;
					}
				}
			}
		}
	}
}
