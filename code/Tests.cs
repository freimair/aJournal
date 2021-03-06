using System;
using System.Xml;
using System.Collections.Generic;
using NUnit.Framework;
using backend;
using backend.Tags;
using backend.NoteElements;

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
			PolylineElement stroke = new PolylineElement ();
			stroke.Points.AddRange (new int[] {0,0,100,0,100,100});
			listA.Add (stroke);
			stroke = new PolylineElement ();
			stroke.Points.AddRange (new int[] {0,0,100,100});
			listA.Add (stroke);

			listB = new List<NoteElement> ();
			stroke = new PolylineElement ();
			stroke.Points.AddRange (new int[] {3,3,3,3,3,3});
			listB.Add (stroke);
			stroke = new PolylineElement ();
			stroke.Points.AddRange (new int[] {4,4,4,4,4,4});
			listB.Add (stroke);
		}

		[Test]
		public void ElementEditTest ()
		{
			// create DUT
			Note DUT = Note.Create ();

			// do some edit tasks
			// - insert
			DUT.Edit (new List<NoteElement> (), listA);
			Assert.AreEqual (listA.ToString (), DUT.GetElements ().ToString (), "adding stroke failed");
			// - change
			DUT.Edit (listA, listB);
			Assert.AreEqual (listB.ToString (), DUT.GetElements ().ToString (), "altering stroke failed");
			// - delete
			DUT.Edit (listB, new List<NoteElement> ());
			Assert.AreEqual (new List<NoteElement> ().ToString (), DUT.GetElements ().ToString (), "deleting stroke failed");
		}

		[Test]
		public void PersistenceTest ()
		{
			// create DUT
			Note DUT = Note.Create ();

			// create stroke
			DUT.Edit (new List<NoteElement> (), listA);

			// save to disk
			DUT.Persist ();

			// reload from disk
			DUT = Note.GetEntries () [0];

			// cleanup
			DUT.Delete ();

			// check
			Assert.AreEqual (listA.ToString (), DUT.GetElements ().ToString (), "reloading stroke from disk failed");
		}

		[Test]
		public void AddingTagsToNoteTest ()
		{
			Note entry = Note.Create ();
			
			Tag tag1 = Tag.Create ("tag1");
			Tag tag2 = Tag.Create ("tag2");
			
			entry.AddTag (tag1);
			entry.AddTag (tag2);
			
			Assert.AreEqual (tag1, entry.GetTags () [0]);
			Assert.AreEqual (tag2, entry.GetTags () [1]);
		}

		[Test]
		public void TaggedNoteRoundtrip ()
		{
			Tag tag = Tag.Create ("mytag");
			Note note = Note.Create ();

			note.AddTag (tag);
			note.Persist ();

			List<Note> recreated = Note.GetEntries ();
			note.Delete ();

			Assert.Contains (tag, recreated [0].GetTags ());
		}

		[Test]
		public void RemoveTagFromNote ()
		{
			// setup
			Note entry = Note.Create ();
			Tag tag = Tag.Create ("tagname");
			entry.AddTag (tag);

			// test
			entry.RemoveTag (tag);

			// check
			Assert.IsEmpty (entry.GetTags ());
		}

		[Test]
		public void TagFilterNote ()
		{
			// setup
			int max = 3;

			// - create entries
			List<Note> entries = new List<Note> ();
			for (int i = 0; i < max; i++)
				entries.Add (Note.Create ());

			// - create tags
			List<Tag> tags = new List<Tag> ();
			for (int i = 0; i < max; i++)
				tags.Add (Tag.Create ("tag" + (i + 1)));

			// - tag entries
			for (int i = 0; i < max; i++)
				for (int j = i; j < max; j++)
					entries [i].AddTag (tags [j]);

			// - persist
			foreach (Note current in entries)
				current.Persist ();

			// test
			// - create filter
			NoteFilter filter = new NoteFilter ();
			filter.IncludedTags.Add (tags [2]);
			filter.ExcludedTags.Add (tags [1]);
			List<Note> result = Note.GetEntries (filter);

			// cleanup
			foreach (Note current in entries)
				current.Delete ();

			// check
			foreach (Note current in result) {
				Assert.Contains (tags [2], current.GetTags ());
				Assert.IsFalse (current.GetTags ().Contains (tags [1]), "entrylist contains entry with excluded tag");
			}
		}

		[Test]
		public void DeleteNote ()
		{
			Note entry = Note.Create ();
			entry.Persist ();
			entry.Delete ();
		}

		[Test]
		public void SizePersistenceTest ()
		{
			Note DUT = Note.Create ();
			DUT.Height = 200;
			DUT.Width = 400;
			DUT.Persist ();

			Assert.AreEqual (200, Note.GetEntries () [0].Height);
			Assert.AreEqual (400, Note.GetEntries () [0].Width);

			DUT.Delete ();
		}

		[Test]
		public void TagListTest ()
		{
			int max = 10;
			List<Tag> tags = new List<Tag> ();
			List<Note> notes = new List<Note> ();

			for (int i = 0; i < max; i++) {
				Note note = Note.Create ();
				Tag tag = Tag.Create ("tag" + i);
				if (max / 2 < i)
					tag.Parent = tags [i - 1];
				note.AddTag (tag);
				note.Persist ();

				notes.Add (note);
				tags.Add (tag);
			}

			foreach (Tag tag in tags)
				Assert.Contains (tag, new List<Tag> (Note.AllTags));

			// cleanup
			foreach (Note note in notes)
				note.Delete ();
		}
	}

	[TestFixture]
	public class NoteElementTests
	{
		[Test]
		public void PolylineSvgRoundtrip ()
		{
			Note note = Note.Create ();
			PolylineElement DUT = new PolylineElement ();
			DUT.Points.AddRange (new int[]{1,2,3,4,5,6});
			note.AddElement (DUT);

			note.Persist ();

			List<NoteElement> recreated = Note.GetEntries () [0].GetElements ();
			note.Delete (); // comment for visual svg check

			Assert.Contains (DUT, recreated);
		}

		[Test]
		public void TextXmlRoundtrip ()
		{
			int y = 0;

			Note note = Note.Create ();

			List<TextElement> DUTs = new List<TextElement> ();
			TextElement DUT = new TextElement ();
			DUT.Text = "Heading";
			DUT.X = 10;
			DUT.Y = 0;
			y += 15;
			DUT.FontSize = 20;
			DUT.FontStrong = true;
			note.AddElement (DUT);
			DUTs.Add (DUT);

			DUT = new TextElement ();
			DUT.Text = "normal";
			DUT.X = 10;
			DUT.Y = y += 15;
			note.AddElement (DUT);
			DUTs.Add (DUT);

			DUT = new TextElement ();
			DUT.Text = "one ident";
			DUT.X = 10;
			DUT.Y = y += 15;
			DUT.IndentationLevel = 1;
			note.AddElement (DUT);
			DUTs.Add (DUT);

			DUT = new TextElement ();
			DUT.Text = "two ident";
			DUT.X = 10;
			DUT.Y = y += 15;
			DUT.IndentationLevel = 2;
			note.AddElement (DUT);
			DUTs.Add (DUT);

			DUT = new TextElement ();
			DUT.Text = "three ident";
			DUT.X = 10;
			DUT.Y = y += 15;
			DUT.IndentationLevel = 3;
			note.AddElement (DUT);
			DUTs.Add (DUT);

			DUT = new TextElement ();
			DUT.Text = "four ident";
			DUT.X = 10;
			DUT.Y = y += 15;
			DUT.IndentationLevel = 4;
			note.AddElement (DUT);
			DUTs.Add (DUT);

			DUT = new TextElement ();
			DUT.Text = "two ident";
			DUT.X = 10;
			DUT.Y = y += 15;
			DUT.IndentationLevel = 2;
			note.AddElement (DUT);
			DUTs.Add (DUT);

			DUT = new TextElement ();
			DUT.Text = "one ident";
			DUT.X = 10;
			DUT.Y = y += 15;
			DUT.IndentationLevel = 1;
			note.AddElement (DUT);
			DUTs.Add (DUT);

			DUT = new TextElement ();
			DUT.Text = "normal";
			DUT.X = 10;
			DUT.Y = y += 15;
			DUT.IndentationLevel = 0;
			note.AddElement (DUT);
			DUTs.Add (DUT);

			note.Persist ();

			List<NoteElement> recreated = Note.GetEntries () [0].GetElements ();
			note.Delete (); // comment for visual svg check

			Assert.AreEqual (DUTs.Count, recreated.Count);
			Assert.Contains (DUTs [0], recreated, "heading: text, font size, font weight, position invalid");
			Assert.Contains (DUTs [1], recreated, "normal: text, font size, font weight, position invalid");
			for (int i = 2; i < DUTs.Count; i++)
				Assert.Contains (DUTs [i], recreated, "text with indent recreation failed");

		}

		[Test]
		public void TextRemovalTest ()
		{
			Note note = Note.Create ();

			// without indentation
			TextElement DUT = new TextElement ();
			DUT.Text = "text";
			DUT.X = 10;
			DUT.Y = 10;
			note.AddElement (DUT);
			Assert.Contains (DUT, note.GetElements ());
			note.RemoveElement (DUT);
			Assert.IsEmpty (note.GetElements (), "removing a text element failed");
		}

		[Test]
		public void TextWithIndentationRemovalTest ()
		{
			Note note = Note.Create ();

			// with indentation
			TextElement DUT = new TextElement ();
			DUT.Text = "text";
			DUT.X = 10;
			DUT.Y = 10;
			DUT.IndentationLevel = 3;
			note.AddElement (DUT);
			Assert.Contains (DUT, note.GetElements ());
			note.RemoveElement (DUT);
			Assert.IsEmpty (note.GetElements (), "removing a text element with indentation failed");
		}

		[Test]
		public void ImageXmlRoundtripTest ()
		{
			Note note = Note.Create ();

			ImageElement DUT = new ImageElement ();
			DUT.X = 10;
			DUT.Y = 10;
			DUT.Width = 20;
			DUT.Height = 20;
			DUT.LoadFromFile ("rect-select.png");

			note.AddElement (DUT);
			note.Persist ();

			List<NoteElement> recreated = Note.GetEntries () [0].GetElements ();

			note.Delete (); // comment for visual svg check
			Assert.IsNotEmpty (recreated);
			Assert.Contains (DUT, recreated);

		}

		[Test]
		public void ImageRemovalTest ()
		{
			Note note = Note.Create ();

			ImageElement DUT = new ImageElement ();
			DUT.X = 10;
			DUT.Y = 10;
			DUT.Width = 20;
			DUT.Height = 20;
			DUT.LoadFromFile ("rect-select.png");

			note.AddElement (DUT);
			Assert.IsNotEmpty (note.GetElements ());
			note.RemoveElement (DUT);
			Assert.IsEmpty (note.GetElements ());
		}
	}

	[TestFixture]
	public class TagTests
	{

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
	}

}

