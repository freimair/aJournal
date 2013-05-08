using System;
using System.IO;
using System.Xml;
using System.Data;
using System.Collections.Generic;
using backend.Tags;

//TODO get rid of sqlite specificas
using Mono.Data.Sqlite;

namespace backend
{
	namespace NoteElements
	{
		/**
	 * some Drawable
	 */
		public abstract class NoteElement
		{
			protected long myId;

			public long X {
				get;
				set;
			}

			public long Y {
				get;
				set;
			}

			public string Time {
				get;
				set;
			}

			public string Color {
				get;
				set;
			}

			protected NoteElement ()
			{
			}

			#region database roundtrip
			public static List<NoteElement> GetElements (ElementFilter filter)
			{
				string sql = "SELECT elements.element_id, type FROM elements " +
					"LEFT JOIN element_tag_mapping ON elements.element_id=element_tag_mapping.element_id " +
					"LEFT JOIN tags ON element_tag_mapping.tag_id=tags.tag_id";

				// display only items newer then specified by filter
				sql += " WHERE elements.timestamp >= '" + filter.NewerAs.ToString ("yyyMMddHHmmssff") + "'";

				// include untagged items
				sql += " AND (element_tag_mapping.tag_id IS NULL";

				if (0 < filter.Tags.Count)
					foreach (Tag current in filter.Tags)
						sql += " OR tags.name ='" + current.Name + "'";
				sql += ")";

				List<NoteElement> result = new List<NoteElement> ();
				IDataReader reader = null;
				try {
					reader = Database.QueryInit (sql);
					while (reader.Read())
						result.Add (NoteElement.RecreateFromDb (reader.GetInt64 (0), reader.GetString (1)));
				} catch (Exception) {

				} finally {
					Database.QueryCleanup (reader);
				}

				return result;
			}

			public static List<NoteElement> Elements {
				get {
					List<NoteElement> result = new List<NoteElement> ();
					IDataReader reader = null;
					try {
						reader = Database.QueryInit ("SELECT element_id, type FROM elements");
						while (reader.Read())
							result.Add (NoteElement.RecreateFromDb (reader.GetInt64 (0), reader.GetString (1)));
					} catch (Exception) {

					} finally {
						Database.QueryCleanup (reader);
					}

					return result;
				}
			}

			static NoteElement RecreateFromDb (long id, string type)
			{
				return (NoteElement)Activator.CreateInstance (Type.GetType (type), id);
			}

			/**
			 * recreate a NoteElement from database
			 */
			protected NoteElement (long id)
			{
				myId = id;

				// fill x, y, timestamp, color
				IDataReader reader = Database.QueryInit ("SELECT x, y, timestamp, color FROM elements WHERE element_id='" + id + "'");
				reader.Read ();
				X = reader.GetInt64 (0);
				Y = reader.GetInt64 (1);
				Time = reader.GetString (2);
				Color = reader.GetString (3);
				Database.QueryCleanup (reader);

				// TODO fill tags
			}

			public void Persist ()
			{
				PersistNoteElement ();
				PersistElementDetails ();
			}

			void PersistNoteElement ()
			{
				if (0 >= myId) {
					// we have a new element here
					// TODO mutex!
					IDataReader reader = null;

					try {
						if (null == Time)
							Time = DateTime.Now.ToString ("yyyMMddHHmmssff");
						Color = "red";
						Database.Execute ("INSERT INTO elements (type, x, y, timestamp, color) VALUES ('" + this.GetType () + "', '" + X + "', '" + Y + "', '" + Time + "' , '" + Color + "')");
						reader = Database.QueryInit ("SELECT MAX(element_id) FROM elements");
						reader.Read ();
						myId = reader.GetInt64 (0);
						Database.QueryCleanup (reader);
					} catch (Exception) {
						if (null != reader)
							Database.QueryCleanup (reader);
						// create table elements
						Database.Execute ("CREATE TABLE elements (" +
							"element_id INTEGER PRIMARY KEY ASC," +
							"type varchar(255)," +
							"x int," +
							"y int," +
							"timestamp varchar(15)," +
							"color varchar(10)" +
							")"
						);
						PersistNoteElement ();
					}
				} else {
					// we may have updated values
					// TODO do we need to update the timestamp?
					Database.Execute ("UPDATE elements SET x='" + X + "', y='" + Y + "', color='" + Color + "' WHERE element_id='" + myId + "'");
				}
			}

