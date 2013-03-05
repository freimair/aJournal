using System;
using Gnome;
using Gtk;
using Gdk;

namespace code
{
	public class aJournal
	{
		TreeView myTreeView;

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
			// and a toggle button to hide the treeview below
			ToggleToolButton showTagTreeButton = new ToggleToolButton (Gtk.Stock.Index);
			showTagTreeButton.TooltipText = "toggle the taglist visibility";
			showTagTreeButton.Active = true;
			showTagTreeButton.Clicked += ShowTagTreeButton_Clicked;
			myToolbar.Insert (showTagTreeButton, 0);
			// insert the toolbar into the layout
			myHBox.Add (myToolbar);

			// add a column-like layout into the second row
			HBox myVBox = new HBox (false, 0);
			myHBox.Add (myVBox);

			// add an empty treeview to the first column
			myTreeView = new TreeView ();
			myVBox.Add (myTreeView);
			// add a canvas to the second column
			Canvas myCanvas = new Canvas ();
			myVBox.Add (myCanvas);

			win.ShowAll ();
		}

		/**
		 * callback for toggeling the tagtree visibility
		 */
		void ShowTagTreeButton_Clicked (object obj, EventArgs args)
		{
			myTreeView.Visible = ((ToggleToolButton)obj).Active;
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

