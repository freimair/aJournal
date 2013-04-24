using System;
using System.IO;
using System.Xml;
using System.Threading;
using System.Collections.Generic;
using NUnit.Framework;
using backend;
using backend.Tags;
using backend.NoteElements;

namespace test
{
	[TestFixture]
	public abstract class DatabaseTests
	{
		[SetUp]
		public void Setup ()
		{
			File.Delete ("test.db");
			Database.ConnectionString = "URI=file:test.db";

			FurtherSetup ();
		}

		public abstract void FurtherSetup ();
	}

	[TestFixture]
	public class ModelTests : DatabaseTests
	{
		List<NoteElement> listA;
		List<NoteElement> listB;

		public override void FurtherSetup ()
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

	}

	[TestFixture]
	public class NoteElementPersistenceTests : DatabaseTests
	{
		public override void FurtherSetup ()
		{

		}

		public void RoundtripElement (NoteElement DUT)
		{
			DUT.Persist ();
			DUT.Persist ();

			Assert.Contains (DUT, NoteElement.Elements);

			DUT.Remove ();

			Assert.IsEmpty (NoteElement.Elements);
		}

		[Test]
		public void RoundtripPolylineElement ()
		{
			RoundtripElement (new PolylineElement ());
		}

		[Test]
		public void RoundtripTextElement ()
		{
			RoundtripElement (new TextElement ());
		}

		[Test]
		public void RoundtripImageElement ()
		{
			RoundtripElement (new ImageElement ());
		}
	}

	[TestFixture]
	public class TagTests : DatabaseTests
	{
		public override void FurtherSetup ()
		{
			foreach (Tag current in Tag.Tags)
				current.Remove ();
			Tag.tagCache.Clear (); // FIXME find another way. this seems to be only necessary for testing
		}

		[Test]
		public void SimpleTagTest ()
		{
			Tag DUT = Tag.Create ("tag1");
			Assert.AreEqual (DUT.Name, DUT.ToString ());
		}

		[Test]
		public void RoundTripTest ()
		{
			Tag DUT = Tag.Create ("tag1");
			Assert.Contains (DUT, Tag.Tags);
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
			Assert.Contains (tag1, Tag.Tags);
			Assert.Contains (tag2, Tag.Tags);
			Assert.Contains (tag3, Tag.Tags);
		}

		[Test]
		public void TagToXmlToTagTest ()
		{
			Tag DUT = Tag.Create ("tagname");
			XmlDocument doc = new XmlDocument ();
			Tag recreated = Tag.RecreateFromXml (DUT.ToXml (doc));

			// TODO leaves a tag in the tag cache behind.

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

	[TestFixture]
	public class TaggingTest : DatabaseTests
	{
		public override void FurtherSetup ()
		{
			foreach (Tag current in Tag.Tags)
				current.Remove ();
			Tag.tagCache.Clear (); // FIXME find another way. this seems to be only necessary for testing
		}

		[Test]
		public void ElementTagging ()
		{
			Tag tag = Tag.Create ("tag1");
			Tag tag1 = Tag.Create ("tag2");

			NoteElement element = new PolylineElement ();
			element.Persist ();

			element.AddTag (tag);

			Assert.Contains (tag, element.Tags);
			Assert.AreEqual (1, element.Tags.Count);

			element.RemoveTag (tag);

			Assert.IsEmpty (element.Tags);
		}

		[Test]
		public void MultiElementTagging ()
		{
			// setup
			Tag[] tags = new Tag[5];
			for (int i = 0; i < 5; i++)
				tags [i] = Tag.Create ("tag" + i);

			NoteElement[] elements = new NoteElement[3];
			List<long> element_ids = new List<long> ();
			for (int i = 0; i < 3; i++) {
				elements [i] = new PolylineElement ();
				elements [i].Persist ();
				/*
				 * nasty hack. we do not get the id out of the
				 * NoteElement object. but since we have a fresh
				 * database for testing the ids will most likely
				 * start at 1
				 */
				element_ids.Add (i + 1);
			}

			// assign tags
			for (int i = 0; i < elements.Length; i++)
				for (int j = i; j < i + 3 && j < tags.Length; j++)
					elements [i].AddTag (tags [j]);

			foreach (Tag current in tags)
				Assert.Contains (current, Tag.AllTagsFor (element_ids));

			Assert.AreEqual (1, Tag.CommonTagsFor (element_ids).Count);
			Assert.Contains (tags [2], Tag.CommonTagsFor (element_ids));
		}

		[Test]
		public void ElementTagFilter ()
		{
			Tag[] tags = new Tag[3];
			NoteElement[] elements = new NoteElement[3];
			for (int i = 0; i < 3; i++) {
				tags [i] = Tag.Create ("tag" + i);
				elements [i] = new PolylineElement ();
				elements [i].Persist ();
			}

			elements [0].AddTag (tags [0]);
			elements [1].AddTag (tags [0]);
			elements [1].AddTag (tags [1]);

			List<NoteElement> result;

			ElementFilter filter = new ElementFilter ();
			result = NoteElement.GetElements (filter);
			Assert.AreEqual (1, result.Count);
			Assert.Contains (elements [2], result);

			filter.Tags.Add (tags [0]);
			result = NoteElement.GetElements (filter);
			Assert.AreEqual (3, result.Count);
			foreach (NoteElement current in elements)
				Assert.Contains (current, result);

			filter.Tags.Remove (tags [0]);
			filter.Tags.Add (tags [1]);
			result = NoteElement.GetElements (filter);
			Assert.AreEqual (2, result.Count);
			Assert.Contains (elements [1], result);
			Assert.Contains (elements [2], result);
		}

		[Test]
		public void ElementTimeFilter ()
		{
			// setup
			NoteElement element1 = new PolylineElement ();
			element1.Persist ();
			Thread.Sleep (100);
			ElementFilter filter = new ElementFilter ();
			filter.NewerAs = DateTime.Now;

			Thread.Sleep (200);
			NoteElement element2 = new PolylineElement ();
			element2.Persist ();

			// create dummy tag
			Tag tag = Tag.Create ("dummy");
			element1.AddTag (tag);


			List<NoteElement> result = NoteElement.GetElements (filter);
			Assert.AreEqual (1, result.Count);
			Assert.Contains (element2, result);
		}
		
		public static void Main (string[] args)
		{
			
		}
	}
}

