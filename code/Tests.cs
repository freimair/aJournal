using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace code
{
	[TestFixture()]
	public class ModelTests
	{
		List<Stroke> listA;
		List<Stroke> listB;

		[SetUp]
		protected void SetUp ()
		{
			// setup test data
			listA = new List<Stroke> ();
			Stroke stroke = new Stroke ();
			stroke.Points.AddRange (new int[] {0,0,100,0,100,100});
			listA.Add (stroke);
			stroke = new Stroke ();
			stroke.Points.AddRange (new int[] {0,0,100,100});
			listA.Add (stroke);

			listB = new List<Stroke> ();
			stroke = new Stroke ();
			stroke.Points.AddRange (new int[] {3,3,3,3,3,3});
			listB.Add (stroke);
			stroke = new Stroke ();
			stroke.Points.AddRange (new int[] {4,4,4,4,4,4});
			listB.Add (stroke);
		}

		[Test()]
		public void StrokesTest ()
		{
			// create DUT
			Entry DUT = Entry.create ();

			// do some edit tasks
			// - insert
			DUT.edit (new List<Stroke> (), listA);
			Assert.AreEqual (listA.ToString (), DUT.get ().ToString (), "adding stroke failed");
			// - change
			DUT.edit (listA, listB);
			Assert.AreEqual (listB.ToString (), DUT.get ().ToString (), "altering stroke failed");
			// - delete
			DUT.edit (listB, new List<Stroke> ());
			Assert.AreEqual (new List<Stroke> ().ToString (), DUT.get ().ToString (), "deleting stroke failed");
		}

		[Test]
		public void PersistenceTest ()
		{
			// create DUT
			Entry DUT = Entry.create ();

			// create stroke
			DUT.edit (new List<Stroke> (), listA);

			// save to disk
			DUT.persist ();

			// reload from disk
			DUT = Entry.getEntries () [0];

			// check
			Assert.AreEqual (listA.ToString (), DUT.get ().ToString (), "reloading stroke from disk failed");
		}
	}
}

