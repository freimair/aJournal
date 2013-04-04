using System;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;

namespace backend
{
	public class Database
	{
		static Database instance;
		IDbConnection dbcon;
		public static string connectionString = "";

		static Database Instance {
			get {
				if (null == instance)
					instance = new Database ();
				return instance;
			}
		}

		Database ()
		{
			if ("".Equals (connectionString))
				connectionString = "URI=file:" + Environment.GetFolderPath (Environment.SpecialFolder.Personal) + "/.aJournal/aJournal.db";
			dbcon = (IDbConnection)new SqliteConnection (connectionString);
			dbcon.Open ();
		}

		~Database ()
		{
			dbcon.Close ();
			dbcon = null;
		}

		public static void Execute (string sql)
		{
			IDbCommand dbcmd = Instance.dbcon.CreateCommand ();
			dbcmd.CommandText = sql;
			dbcmd.ExecuteNonQuery ();
			dbcmd.Dispose ();
			dbcmd = null;
		}

		public static IDataReader QueryInit (string sql)
		{
			IDbCommand dbcmd = Instance.dbcon.CreateCommand ();
			dbcmd.CommandText = sql;

			IDataReader reader = dbcmd.ExecuteReader ();

			dbcmd.Dispose ();
			dbcmd = null;

			return reader;
		}

		public static void QueryCleanup (IDataReader reader)
		{
			// clean up
			try {
				reader.Close ();
			} catch (NullReferenceException) {
			} finally {
				reader = null;
			}
		}
	}
}