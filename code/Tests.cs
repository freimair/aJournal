using System;
using System.Xml;
using System.Collections.Generic;
using NUnit.Framework;
using code;

namespace test
{
	[TestFixture()]
	public class ModelTests
	{
		List<Drawable> listA;
		List<Drawable> listB;

		[SetUp]
		protected void SetUp ()
		{
			// setup test data
			listA = new List<Drawable> ();
			Stroke stroke = new Stroke ();
			stroke.Points.AddRange (new int[] {0,0,100,0,100,100});
			listA.Add (stroke);
			stroke = new Stroke ();
			stroke.Points.AddRange (new int[] {0,0,100,100});
			listA.Add (stroke);

			listB = new List<Drawable> ();
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
			DUT.edit (new List<Drawable> (), listA);
			Assert.AreEqual (listA.ToString (), DUT.get ().ToString (), "adding stroke failed");
			// - change
			DUT.edit (listA, listB);
			Assert.AreEqual (listB.ToString (), DUT.get ().ToString (), "altering stroke failed");
			// - delete
			DUT.edit (listB, new List<Drawable> ());
			Assert.AreEqual (new List<Drawable> ().ToString (), DUT.get ().ToString (), "deleting stroke failed");
		}

		[Test]
		public void PersistenceTest ()
		{
			// create DUT
			Entry DUT = Entry.create ();

			// create stroke
			DUT.edit (new List<Drawable> (), listA);

			// save to disk
			DUT.persist ();

			// reload from disk
			DUT = Entry.getEntries () [0];

			// check
			Assert.AreEqual (listA.ToString (), DUT.get ().ToString (), "reloading stroke from disk failed");

			// cleanup
			DUT.Delete ();
		}

		[Test]
		public void SimpleTagTest ()
		{
			Tag DUT = new Tag ("");
			DUT.Name = "tag1";
			Assert.AreEqual (DUT.Name, DUT.ToString ());
		}

		[Test]
		public void TagTreeTest ()
		{
			// setup testing entities
			string tag1name = "tag1", tag2name = "tag2", tag3name = "tag3";
			Tag tag1 = new Tag (tag1name);
			Tag tag2 = new Tag (tag2name);
			Tag tag3 = new Tag (tag3name);

			// assemble tree
			tag3.Parent = tag2;
			tag2.Parent = tag1;

			Assert.AreEqual ("tag1.tag2.tag3", tag3.ToString ());
		}

		[Test]
		public void TagToXmlToTagTest ()
		{
			Tag DUT = new Tag ("tagname");
			XmlDocument doc = new XmlDocument ();
			Tag recreated = Tag.RecreateFromXml (DUT.ToXml (doc));

			Assert.AreEqual (DUT, recreated);
		}

		[Test]
		public void TagTreeXmlRoundtrip ()
		{
			// setup
			int max = 3;

			// - create tags
			List<Tag> tags = new List<Tag> ();
			for (int i = 0; i < max; i++)
				tags.Add (new Tag ("tag" + (i + 1)));

			// - relate tags
			for (int i = 0; i < max - 1; i++)
				tags [i].Parent = tags [i + 1];

			// test
			// - to XML
			XmlDocument doc = new XmlDocument ();
			XmlNode xml = tags [0].ToXml (doc);

			// - recreate
			Tag recreated = Tag.RecreateFromXml (xml);

			Assert.AreEqual (tags [0].ToString (), recreated.ToString ());
			Assert.AreEqual (tags [0], recreated);
		}

		[Test]
		public void AddingTagsToEntriesTest ()
		{
			Entry entry = Entry.create ();

			Tag tag1 = new Tag ("tag1");
			Tag tag2 = new Tag ("tag2");

			entry.addTag (tag1);
			entry.addTag (tag2);

			Assert.AreEqual (tag1, entry.getTags () [0]);
			Assert.AreEqual (tag2, entry.getTags () [1]);
		}

		[Test]
		public void RemoveTagFromEntry ()
		{
			// setup
			Entry entry = Entry.create ();
			Tag tag = new Tag ("tagname");
			entry.addTag (tag);

			// test
			entry.removeTag (tag);

			// check
			Assert.IsEmpty (entry.getTags ());
		}

		[Test]
		public void TagFilterEntries ()
		{
			// setup
			int max = 3;

			// - create entries
			List<Entry> entries = new List<Entry> ();
			for (int i = 0; i < max; i++)
				entries.Add (Entry.create ());

			// - create tags
			List<Tag> tags = new List<Tag> ();
			for (int i = 0; i < max; i++)
				tags.Add (new Tag ("tag" + (i + 1)));

			// - tag entries
			for (int i = 0; i < max; i++)
				for (int j = i; j < max; j++)
					entries [i].addTag (tags [j]);

			// - persist
			foreach (Entry current in entries)
				current.persist ();

			// test
			// - create filter
			EntryFilter filter = new EntryFilter ();
			filter.IncludedTags.Add (tags [2]);
			filter.ExcludedTags.Add (tags [1]);
			List<Entry> result = Entry.getEntries (filter);

			// check
			foreach (Entry current in result) {
				Assert.Contains (tags [2], current.getTags ());
				Assert.IsFalse (current.getTags ().Contains (tags [1]), "entrylist contains entry with excluded tag");
			}

			// cleanup
			foreach (Entry current in entries)
				current.Delete ();
		}

		[Test]
		public void DeleteEntry ()
		{
			Entry entry = Entry.create ();
			entry.persist ();
			entry.Delete ();
		}
	}
}

