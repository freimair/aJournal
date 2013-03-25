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
		HashSet<NoteElement> elements = new HashSet<NoteElement> ();
		HashSet<Tag> tags = new HashSet<Tag> ();

		private Note (String file)
		{
			filename = file;
			// instantiate XmlDocument and load XML from file
			XmlDocument document = new XmlDocument ();
			document.Load (file);

			// recreate elements
			XmlNodeList svgNodeList = document.SelectNodes ("/svg/*");
			foreach (XmlNode current in svgNodeList) {
				NoteElement tmp = NoteElement.RecreateFromXml (current);
				if (null != tmp)
					elements.Add (tmp);
			}

			// recreate tags
			XmlNodeList svgTagList = document.SelectNodes ("/svg/desc/tags/tag");
			foreach (XmlNode current in svgTagList) {
				Tag tmp = Tag.RecreateFromXml (current);
				if (null != tmp)
					tags.Add (tmp);
			}
		}

		private Note ()
		{
			filename = Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/.aJournal/" + DateTime.Now.ToString ("yyyyMMddHHmmss") + ".svg";
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
			tags.Add (tag);
		}

		public void RemoveTag (Tag tag)
		{
			tags.Remove (tag);
		}

		public List<Tag> GetTags ()
		{
			return new List<Tag> (tags);
		}

		public List<NoteElement> GetElements ()
		{
			return new List<NoteElement> (elements);
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
			elements.Add (element);
		}

		public void RemoveElement (NoteElement element)
		{
			elements.Remove (element);
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

			// assemble svg document
			XmlDocument document = new XmlDocument ();
			document.AppendChild (document.CreateXmlDeclaration ("1.0", "utf-8", null));

			XmlNode rootNode = document.CreateElement ("svg");
			document.AppendChild (rootNode);

			XmlNode descriptionNode = document.CreateElement ("desc");
			rootNode.AppendChild (descriptionNode);

			XmlNode tagsNode = document.CreateElement ("tags");
			descriptionNode.AppendChild (tagsNode);

			foreach (Tag current in tags)
				tagsNode.AppendChild (current.ToXml (document));

			foreach (NoteElement current in elements)
				rootNode.AppendChild (current.ToXml (document));

			// check if data directory exists and create if neccessary
			if (!Directory.GetParent (filename).Exists)
				Directory.GetParent (filename).Create ();

			document.Save (filename);
		}

		public void Delete ()
		{
			File.Delete (filename);
		}

		static void Main (string[] args)
		{
			// dummy entry point
		}
	}
}

