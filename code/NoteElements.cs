using System;
using System.Xml;
using System.Collections.Generic;

namespace backend
{
	namespace NoteElements
	{
		/**
	 * some Drawable
	 */
		public abstract class NoteElement
		{
			public static NoteElement RecreateFromXml (XmlNode node)
			{
				switch (node.Name) {
				case "polyline":
					return new PolylineElement (node);
				default:
					throw new Exception ("no matching drawable found");
				}
			}

			public abstract XmlNode Find (XmlNode root);

			public abstract XmlNode ToXml (XmlDocument document);
		}

		public class PolylineElement : NoteElement
		{
			string color = "red";
			int strength = 1;
			private List<int> points;

			public List<int> Points {
				get { return points; }
				set { points = value; }
			}

			public PolylineElement ()
			{
				points = new List<int> ();
			}

			public PolylineElement (XmlNode node) : this()
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
	}
}

