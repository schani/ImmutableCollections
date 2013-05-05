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

namespace UnitTests
{
	[TestFixture]
	public class ImmutableListTests
	{
		[Test]
		public void TestSimpleOperation ()
		{
			IImmutableList<int> list = new ImmutableList<int> ();
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
			IImmutableList<int> list = new ImmutableList<int> ();
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
			IImmutableList<int> list = new ImmutableList<int> ();
			list = list.Add (1);
			list = list.Add (3);
			list = list.InsertRange (1, new [] {4, 5, 6});

			Assert.AreEqual (1, list[0]);
			Assert.AreEqual (4, list[1]);
			Assert.AreEqual (5, list[2]);
			Assert.AreEqual (6, list[3]);
			Assert.AreEqual (3, list[4]);
		}
	}
}

