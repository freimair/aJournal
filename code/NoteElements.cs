using System;
using System.IO;
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
				case "image":
					return new ImageElement (node);
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

		public class ImageElement : NoteElement
		{
			int x, y, width, height;
			string type, image;

			public int X {
				get { return x;}
				set { x = value;}
			}

			public int Y {
				get { return y;}
				set { y = value;}
			}

			public int Width {
				get { return width;}
				set { width = value;}
			}

			public int Height {
				get { return height;}
				set{ height = value;}
			}

			public string Image {
				get { return image;}
			}

			public void LoadFromFile (string path)
			{
				image = Convert.ToBase64String (File.ReadAllBytes (path));
				type = "image/" + path.Substring (path.LastIndexOf (".") + 1);
			}

			public ImageElement ()
			{
			}

			public ImageElement (XmlNode node)
			{
				foreach (XmlAttribute current in node.Attributes) {
					switch (current.Name) {
					case "x":
						X = Convert.ToInt32 (current.Value);
						break;
					case "y":
						Y = Convert.ToInt32 (current.Value);
						break;
					case "width":
						Width = Convert.ToInt32 (current.Value);
						break;
					case "height":
						Height = Convert.ToInt32 (current.Value);
						break;
					case "xlink:href":
						type = current.Value.Substring (5, 9);
						image = current.Value.Substring (22);
						break;
					}
				}
			}

			public override XmlNode Find (XmlNode root)
			{
				return root.SelectSingleNode ("/svg/image[@x='" + X + "' and @y='" + Y + "' and @width='" + Width + "' and @height='" + Height + "']");
			}

			/**
			 * <image x="240" y="0" width="240" height="150" xlink:href="data:image/jpg;base64,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"/>
			 */
			public override XmlNode ToXml (XmlDocument document)
			{
				XmlNode currentNode = document.CreateElement ("image");

				XmlAttribute a = document.CreateAttribute ("x");
				a.Value = Convert.ToString (X);
				currentNode.Attributes.Append (a);

				a = document.CreateAttribute ("y");
				a.Value = Convert.ToString (Y);
				currentNode.Attributes.Append (a);

				a = document.CreateAttribute ("width");
				a.Value = Convert.ToString (Width);
				currentNode.Attributes.Append (a);

				a = document.CreateAttribute ("height");
				a.Value = Convert.ToString (Height);
				currentNode.Attributes.Append (a);

				a = document.CreateAttribute ("xlink", "href", "http://www.w3.org/1999/xlink");
				a.Value = "data:" + type + ";base64," + image;
				currentNode.Attributes.Append (a);

				return currentNode;
			}

			public override bool Equals (object obj)
			{
				if (!(obj is ImageElement))
					return false;
				ImageElement tmp = (ImageElement)obj;
				if (x != tmp.x)
					return false;
				if (y != tmp.y)
					return false;
				if (width != tmp.width)
					return false;
				if (height != tmp.height)
					return false;
				if (type != tmp.type)
					return false;
				if (image != tmp.image)
					return false;

				return true;
			}
		}
	}
}

