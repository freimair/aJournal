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
			public static Dictionary<string, Tag> tagCache = new Dictionary<string, Tag> ();

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
				} catch (SqliteException) {
					// in case no database exists we return an empty list
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
					name = reader.GetString (1);
				} finally {
					Database.QueryCleanup (reader);
				}
			}

			Tag (string name)
			{
				this.name = name;
				Persist ();
			}

			void Persist ()
			{
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
						Database.Execute ("CREATE TABLE tags (" +
							"tag_id INTEGER PRIMARY KEY ASC," +
							"name varchar(255)" +
							")"
						);
						Persist ();
						break;
					}
				} finally {
					Database.QueryCleanup (reader);
				}
			}

			public void Remove ()
			{
				try {
					Database.Execute ("DELETE FROM tags WHERE tag_id='" + myId + "'");
				} catch (Exception) {
				} finally {
					tagCache.Remove (name);
				}
			}

			string name;

			public string Name {
				get {
					return name;
				}
				set {
					string oldname = name;
					IDataReader reader = Database.QueryInit ("SELECT name FROM tags WHERE name LIKE '" + oldname + "%'");
					while (reader.Read ()) {
						string current = reader.GetString (0);
						string renamed = reader.GetString (0).Replace (oldname, value);

						Database.Execute ("UPDATE tags SET name='" + renamed + "' WHERE name='" + current + "'");
						Tag currentTag = tagCache [current];
						tagCache.Remove (current);
						currentTag.name = renamed;
						tagCache.Add (renamed, currentTag);
					}
					Database.QueryCleanup (reader);
				}
			}

			public Tag Parent {
				get { return tagCache [Name.Substring (0, Name.LastIndexOf ("."))]; }
				set {
					if (Name.Contains ("."))
						Name = value.Name + "." + Name.Substring (Name.LastIndexOf ("."));
					else
						Name = value.Name + "." + Name;
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

