using System;
using Gnome;
using Gtk;
using Gdk;

namespace code
{
	public class aJournal
	{
		public aJournal ()
		{
			Gtk.Window win = new Gtk.Window ("aJournal");
			win.SetSizeRequest(600, 600);
			win.DeleteEvent += new DeleteEventHandler (Window_Delete);

			win.ShowAll ();
		}

		void Window_Delete (object obj, DeleteEventArgs args)
		{
			Application.Quit ();
		}

		public static int Main (string[] args)
		{
			Application.Init ();

			new aJournal ();

			Application.Run ();
			return 0;
		}
	}
}

