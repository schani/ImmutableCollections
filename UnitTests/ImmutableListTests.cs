//
// ImmutableListTests.cs
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
using NUnit.Framework;
using System.Collections.Immutable;
using System.Collections.Generic;

namespace UnitTests
{
	[TestFixture]
	public class ImmutableListTests
	{
		[Test]
		public void TestSimpleOperation ()
		{
			var list = ImmutableList.Create<int> ();
			list = list.Add (1);
			list = list.Add (2);
			list = list.Add (3);

			Assert.AreEqual (1, list[0]);
			Assert.AreEqual (2, list[1]);
			Assert.AreEqual (3, list[2]);
			Assert.AreEqual (3, list.Count);

			list = list.RemoveAt (1);
			Assert.AreEqual (2, list.Count);

			Assert.AreEqual (1, list[0]);
			Assert.AreEqual (3, list[1]);
		}

		[Test]
		public void TestInsert ()
		{
			var list = ImmutableList.Create<int> ();
			list = list.Add (1);
			list = list.Add (3);
			list = list.Insert (1, 2);

			Assert.AreEqual (1, list[0]);
			Assert.AreEqual (2, list[1]);
			Assert.AreEqual (3, list[2]);
		}

		[Test]
		public void TestInsertCollection ()
		{
			var list = ImmutableList.Create<int> ();
			list = list.Add (1);
			list = list.Add (3);
			list = list.InsertRange (1, new [] {4, 5, 6});

			Assert.AreEqual (1, list[0]);
			Assert.AreEqual (4, list[1]);
			Assert.AreEqual (5, list[2]);
			Assert.AreEqual (6, list[3]);
			Assert.AreEqual (3, list[4]);
		}


		[Test]
		public void TestAddRange()
		{
			var toAdd = new List<int>() { 1, 2, 3, 4 };
			var immutable1 = ImmutableList.Create<int> ();
			var immutable2 = immutable1.AddRange(toAdd);
			Assert.AreNotEqual(immutable1, immutable2);
			Assert.AreEqual(4, immutable2.Count);
			for(int i = 0; i < toAdd.Count; i++)
			{
				Assert.AreEqual(toAdd[i], immutable2[i]);
			}
		}


		[Test]
		public void TestAddRangeToListCreatedWithAddRange()
		{
			var toAdd = new List<int>() { 1, 2, 3, 4 };
			var immutable1 = ImmutableList.Create<int> ();
			var immutable2 = immutable1.AddRange(toAdd);
			Assert.AreNotEqual(immutable1, immutable2);
			Assert.AreEqual(4, immutable2.Count);
			for(int i = 0; i < toAdd.Count; i++)
			{
				Assert.AreEqual(toAdd[i], immutable2[i]);
			}
			var toAdd2 = new List<int> { 5, 6, 7 };
			var immutable3 = immutable2.AddRange(toAdd2);
			Assert.AreNotEqual(immutable2, immutable3);
			Assert.AreEqual(7, immutable3.Count);
			for(int i = 0; i < toAdd2.Count; i++)
			{
				Assert.AreEqual(toAdd2[i], immutable3[4+i]);
			}
		}

		[Test]
		public void TestFork()
		{
			var immutableBase = ImmutableList.Create<int>(1);

			var fork1 = immutableBase.Add(2);
			var fork2 = immutableBase.Add(3);

			Assert.AreEqual(immutableBase.Count, 1);
			Assert.AreEqual(immutableBase[0], 1);

			Assert.AreEqual(fork1.Count, 2);
			Assert.AreEqual(fork1[0], 1);
			Assert.AreEqual(fork1[1], 2);

			Assert.AreEqual(fork2.Count, 2);
			Assert.AreEqual(fork2[0], 1);
			Assert.AreEqual(fork2[1], 3);
		}
	}
}