			protected abstract void PersistElementDetails ();

			public void Remove ()
			{
				RemoveElementDetails ();
				RemoveNoteElement ();
			}

			private void RemoveNoteElement ()
			{
				Database.Execute ("DELETE FROM elements WHERE element_id='" + myId + "'");
			}

			protected abstract void RemoveElementDetails ();
			#endregion

			#region tags
			public List<Tag> Tags {
				get { return Tag.TagsFor (myId);}
				set {
					ClearTags ();
					foreach (Tag current in value)
						AddTag (current);
				}
			}

			public void AddTag (Tag tag)
			{
				tag.AssignTo (myId);
			}

			public void RemoveTag (Tag tag)
			{
				tag.RemoveFrom (myId);
			}

			public void ClearTags ()
			{
				foreach (Tag current in Tags)
					RemoveTag (current);
			}

			static List<long> GetIds (List<NoteElement> elements)
			{
				List<long> ids = new List<long> ();
				foreach (NoteElement element in elements)
					ids.Add (element.myId);

				return ids;
			}

			public static List<Tag> AllTagsFor (List<NoteElement> elements)
			{
				return Tag.AllTagsFor (GetIds (elements));
			}

			public static List<Tag> CommonTagsFor (List<NoteElement> elements)
			{
				return Tag.CommonTagsFor (GetIds (elements));
			}
			#endregion

			#region svg roundtrip
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

			public abstract XmlNode ToXml (XmlDocument document);
			#endregion

			#region comparison
			public override bool Equals (object obj)
			{
				if (!(obj is NoteElement))
					return false;
				if (myId != ((NoteElement)obj).myId)
					return false;

				return true;
			}

			public override int GetHashCode ()
			{
				return Convert.ToInt32 (myId);
			}
			#endregion
		}

		public class PolylineElement : NoteElement
		{
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

			#region database roundtrip
			/**
			 * recreate a PolylineElement from database
			 */
			public PolylineElement (long id) : base(id)
			{
				// fill x, y, timestamp, color
				IDataReader reader = Database.QueryInit ("SELECT width, points FROM polyline_elements WHERE element_id='" + id + "'");
				reader.Read ();
				strength = reader.GetInt32 (0);
				string pointsstring = reader.GetString (1);
				if (!"".Equals (pointsstring)) {
					String[] values = pointsstring.Split (new char[] {' ', ','});

					foreach (String currentValue in values)
						points.Add (Convert.ToInt32 (currentValue));
				}
				Database.QueryCleanup (reader);
			}

			protected override void PersistElementDetails ()
			{
				try {
					// we have a new element here
					Database.Execute ("INSERT INTO polyline_elements (element_id, width, points) VALUES ('" + myId + "', '" + strength + "', '" + GetSVGPointList () + "')");
				} catch (SqliteException e) {
					switch (e.ErrorCode) {
					case SQLiteErrorCode.Constraint:
						Database.Execute ("UPDATE polyline_elements SET width='" + strength + "', points='" + GetSVGPointList () + "' WHERE element_id='" + myId + "'");
						break;
					case SQLiteErrorCode.Error:
						Database.Execute ("CREATE TABLE polyline_elements (" +
							"element_id INTEGER PRIMARY KEY," +
							"width INTEGER," +
							"points TEXT" +
							")"
						);
						PersistElementDetails ();
						break;
					}
				}
			}

			protected override void RemoveElementDetails ()
			{
				Database.Execute ("DELETE FROM polyline_elements WHERE element_id='" + myId + "'");
			}
			#endregion

