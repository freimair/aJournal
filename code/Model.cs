using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace code
{
	public class Entry
	{
		XmlDocument document;

		XmlNode root;

		private Entry ()
		{
			document = new XmlDocument ();
			document.AppendChild (document.CreateXmlDeclaration ("1.0", "utf-8", null));

			root = document.CreateElement ("svg");
			document.AppendChild (root);

			XmlNode descriptionNode = document.CreateElement ("desc");
			root.AppendChild (descriptionNode);

			XmlNode tagsNode = document.CreateElement ("tags");
			descriptionNode.AppendChild (tagsNode);

			String[] tags = new String[]{"tag1", "tag2", "tag3"};
			foreach (String tag in tags) {
				XmlNode tagNode = document.CreateElement ("tag");
				tagNode.AppendChild (document.CreateTextNode (tag));
				tagsNode.AppendChild (tagNode);
			}

//			// instantiate XmlDocument and load XML from file
//			XmlDocument doc = new XmlDocument ();
//			doc.Load (@"D:\test.xml");
//
//// get a list of nodes - in this case, I'm selecting all <AID> nodes under
//// the <GroupAIDs> node - change to suit your needs
//			XmlNodeList aNodes = doc.SelectNodes ("/Equipment/DataCollections/GroupAIDs/AID");
//
//// loop through all AID nodes
//			foreach (XmlNode aNode in aNodes) {
//				// grab the "id" attribute
//				XmlAttribute idAttribute = aNode.Attributes ["id"];
//
//				// check if that attribute even exists...
//				if (idAttribute != null) {
//					// if yes - read its current value
//					string currentValue = idAttribute.Value;
//
//					// here, you can now decide what to do - for demo purposes,
//					// I just set the ID value to a fixed value if it was empty before
//					if (string.IsNullOrEmpty (currentValue)) {
//						idAttribute.Value = "515";
//					}
//				}
//			}
//
//// save the XmlDocument back to disk
//			doc.Save (@"D:\test2.xml");
		}

		public static Entry create ()
		{
			return new Entry ();
		}

		public static List<Entry> getEntries ()
		{
			return new List<Entry> ();
		}

		public List<Array> get ()
		{
			return new List<Array> ();
		}

		public void edit (List<Array> before, List<Array> after)
		{
			// http://www.w3.org/TR/SVG/struct.html
//			<svg width="12cm" height="4cm" viewBox="0 0 1200 400"
//     xmlns="http://www.w3.org/2000/svg" version="1.1">
//  <desc><tags><tag>jour fixe</tag><tag>skytrust</tag></tags></desc>
//
//  <!-- Show outline of canvas using 'rect' element -->
//  <rect x="1" y="1" width="1198" height="398"
//        fill="none" stroke="blue" stroke-width="2" />
//
//  <polyline fill="none" stroke="blue" stroke-width="10" 
//            points="50,375
//                    150,375 150,325 250,325 250,375
//                    350,375 350,250 450,250 450,375
//                    550,375 550,175 650,175 650,375
//                    750,375 750,100 850,100 850,375
//                    950,375 950,25 1050,25 1050,375
//                    1150,375" />
//</svg>

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
			// check if data directory exists and create if neccessary
			if (!Directory.Exists (Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/.aJournal/"))
				Directory.CreateDirectory (Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/.aJournal/");

			document.Save (Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/.aJournal/probe.svg");
		}

		public static int Main (string[] args)
		{
			Entry DUT = new Entry ();
			DUT.persist ();
			return 0;
		}

	}
}

