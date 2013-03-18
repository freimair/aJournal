using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace code
{
	public class Entry
	{
		XmlDocument document;

		XmlNode rootNode;
		XmlNode tagsNode;

		private Entry (String file)
		{
			// instantiate XmlDocument and load XML from file
			document = new XmlDocument ();
			document.Load (file);

			rootNode = document.GetElementsByTagName ("svg") [0];
			tagsNode = document.GetElementsByTagName ("tags") [0];
		}

		private Entry ()
		{
			document = new XmlDocument ();
			document.AppendChild (document.CreateXmlDeclaration ("1.0", "utf-8", null));

			rootNode = document.CreateElement ("svg");
			document.AppendChild (rootNode);

			XmlNode descriptionNode = document.CreateElement ("desc");
			rootNode.AppendChild (descriptionNode);

			tagsNode = document.CreateElement ("tags");
			descriptionNode.AppendChild (tagsNode);

			String[] tags = new String[]{"tag1", "tag2", "tag3"};
			foreach (String tag in tags) {
				XmlNode tagNode = document.CreateElement ("tag");
				tagNode.AppendChild (document.CreateTextNode (tag));
				tagsNode.AppendChild (tagNode);
			}
		}

		public static Entry create ()
		{
			return new Entry ();
		}

		public static List<Entry> getEntries ()
		{
			String[] files = Directory.GetFiles (Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/.aJournal/", "*.svg");

			// introduce some sort of caching functionality
			List<Entry> result = new List<Entry> ();
			foreach (String file in files)
				result.Add (new Entry (file));

			return result;
		}

		public List<int[]> get ()
		{
			XmlNodeList nodes = rootNode.SelectNodes ("/svg/polyline/@points");

			List<int[]> result = new List<int[]> ();
			for (int i = 0; i < nodes.Count; i++) {
				// parse points
				String[] values = nodes [i].Value.Split (new char[] {' ', ','});

				// create integer array for frontend
				int[] current = new int[values.Length];
				int j = 0;
				foreach (String currentValue in values)
					current [j++] = Convert.ToInt32 (currentValue);

				// add it to the list of strokes
				result.Add (current);
			}
			return result;
		}

		/*
		 * http://www.w3.org/TR/SVG/struct.html
		 * 
		 * <svg width="12cm" height="4cm" viewBox="0 0 1200 400" xmlns="http://www.w3.org/2000/svg" version="1.1">
		 *     <desc><tags><tag>jour fixe</tag><tag>skytrust</tag></tags></desc>
		 *     <polyline fill="none" stroke="blue" stroke-width="10" points="50,375
         *          150,375 150,325 250,325 250,375
         *          350,375 350,250 450,250 450,375
         *          550,375 550,175 650,175 650,375
         *          750,375 750,100 850,100 850,375
         *          950,375 950,25 1050,25 1050,375
         *          1150,375" />
         * </svg>
         */
		public void edit (List<int[]> before, List<int[]> after)
		{
			// remove deprecated polylines
			foreach (int[] current in before) {
				// locate node
				String pattern = "";
				for (int i = 0; i < current.Length; i += 2)
					pattern += current [i] + "," + current [i + 1] + " ";
				XmlNode currentNode = rootNode.SelectSingleNode ("/svg/polyline[@points='" + pattern.Trim () + "']");

				// remove node
				rootNode.RemoveChild (currentNode);
			}

			// add new polylines
			foreach (int[] current in after) {
				XmlAttribute fillAttribute = document.CreateAttribute ("fill");
				fillAttribute.Value = "none";

				XmlAttribute strokeAttribute = document.CreateAttribute ("stroke");
				strokeAttribute.Value = "black";

				XmlAttribute strokeWidthAttribute = document.CreateAttribute ("stroke-width");
				strokeWidthAttribute.Value = "10";

				XmlAttribute pointsAttribute = document.CreateAttribute ("points");
				for (int i = 0; i < current.Length; i += 2)
					pointsAttribute.Value += current [i] + "," + current [i + 1] + " ";
				pointsAttribute.Value = pointsAttribute.Value.Trim ();

				XmlNode currentNode = document.CreateElement ("polyline");

				currentNode.Attributes.Append (fillAttribute);
				currentNode.Attributes.Append (strokeAttribute);
				currentNode.Attributes.Append (strokeWidthAttribute);
				currentNode.Attributes.Append (pointsAttribute);
				rootNode.AppendChild (currentNode);
			}
		}

		public DateTime getCreationTimestamp ()
		{
			return new DateTime ();
		}

		public DateTime getModificationTimestamp ()
		{

			return new DateTime ();
		}

		public void persist ()
		{
			// check and adjust boundaries
			// TODO check and adjust boundaries

			// check if data directory exists and create if neccessary
			if (!Directory.Exists (Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/.aJournal/"))
				Directory.CreateDirectory (Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/.aJournal/");

			document.Save (Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/.aJournal/probe.svg");
		}

		public static int Main (string[] args)
		{
			Entry DUT = new Entry ();
			List<int[]> listA = new List<int[]> ();
			listA.Add (new int[] {1,1,1,1,1,1});
			listA.Add (new int[] {2,2,2,2,2,2});

			List<int[]> listB = new List<int[]> ();
			listB.Add (new int[] {3,3,3,3,3,3});
			listB.Add (new int[] {4,4,4,4,4,4});

			DUT.edit (new List<int[]> (), listA);
			DUT.edit (listA, listB);
			DUT.edit (listB, new List<int[]> ());
			DUT.edit (new List<int[]> (), listA);

			System.Console.WriteLine (listA.ToString () == DUT.get ().ToString ());

			DUT.persist ();

			DUT = Entry.getEntries () [0];
			System.Console.WriteLine (listA.ToString () == DUT.get ().ToString ());

			return 0;
		}
	}
}