			#region svg roundtrip
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

			public override XmlNode ToXml (XmlDocument document)
			{
				XmlAttribute fillAttribute = document.CreateAttribute ("fill");
				fillAttribute.Value = "none";

				XmlAttribute strokeAttribute = document.CreateAttribute ("stroke");
				strokeAttribute.Value = Color;

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
			#endregion
		}

		public class TextElement : NoteElement
		{
			string text = "";
			int indentationLevel = 0;
			uint style = 0; // 0 - p, 1 - h3, 2 - h2, 3 - h1 for example

			public string Text {
				get{ return text;}
				set{ text = value;}
			}

			public int IndentationLevel {
				get{ return indentationLevel;}
				set{ indentationLevel = value;}
			}

			public uint Style {
				get{ return style;}
				set {
					style = value;
				}
			}

			public TextElement ()
			{
			}

			#region database roundtrip
			/**
			 * recreate a TextElement from database
			 */
			public TextElement (long id) : base(id)
			{
				// fill x, y, timestamp, color
				IDataReader reader = Database.QueryInit ("SELECT size, indentation_level, text FROM text_elements WHERE element_id='" + id + "'");
				reader.Read ();
				Style = Convert.ToUInt32 (reader.GetInt32 (0));
				IndentationLevel = reader.GetInt32 (1);
				Text = reader.GetString (2);
				Database.QueryCleanup (reader);
			}

			protected override void PersistElementDetails ()
			{
				try {
					// we have a new element here
					Database.Execute ("INSERT INTO text_elements " +
						"(element_id, size, indentation_level, text) VALUES " +
						"('" + myId + "', '" + Style + "', '" + IndentationLevel + "', '" + Text + "')"
					);
				} catch (SqliteException e) {
					switch (e.ErrorCode) {
					case SQLiteErrorCode.Constraint:
						Database.Execute ("UPDATE text_elements SET " +
							"size='" + Style +
							"' WHERE element_id='" + myId + "'"
						);
						break;
					case SQLiteErrorCode.Error:
						Database.Execute ("CREATE TABLE text_elements (" +
							"element_id INTEGER PRIMARY KEY," +
							"size INTEGER," +
							"indentation_level INTEGER," +
							"text TEXT" +
							")"
						);
						PersistElementDetails ();
						break;
					}
				}
			}

			protected override void RemoveElementDetails ()
			{
				Database.Execute ("DELETE FROM text_elements WHERE element_id='" + myId + "'");
			}
			#endregion

			#region svg roundtrip
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
						Style = GetStyleFromSvgEncoding (Convert.ToInt32 (current.Value)); // TODO beware of the magic number
						break;
					}

				foreach (XmlAttribute current in textNode.Attributes) {
					switch (current.Name) {
					case "x":
						X = Convert.ToInt32 (current.Value);
						break;
					case "y":
						Y = Convert.ToInt32 (current.Value) - GetSvgFontSize (Style); // TODO beware of the magic number
						break;
					case "fill":
						Color = current.Value;
						break;
					case "font-weight":
						Style = GetStyleFromSvgEncoding (GetSvgFontSize (Style), current.Value);
						break;
					case "transform":
						int indent = Convert.ToInt32 (current.Value.Replace ("translate(", "").Replace (")", ""));
						IndentationLevel = ParseIndent (indent);
						break;
					}
				}
				Text = textNode.InnerText;
			}

			uint GetStyleFromSvgEncoding (int fontSize)
			{
				return GetStyleFromSvgEncoding (fontSize, "normal");
			}

			uint GetStyleFromSvgEncoding (int fontSize, string fontWeight)
			{
				if (fontWeight.Equals ("bold"))
					return Convert.ToUInt32 (fontSize / 10);
				return 0;
			}

			int GetSvgFontSize (uint style)
			{
				return Convert.ToInt32 (style > 0 ? style * 10 : 10);
			}

