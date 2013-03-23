using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using backend.Tags;
using backend.NoteElements;

namespace backend
{
	public class NoteFilter
	{
		List<Tag> includedTags;

		public List<Tag> IncludedTags {
			get { return includedTags; }
		}

		List<Tag> excludedTags;

		public List<Tag> ExcludedTags {
			get { return excludedTags; }
		}

		public NoteFilter ()
		{
			includedTags = new List<Tag> ();
			excludedTags = new List<Tag> ();
		}
	}

	public class Note
	{
		string filename;
		XmlDocument document;
		XmlNode rootNode;
		XmlNode tagsNode;

		private Note (String file)
		{
			// instantiate XmlDocument and load XML from file
			document = new XmlDocument ();
			document.Load (file);

			rootNode = document.GetElementsByTagName ("svg") [0];
			tagsNode = document.GetElementsByTagName ("tags") [0];
		}

		private Note ()
		{
			document = new XmlDocument ();
			document.AppendChild (document.CreateXmlDeclaration ("1.0", "utf-8", null));

			rootNode = document.CreateElement ("svg");
			document.AppendChild (rootNode);

			XmlNode descriptionNode = document.CreateElement ("desc");
			rootNode.AppendChild (descriptionNode);

			tagsNode = document.CreateElement ("tags");
			descriptionNode.AppendChild (tagsNode);
		}

		public static Note Create ()
		{
			return new Note ();
		}

		public static List<Note> GetEntries ()
		{
			return GetEntries (null);
		}

		public static List<Note> GetEntries (NoteFilter filter)
		{
			String[] files = Directory.GetFiles (Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/.aJournal/", "*.svg");

			List<Note> result = new List<Note> ();
			foreach (String file in files) {
				Note candiate = new Note (file);
				bool addCandidate = false;

				try {
					foreach (Tag current in filter.IncludedTags) {
						if (candiate.GetTags ().Contains (current)) {
							addCandidate = true;
							break;
						}
					}

					foreach (Tag current in filter.ExcludedTags) {
						if (candiate.GetTags ().Contains (current)) {
							addCandidate = false;
							break;
						}
					}
				} catch (NullReferenceException) {
					addCandidate = true;
				}

				if (addCandidate)
					result.Add (candiate);
			}

			return result;
		}

		public void AddTag (Tag tag)
		{
			tagsNode.AppendChild (tag.ToXml (document));
		}

		public void RemoveTag (Tag tag)
		{
			XmlNode node = tagsNode.SelectSingleNode ("//tag[text()='" + tag.Name + "']");
			tagsNode.RemoveChild (node);
		}

		public List<Tag> GetTags ()
		{
			List<Tag> result = new List<Tag> ();
			foreach (XmlNode current in tagsNode.ChildNodes) {
				result.Add (Tag.RecreateFromXml (current));
			}
			return result;
		}

		public List<NoteElement> GetElements ()
		{
			XmlNodeList nodes = rootNode.SelectNodes ("/svg/polyline");

			List<NoteElement> result = new List<NoteElement> ();
			foreach (XmlNode current in nodes)
				result.Add (NoteElement.RecreateFromXml (current));

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
		public void Edit (List<NoteElement> before, List<NoteElement> after)
		{
			// remove deprecated polylines
			foreach (NoteElement current in before)
				RemoveElement (current);

			// add new polylines
			foreach (NoteElement current in after)
				AddElement (current);
		}

		public void AddElement (NoteElement element)
		{
			rootNode.AppendChild (element.ToXml (document));
		}

		public void RemoveElement (NoteElement element)
		{
			rootNode.RemoveChild (element.Find (rootNode));
		}

		public DateTime CreationTimestamp {
			get{ return new DateTime ();}
		}

		public DateTime ModificationTimestamp {
			get { return new DateTime ();}
		}

		public void Persist ()
		{
			// check and adjust boundaries
			// TODO check and adjust boundaries

			// check if data directory exists and create if neccessary
			if (!Directory.Exists (Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/.aJournal/"))
				Directory.CreateDirectory (Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/.aJournal/");

			filename = DateTime.Now.ToString ("yyyyMMddHHmmss");
			document.Save (Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/.aJournal/" + filename + ".svg");
		}

		public void Delete ()
		{
			File.Delete (Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/.aJournal/" + filename + ".svg");
		}

		static void Main (string[] args)
		{
			// dummy entry point
		}
	}
}

