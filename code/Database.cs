using System;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;

namespace backend
{
	public class Database
	{
		IDbConnection dbcon;
		public static string connectionString = "";

		public Database ()
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
	}
}