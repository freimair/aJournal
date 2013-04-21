using System;
using System.Data;
using System.Collections.Generic;
using backend.NoteElements;
using System.Linq;

using Mono.Data.Sqlite;

namespace backend
{
	namespace Tags
	{
		public class TagElementMapping
		{
			public static void Link (long element, long tag)
			{
				try {
					Database.Execute ("INSERT INTO element_tag_mapping (element_id, tag_id) VALUES ('" + element + "', '" + tag + "')");
				} catch (Exception e) {
					InitDatabase ();
					Link (element, tag);
				}
			}

			public static void Unlink (long element, long tag)
			{
				try {
					Database.Execute ("DELETE FROM element_tag_mapping WHERE element_id='" + element + "' and tag_id = '" + tag + "'");
				} catch (Exception) {
				}
			}

			public static List<long> GetElements (List<long> tags)
			{
				List<long> result = null;

				foreach (int currentTag in tags) {
					if (null == result) {
						result = new List<long> ();
						result.AddRange (GetElements (currentTag));
					}
					result.Intersect (GetElements (currentTag));
				}
				return result;
			}

			public static List<long> GetAllTags (List<long> elements)
			{
				List<long> result = new List<long> ();

				foreach (int currentTag in elements)
					result.AddRange (GetTags (currentTag));

				return result;
			}

			public static List<long> GetSharedTags (List<long> elements)
			{
				List<long> result = null;

				foreach (int currentTag in elements) {
					if (null == result) {
						result = new List<long> ();
						result.AddRange (GetTags (currentTag));
					}
					result.Intersect (GetTags (currentTag));
				}
				return result;
			}

			public static List<long> GetElements (long tag)
			{
				return Get (tag, "tag_id", "element_id");
			}


			public static List<long> GetTags (long element)
			{
				return Get (element, "element_id", "tag_id");
			}

			static List<long> Get (long id, string field, string target)
			{
				List<long> result = new List<long> ();
				IDataReader reader = null;
				try {
					reader = Database.QueryInit ("SELECT " + target + " FROM element_tag_mapping WHERE " + field + "='" + id + "'");
					while (reader.Read())
						result.Add (reader.GetInt32 (0));
				} catch (Exception) {
				} finally {
					Database.QueryCleanup (reader);
				}
				return result;
			}

			static void InitDatabase ()
			{
				Database.Execute ("CREATE TABLE element_tag_mapping (" +
					"element_id INTEGER," +
					"tag_id INTEGER" +
					");"
				);
			}
		}
	}
}

