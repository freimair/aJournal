using System;
using System.Xml;
using System.Collections.Generic;
using System.Data;

//TODO get rid of sqlite specificas
using Mono.Data.Sqlite;

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
					Tag newTag = new Tag (path);

					// find parent
					if (path.Contains ("."))
						Create (path.Substring (0, path.LastIndexOf (".")));

					tagCache.Add (path, newTag);
					return newTag;
				}
			}

			public static List<Tag> Tags {
				get{ return Get ();}
			}

			public static List<Tag> Get ()
			{
				IDataReader reader = null;
				List<Tag> result = new List<Tag> ();
				try {
					reader = Database.QueryInit ("SELECT tag_id, name FROM tags"); // TODO where name like "bla%"
					while (reader.Read ()) {
						if (!tagCache.ContainsKey (reader.GetString (1)))
							tagCache.Add (reader.GetString (1), new Tag (reader.GetInt64 (0)));
						result.Add (tagCache [reader.GetString (1)]);
					}
				} catch (SqliteException e) {
					switch (e.ErrorCode) {
					case SQLiteErrorCode.Error:
						SetupDatabase ();
						break;
					}
				} finally {
					Database.QueryCleanup (reader);
				}
				return result;
			}

			public static Tag RecreateFromXml (XmlNode node)
			{
				return Create (node.FirstChild.Value);
			}

			long myId;

			Tag (long id)
			{
				// TODO mutex!
				IDataReader reader = null;

				try {
					reader = Database.QueryInit ("SELECT tag_id, name FROM tags");
					reader.Read ();
					myId = reader.GetInt64 (0);
					Name = reader.GetString (1);
				} finally {
					Database.QueryCleanup (reader);
				}
			}

			Tag (string name)
			{
				this.name = name;

				// TODO mutex!
				IDataReader reader = null;

				try {
					Database.Execute ("INSERT INTO tags (name) VALUES ('" + Name + "')");
					reader = Database.QueryInit ("SELECT MAX(tag_id) FROM tags");
					reader.Read ();
					myId = reader.GetInt64 (0);
				} catch (SqliteException e) {
					switch (e.ErrorCode) {
					case SQLiteErrorCode.Constraint:
						Database.Execute ("UPDATE tags SET name='" + Name + "' WHERE tag_id='" + myId + "'");
						break;
					case SQLiteErrorCode.Error:
						SetupDatabase ();
						Database.Execute ("INSERT INTO tags (name) VALUES ('" + Name + "')");
						break;
					}
				} finally {
					Database.QueryCleanup (reader);
				}
			}

			static void SetupDatabase ()
			{
				Database.Execute ("CREATE TABLE tags (" +
					"tag_id INTEGER PRIMARY KEY ASC," +
					"name varchar(255)" +
					")"
				);
			}

			string name;

			public string Name {
				get { return name; }
				set {
					tagCache.Remove (name);
					name = value;
					Database.Execute ("UPDATE tags SET name='" + name + "' WHERE tag_id='" + myId + "'");
					tagCache.Add (name, this);
				}
			}

			public Tag Parent {
				get { return tagCache [name.Substring (0, name.LastIndexOf ("."))]; }
				set {
					Tag parent = value;
					Name = parent.Name + "." + Name.Substring (Name.LastIndexOf (".") + 1);
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
				return Name;
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

