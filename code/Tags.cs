using System;
using System.Xml;
using System.Collections.Generic;
using System.Data;
using System.Linq;

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

			public static List<Tag> AllTagsFor (List<long> element_ids)
			{
				List<Tag> result = new List<Tag> ();
				foreach (long element_id in element_ids)
					result.AddRange (TagsFor (element_id));
				return new List<Tag> (result.Distinct ());
			}

			public static List<Tag> CommonTagsFor (List<long> element_ids)
			{
				List<Tag> result = null;
				foreach (long element_id in element_ids) {
					if (null == result) {
						result = new List<Tag> ();
						result.AddRange (TagsFor (element_id));
					} else
						result = new List<Tag> (result.Intersect (TagsFor (element_id)));
				}
				return result;
			}

			public static List<Tag> TagsFor (long element_id)
			{
				List<Tag> result = new List<Tag> ();
				IDataReader reader = null;
				try {
					reader = Database.QueryInit ("SELECT tags.tag_id, name FROM tags INNER JOIN element_tag_mapping ON element_tag_mapping.tag_id=tags.tag_id WHERE element_id='" + element_id + "'");
					while (reader.Read()) {
						if (!tagCache.ContainsKey (reader.GetString (1)))
							tagCache.Add (reader.GetString (1), new Tag (reader.GetInt64 (0)));
						result.Add (tagCache [reader.GetString (1)]);
					}
				} catch (Exception) {
				} finally {
					Database.QueryCleanup (reader);
				}
				return result;
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

			public long myId;

			Tag (long id)
			{
				// TODO mutex!
				IDataReader reader = null;

				try {
					myId = id;
					reader = Database.QueryInit ("SELECT name FROM tags WHERE tag_id = '" + myId + "'");
					reader.Read ();
					name = reader.GetString (0);
				} finally {
					Database.QueryCleanup (reader);
				}
			}

			Tag (string name)
			{
				this.name = name;
				Persist ();
			}

			public void AssignTo (long element_id)
			{
				TagElementMapping.Link (element_id, myId);
			}

			public void RemoveFrom (long element_id)
			{
				TagElementMapping.Unlink (element_id, myId);
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
				get { 
					try {
						return tagCache [Name.Substring (0, Name.LastIndexOf ("."))];
					} catch (ArgumentOutOfRangeException) {
						// no "." in the tag name -> no parent
						return null;
					}
				}
				set {
					if (null != value) { // we remove the parent by assigning null - null breaks the code below
						if (Name.Contains ("."))
							Name = value.Name + "." + Name.Substring (Name.LastIndexOf ("."));
						else
							Name = value.Name + "." + Name;
					}
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

