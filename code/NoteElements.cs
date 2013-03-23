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
				case "text":
				case "g":
					return new TextElement (node);
				case "desc":
					return null;
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

		public class TextElement : NoteElement
		{
			string text = "";
			int indentationLevel = 0;
			int size = 10;
			bool strong = false;
			string color = "red";
			int x = 0, y = 0;

			public string Text {
				get{ return text;}
				set{ text = value;}
			}

			public int IndentationLevel {
				get{ return indentationLevel;}
				set{ indentationLevel = value;}
			}

			public int FontSize {
				get{ return size;}
				set{ size = value;}
			}

			public bool FontStrong {
				get{ return strong;}
				set{ strong = value;}
			}

			public int X {
				get{ return x;}
				set{ x = value;}
			}

			public int Y {
				get{ return y;}
				set{ y = value;}
			}

			public TextElement ()
			{
			}

			public TextElement (XmlNode node)
			{

				// scan nodes for the text node
				// we do not need the bullet
				XmlNode textNode = node;
				if (0 < node.ChildNodes.Count)
					foreach (XmlNode current in node.ChildNodes)
						if ("text".Equals (current.Name))
							textNode = current;

				// first, find fontsize first!
				foreach (XmlAttribute current in textNode.Attributes)
					if ("font-size".Equals (current.Name)) {
						FontSize = Convert.ToInt32 (current.Value);
						break;
					}

				foreach (XmlAttribute current in textNode.Attributes) {
					switch (current.Name) {
					case "x":
						X = Convert.ToInt32 (current.Value);
						break;
					case "y":
						Y = Convert.ToInt32 (current.Value) - FontSize;
						break;
					case "fill":
						color = current.Value;
						break;
					case "font-weight":
						FontStrong = current.Value == "bold" ? true : false;
						break;
					case "transform":
						int indent = Convert.ToInt32 (current.Value.Replace ("translate(", "").Replace (")", ""));
						IndentationLevel = ParseIndent (indent);
						break;
					}
				}
				Text = textNode.InnerText;
			}

			public override XmlNode Find (XmlNode root)
			{
				if (0 < IndentationLevel)
					return root.SelectSingleNode ("/svg/g[text[@x='" + X + "' and @y='" + (Y + FontSize) + "' and text() = '" + Text + "']]");
				else
					return root.SelectSingleNode ("/svg/text[@x='" + X + "' and @y='" + (Y + FontSize) + "' and text() = '" + Text + "']");
			}

			/**
			 * <text x="250" y="150" font-family="Verdana" font-size="55" fill="blue" >
			 * 	Hello, out there
			 * </text>
			 */
			public override XmlNode ToXml (XmlDocument document)
			{
				XmlNode currentNode = document.CreateElement ("text");

				XmlAttribute a = document.CreateAttribute ("x");
				a.Value = Convert.ToString (X);
				currentNode.Attributes.Append (a);

				a = document.CreateAttribute ("y");
				a.Value = Convert.ToString (Y + FontSize);
				currentNode.Attributes.Append (a);

				a = document.CreateAttribute ("fill");
				a.Value = color;
				currentNode.Attributes.Append (a);

				a = document.CreateAttribute ("font-size");
				a.Value = Convert.ToString (FontSize);
				currentNode.Attributes.Append (a);

				a = document.CreateAttribute ("font-weight");
				a.Value = strong ? "bold" : "normal";
				currentNode.Attributes.Append (a);

				a = document.CreateAttribute ("font-family");
				a.Value = "sans serif";
				currentNode.Attributes.Append (a);

				if (indentationLevel > 0) {
					a = document.CreateAttribute ("transform");
					a.Value = "translate(" + indent (0.5) + ")";
					currentNode.Attributes.Append (a);
				}

				XmlNode textNode = document.CreateTextNode (Text);
				currentNode.AppendChild (textNode);

				if (indentationLevel > 0) {
					// create group
					XmlNode tmpNode = currentNode;
					currentNode = document.CreateElement ("g");

					// create and insert appropriate bullet
					XmlNode bullet;
					if (indentationLevel % 2 == 1) {
						bullet = document.CreateElement ("circle");

						a = document.CreateAttribute ("cx");
						a.Value = Convert.ToString (X + indent (-1 + 0.5));
						bullet.Attributes.Append (a);

						a = document.CreateAttribute ("cy");
						a.Value = Convert.ToString (Y + FontSize * 3 / 4);
						bullet.Attributes.Append (a);

						a = document.CreateAttribute ("r");
						a.Value = Convert.ToString (FontSize / 4);
						bullet.Attributes.Append (a);

						a = document.CreateAttribute ("fill");
						a.Value = color;
						bullet.Attributes.Append (a);
					} else {
						bullet = document.CreateElement ("rect");

						a = document.CreateAttribute ("x");
						a.Value = Convert.ToString (X + indent (-1 + 0.25));
						bullet.Attributes.Append (a);

						a = document.CreateAttribute ("y");
						a.Value = Convert.ToString (Y + FontSize * 4 / 8);
						bullet.Attributes.Append (a);

						a = document.CreateAttribute ("width");
						a.Value = Convert.ToString (FontSize / 2);
						bullet.Attributes.Append (a);

						a = document.CreateAttribute ("height");
						a.Value = Convert.ToString (FontSize / 2);
						bullet.Attributes.Append (a);

						a = document.CreateAttribute ("fill");
						a.Value = color;
						bullet.Attributes.Append (a);
					}

					currentNode.AppendChild (bullet);

					// insert text
					currentNode.AppendChild (tmpNode);
				}

				return currentNode;
			}

			int indent (double offset)
			{
				return Convert.ToInt32 ((((double)indentationLevel) * 2 + offset) * FontSize);
			}

			int ParseIndent (int indent)
			{
				return indent / FontSize / 2;
			}

			public override bool Equals (object obj)
			{
				if (!(obj is TextElement))
					return false;
				TextElement tmp = (TextElement)obj;
				if (text != tmp.text)
					return false;
				if (indentationLevel != tmp.indentationLevel)
					return false;
				if (size != tmp.size)
					return false;
				if (strong != tmp.strong)
					return false;
				if (color != tmp.color)
					return false;
				if (x != tmp.x)
					return false;
				if (y != tmp.y)
					return false;

				return true;
			}
		}
	}
}