			string GetSvgFontWeight (uint style)
			{
				return style >= 0 ? "bold" : "normal";
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
				a.Value = Convert.ToString (Y + GetSvgFontSize (Style));
				currentNode.Attributes.Append (a);

				a = document.CreateAttribute ("fill");
				a.Value = Color;
				currentNode.Attributes.Append (a);

				a = document.CreateAttribute ("font-size");
				a.Value = Convert.ToString (GetSvgFontSize (Style));
				currentNode.Attributes.Append (a);

				a = document.CreateAttribute ("font-weight");
				a.Value = GetSvgFontWeight (Style);
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
						a.Value = Convert.ToString (Y + GetSvgFontSize (Style) * 3 / 4);
						bullet.Attributes.Append (a);

						a = document.CreateAttribute ("r");
						a.Value = Convert.ToString (GetSvgFontSize (Style) / 4);
						bullet.Attributes.Append (a);

						a = document.CreateAttribute ("fill");
						a.Value = Color;
						bullet.Attributes.Append (a);
					} else {
						bullet = document.CreateElement ("rect");

						a = document.CreateAttribute ("x");
						a.Value = Convert.ToString (X + indent (-1 + 0.25));
						bullet.Attributes.Append (a);

						a = document.CreateAttribute ("y");
						a.Value = Convert.ToString (Y + GetSvgFontSize (Style) * 4 / 8);
						bullet.Attributes.Append (a);

						a = document.CreateAttribute ("width");
						a.Value = Convert.ToString (GetSvgFontSize (Style) / 2);
						bullet.Attributes.Append (a);

						a = document.CreateAttribute ("height");
						a.Value = Convert.ToString (GetSvgFontSize (Style) / 2);
						bullet.Attributes.Append (a);

						a = document.CreateAttribute ("fill");
						a.Value = Color;
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
				return Convert.ToInt32 ((((double)indentationLevel) * 2 + offset) * GetSvgFontSize (Style));
			}

			int ParseIndent (int indent)
			{
				return indent / GetSvgFontSize (Style) / 2;
			}
			#endregion
		}

		public class ImageElement : NoteElement
		{
			int width, height;
			string type, image;

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
				type = "image/" + path.Substring (path.LastIndexOf (".") + 1).ToLower ();
			}

			public ImageElement ()
			{
			}

			#region database roundtrip
			/**
			 * recreate an ImageElement from database
			 */
			public ImageElement (long id) : base(id)
			{
				// fill x, y, timestamp, color
				IDataReader reader = Database.QueryInit ("SELECT width, height, type, image FROM image_elements WHERE element_id='" + id + "'");
				reader.Read ();
				Width = reader.GetInt32 (0);
				Height = reader.GetInt32 (1);
				type = reader.GetString (2);
				image = reader.GetString (3);
				Database.QueryCleanup (reader);
			}

			protected override void PersistElementDetails ()
			{
				try {
					// we have a new element here
					Database.Execute ("INSERT INTO image_elements " +
						"(element_id, width, height, type, image) VALUES " +
						"('" + myId + "', '" + Width + "', '" + Height + "', '" + type + "', '" + image + "')"
					);
				} catch (SqliteException e) {
					switch (e.ErrorCode) {
					case SQLiteErrorCode.Constraint:
						// do we need to update type and image?
						Database.Execute ("UPDATE image_elements SET " +
							"width='" + width +
							"', height='" + Height + 
							"' WHERE element_id='" + myId + "'"
						);
						break;
					case SQLiteErrorCode.Error:
						Database.Execute ("CREATE TABLE image_elements (" +
							"element_id INTEGER PRIMARY KEY," +
							"width INTEGER," +
							"height INTEGER," +
							"type VARCHAR(15)," +
							"image TEXT" +
							")"
						);
						PersistElementDetails ();
						break;
					}
				}
			}

			protected override void RemoveElementDetails ()
			{
				Database.Execute ("DELETE FROM image_elements WHERE element_id='" + myId + "'");
			}
			#endregion

			#region svg roundtrip
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
			#endregion
		}
	}
}

