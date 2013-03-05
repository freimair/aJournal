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

			// add row-like layout
			VBox myHBox = new VBox (false, 0);
			win.Add (myHBox);

			// create a toolbar
			Toolbar myToolbar = new Toolbar ();
			// with a very simple button
			ToolButton myToolButton = new ToolButton (Gtk.Stock.About);
			myToolbar.Insert (myToolButton, 0);
			// insert the toolbar into the layout
			myHBox.Add (myToolbar);

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

