using System;
using Gnome;
using Gtk;
using Gdk;

namespace code
{
	public class aJournal
	{
		TreeView myTreeView;

		Canvas myCanvas;

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
			// add zoom buttons
			ToolButton zoomInButton = new ToolButton (Gtk.Stock.ZoomIn);
			zoomInButton.TooltipText = "zoom in";
			zoomInButton.Clicked += ZoomInButton_Clicked;
			myToolbar.Insert (zoomInButton, 1);
			ToolButton zoomOutButton = new ToolButton (Gtk.Stock.ZoomOut);
			zoomOutButton.TooltipText = "zoom out";
			zoomOutButton.Clicked += ZoomOutButton_Clicked;
			myToolbar.Insert (zoomOutButton, 2);

			// insert the toolbar into the layout
			myHBox.PackStart (myToolbar, false, false, 0);

			// add a column-like layout into the second row
			HBox myVBox = new HBox (false, 0);
			myHBox.Add (myVBox);

			// add an empty treeview to the first column
			myTreeView = new TreeView ();
			myVBox.Add (myTreeView);

			// add a canvas to the second column
			myCanvas = new Canvas ();
			// TODO find out why this somehow centers the axis origin.
			myCanvas.SetScrollRegion (0.0, 0.0, (double)300, (double)300);

			myVBox.Add (myCanvas);
			win.ShowAll ();

			// draw a filled rectangle
			CanvasRE item = new CanvasRect (myCanvas.Root ());
			item.FillColor = "white";
			item.OutlineColor = "black";
			item.X1 = 0;
			item.Y1 = 0;
			item.X2 = 299;
			item.Y2 = 299;
		}

		/**
		 * callback for toggeling the tagtree visibility
		 */
		void ShowTagTreeButton_Clicked (object obj, EventArgs args)
		{
			myTreeView.Visible = ((ToggleToolButton)obj).Active;
		}

		/**
		 * callback for zooming in
		 */
		void ZoomInButton_Clicked (object obj, EventArgs args)
		{
			myCanvas.PixelsPerUnit *= (double)5 / 4;
		}

		/**
		 * callback for zooming out
		 */
		void ZoomOutButton_Clicked (object obj, EventArgs args)
		{
			myCanvas.PixelsPerUnit *= (double)4 / 5;
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

