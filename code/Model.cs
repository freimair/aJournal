using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace code
{
	public class Tag
	{
		public static Tag RecreateFromXml (XmlNode node)
		{
			// TODO insert tag caching mechanism here
			return new Tag (node);
		}

		Tag (XmlNode node)
		{
			// TODO insert error handling here
			Name = node.FirstChild.Value;
		}

		public Tag (string name)
		{
			Name = name;
		}

		string name;

		public string Name {
			get { return name; }
			set { name = value; }
		}

		Tag parent;

		public Tag Parent {
			get { return parent; }
			set { parent = value; }
		}

		public XmlNode ToXml (XmlDocument document)
		{
			XmlNode tagNode = document.CreateElement ("tag");
			tagNode.AppendChild (document.CreateTextNode (name));
			return tagNode;
		}

		public override string ToString ()
		{
			string result = name;
			try {
				result = parent.ToString () + "." + result;
			} catch (NullReferenceException) {
			}
			return result;
		}

		public override bool Equals (object obj)
		{
			if (obj.GetType () != this.GetType ())
				return false;
			if (!Name.Equals (((Tag)obj).Name))
				return false;
			return true;
		}
	}

	/**
	 * some Drawable
	 */
	public abstract class Drawable
	{
		public static Drawable RecreateFromXml (XmlNode node)
		{
			switch (node.Name) {
			case "polyline":
				return new Stroke (node);
			default:
				throw new Exception ("no matching drawable found");
			}
		}

		public abstract XmlNode Find (XmlNode root);

		public abstract XmlNode ToXml (XmlDocument document);
	}

	public class Stroke : Drawable
	{
		string color = "red";
		int strength = 1;
		private List<int> points;

		public List<int> Points {
			get { return points; }
			set { points = value; }
		}

		public Stroke ()
		{
			points = new List<int> ();
		}

		public Stroke (XmlNode node) : this()
		{
			// parse points
			String[] values = node.Attributes.GetNamedItem ("points").Value.Split (new char[] {' ', ','});

			foreach (String currentValue in values)
				points.Add (Convert.ToInt32 (currentValue));
		}

		private string GetSVGPointList ()
		{
			string result = "";
			for (int i = 0; i < points.Count; i += 2)
				result += points [i] + "," + points [i + 1] + " ";
			return result.Trim ();
		}

		public override XmlNode Find (XmlNode root)
		{
			return root.SelectSingleNode ("/svg/polyline[@points='" + GetSVGPointList () + "']");
		}

		public override XmlNode ToXml (XmlDocument document)
		{
			XmlAttribute fillAttribute = document.CreateAttribute ("fill");
			fillAttribute.Value = "none";

			XmlAttribute strokeAttribute = document.CreateAttribute ("stroke");
			strokeAttribute.Value = color;

			XmlAttribute strokeWidthAttribute = document.CreateAttribute ("stroke-width");
			strokeWidthAttribute.Value = strength.ToString ();

			XmlAttribute pointsAttribute = document.CreateAttribute ("points");
			pointsAttribute.Value = GetSVGPointList ();

			XmlNode currentNode = document.CreateElement ("polyline");

			currentNode.Attributes.Append (fillAttribute);
			currentNode.Attributes.Append (strokeAttribute);
			currentNode.Attributes.Append (strokeWidthAttribute);
			currentNode.Attributes.Append (pointsAttribute);

			return currentNode;
		}
	}

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

		public void addTag (Tag tag)
		{
			tagsNode.AppendChild (tag.ToXml (document));
		}

		public void removeTag (Tag tag)
		{
			XmlNode node = tagsNode.SelectSingleNode ("//tag[text()='" + tag.Name + "']");
			tagsNode.RemoveChild (node);
		}

		public List<Tag> getTags ()
		{
			List<Tag> result = new List<Tag> ();
			foreach (XmlNode current in tagsNode.ChildNodes) {
				result.Add (Tag.RecreateFromXml (current));
			}
			return result;
		}

		public List<Drawable> get ()
		{
			XmlNodeList nodes = rootNode.SelectNodes ("/svg/polyline");

			List<Drawable> result = new List<Drawable> ();
			foreach (XmlNode current in nodes)
				result.Add (Drawable.RecreateFromXml (current));

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
		public void edit (List<Drawable> before, List<Drawable> after)
		{
			// remove deprecated polylines
			foreach (Drawable current in before)
				rootNode.RemoveChild (current.Find (rootNode));

			// add new polylines
			foreach (Drawable current in after)
				rootNode.AppendChild (current.ToXml (document));
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

			document.Save (Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/.aJournal/" + DateTime.Now.ToString ("yyyyMMddHHmmss") + ".svg");
		}

		static void Main (string[] args)
		{
			// dummy entry point
		}
	}
}

