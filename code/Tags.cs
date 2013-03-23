using System;
using System.Xml;
using System.Collections.Generic;

namespace backend
{
	namespace Tags
	{
		public class Tag
		{
			static Dictionary<string, Tag> tagCache = new Dictionary<string, Tag> ();

			static void updateCache ()
			{
				var buffer = new List<string> (tagCache.Keys);
				foreach (string current in buffer) {
					if (!current.Equals (tagCache [current].ToString ())) {
						Tag tmp = tagCache [current];
						tagCache.Remove (current);
						tagCache.Add (tmp.ToString (), tmp);
					}
				}
			}

			public static Tag Create (string path)
			{
				try {
					return tagCache [path];
				} catch (KeyNotFoundException) {
					// create
					Tag newTag = new Tag (path.Substring (path.LastIndexOf (".") + 1));

					// find parent
					if (path.Contains ("."))
						newTag.Parent = Create (path.Substring (0, path.LastIndexOf (".")));

					tagCache.Add (path, newTag);
					return newTag;
				}
			}

			public static Tag RecreateFromXml (XmlNode node)
			{
				return Create (node.FirstChild.Value);
			}

			Tag (string name)
			{
				Name = name;
			}

			string name;

			public string Name {
				get { return name; }
				set {
					name = value;
					updateCache ();
				}
			}

			Tag parent;

			public Tag Parent {
				get { return parent; }
				set {
					parent = value;
					updateCache ();
				}
			}

			public XmlNode ToXml (XmlDocument document)
			{
				XmlNode tagNode = document.CreateElement ("tag");
				tagNode.AppendChild (document.CreateTextNode (ToString ()));
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

//		public override bool Equals (object obj)
//		{
//			if (obj.GetType () != this.GetType ())
//				return false;
//			Tag other = (Tag)obj;
//			if (!Name.Equals (other.Name))
//				return false;
//			if (Parent != null && other.Parent != null) {
//				if (!Parent.Equals (((Tag)obj).Parent))
//					return false;
//			}
//
//			return true;
//		}
		}

	}
}

