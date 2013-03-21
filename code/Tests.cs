using System;
using System.Xml;
using System.Collections.Generic;
using NUnit.Framework;
using backend;

namespace test
{
	[TestFixture()]
	public class ModelTests
	{
		List<NoteElement> listA;
		List<NoteElement> listB;

		[SetUp]
		protected void SetUp ()
		{
			// setup test data
			listA = new List<NoteElement> ();
			Polyline stroke = new Polyline ();
			stroke.Points.AddRange (new int[] {0,0,100,0,100,100});
			listA.Add (stroke);
			stroke = new Polyline ();
			stroke.Points.AddRange (new int[] {0,0,100,100});
			listA.Add (stroke);

			listB = new List<NoteElement> ();
			stroke = new Polyline ();
			stroke.Points.AddRange (new int[] {3,3,3,3,3,3});
			listB.Add (stroke);
			stroke = new Polyline ();
			stroke.Points.AddRange (new int[] {4,4,4,4,4,4});
			listB.Add (stroke);
		}

		[Test()]
		public void StrokesTest ()
		{
			// create DUT
			Note DUT = Note.create ();

			// do some edit tasks
			// - insert
			DUT.edit (new List<NoteElement> (), listA);
			Assert.AreEqual (listA.ToString (), DUT.get ().ToString (), "adding stroke failed");
			// - change
			DUT.edit (listA, listB);
			Assert.AreEqual (listB.ToString (), DUT.get ().ToString (), "altering stroke failed");
			// - delete
			DUT.edit (listB, new List<NoteElement> ());
			Assert.AreEqual (new List<NoteElement> ().ToString (), DUT.get ().ToString (), "deleting stroke failed");
		}

		[Test]
		public void PersistenceTest ()
		{
			// create DUT
			Note DUT = Note.create ();

			// create stroke
			DUT.edit (new List<NoteElement> (), listA);

			// save to disk
			DUT.persist ();

			// reload from disk
			DUT = Note.getEntries () [0];

			// check
			Assert.AreEqual (listA.ToString (), DUT.get ().ToString (), "reloading stroke from disk failed");

			// cleanup
			DUT.Delete ();
		}

		[Test]
		public void SimpleTagTest ()
		{
			Tag DUT = Tag.Create ("tag1");
			Assert.AreEqual (DUT.Name, DUT.ToString ());
		}

		[Test]
		public void TagTreeTest ()
		{
			// setup testing entities
			string tag1name = "tag1", tag2name = "tag2", tag3name = "tag3";
			Tag tag1 = Tag.Create (tag1name);
			Tag tag2 = Tag.Create (tag2name);
			Tag tag3 = Tag.Create (tag3name);

			// assemble tree
			tag3.Parent = tag2;
			tag2.Parent = tag1;

			Assert.AreEqual ("tag1.tag2.tag3", tag3.ToString ());
		}

		[Test]
		public void TagToXmlToTagTest ()
		{
			Tag DUT = Tag.Create ("tagname");
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
				tags.Add (Tag.Create ("tag" + (i + 1)));

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
			Note entry = Note.create ();

			Tag tag1 = Tag.Create ("tag1");
			Tag tag2 = Tag.Create ("tag2");

			entry.addTag (tag1);
			entry.addTag (tag2);

			Assert.AreEqual (tag1, entry.getTags () [0]);
			Assert.AreEqual (tag2, entry.getTags () [1]);
		}

		[Test]
		public void RemoveTagFromEntry ()
		{
			// setup
			Note entry = Note.create ();
			Tag tag = Tag.Create ("tagname");
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
			List<Note> entries = new List<Note> ();
			for (int i = 0; i < max; i++)
				entries.Add (Note.create ());

			// - create tags
			List<Tag> tags = new List<Tag> ();
			for (int i = 0; i < max; i++)
				tags.Add (Tag.Create ("tag" + (i + 1)));

			// - tag entries
			for (int i = 0; i < max; i++)
				for (int j = i; j < max; j++)
					entries [i].addTag (tags [j]);

			// - persist
			foreach (Note current in entries)
				current.persist ();

			// test
			// - create filter
			NoteFilter filter = new NoteFilter ();
			filter.IncludedTags.Add (tags [2]);
			filter.ExcludedTags.Add (tags [1]);
			List<Note> result = Note.getEntries (filter);

			// check
			foreach (Note current in result) {
				Assert.Contains (tags [2], current.getTags ());
				Assert.IsFalse (current.getTags ().Contains (tags [1]), "entrylist contains entry with excluded tag");
			}

			// cleanup
			foreach (Note current in entries)
				current.Delete ();
		}

		[Test]
		public void DeleteEntry ()
		{
			Note entry = Note.create ();
			entry.persist ();
			entry.Delete ();
		}
	}
}

