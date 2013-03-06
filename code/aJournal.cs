using System;
using System.Collections;
using Gnome;
using Gtk;
using Gdk;

namespace code
{
	public class aJournal
	{
		TreeView myTreeView;
		CanvasRE selection;
		Canvas myCanvas;
		CanvasLine currentStroke;
		ArrayList currentStrokePoints;

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

			// draw a filled rectangle to represent drawing area
			CanvasRE item = new CanvasRect (myCanvas.Root ());
			item.FillColor = "white";
			item.OutlineColor = "black";
			item.X1 = 0;
			item.Y1 = 0;
			item.X2 = 299;
			item.Y2 = 299;

			// add mouse trackers
			item.CanvasEvent += new Gnome.CanvasEventHandler (MyCanvas_Event);
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

		/**
		 * callback for handling events from canvas drawing area
		 */
		void MyCanvas_Event (object obj, Gnome.CanvasEventArgs args)
		{
			EventButton ev = new EventButton (args.Event.Handle);

			switch (ev.Type) {
			case EventType.ButtonPress:
				MyCanvas_MouseDown (obj, ev);
				break;
			case EventType.MotionNotify:
				MyCanvas_MouseMove (obj, ev);
				break;
			case EventType.ButtonRelease:
				MyCanvas_MouseUp (obj, ev);
				break;
			}
		}

		/**
		 * callback for mousedown in canvas
		 */
		void MyCanvas_MouseDown (object obj, EventButton args)
		{
			unselect ();
			currentStroke = new CanvasLine (myCanvas.Root ());
			currentStroke.WidthUnits = 2;
			currentStroke.CanvasEvent += new Gnome.CanvasEventHandler (Line_Event);
			currentStrokePoints = new ArrayList ();
			currentStrokePoints.Add ((double)args.X);
			currentStrokePoints.Add ((double)args.Y);
		}

		/**
		 * callback for mouse move in canvas
		 */
		void MyCanvas_MouseMove (object obj, EventButton args)
		{
			try {
				currentStrokePoints.Add (args.X);
				currentStrokePoints.Add (args.Y);
				currentStroke.Points = new CanvasPoints (currentStrokePoints.ToArray (typeof(double)) as double[]);
			} catch (NullReferenceException) {
				// in case there was no line started
			}
		}

		/**
		 * callback for mouseup in canvas
		 */
		void MyCanvas_MouseUp (object obj, EventButton args)
		{
			currentStrokePoints.Add (args.X);
			currentStrokePoints.Add (args.Y);
			currentStroke.Points = new CanvasPoints (currentStrokePoints.ToArray (typeof(double)) as double[]);
			currentStroke = null;
			currentStrokePoints = null;
		}

		/**
		 * callback for strokes in canvas
		 */
		void Line_Event (object obj, Gnome.CanvasEventArgs args)
		{
			EventButton ev = new EventButton (args.Event.Handle);

			switch (ev.Type) {
			case EventType.ButtonPress:
				Line_MouseDown (obj, ev);
				break;
			}
		}

		/**
		 * callback for mousedown of stroke
		 */
		void Line_MouseDown (object obj, EventButton args)
		{

			switch (args.Button) {
			case 1: // left mouse button selects the line
				select ((CanvasLine)obj);
				break;
			case 3:	// right mouse button deletes the line
				((CanvasLine)obj).Destroy ();
				break;
			}
		}

		void Window_Delete (object obj, DeleteEventArgs args)
		{
			Application.Quit ();
		}

		/**
		 * select one item by placing a gray box around it
		 * TODO offer various selection methods
		 */
		public void select (CanvasItem item)
		{
			double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
			item.GetBounds (out x1, out y1, out x2, out y2);

			// draw a filled rectangle to represent the selection
			selection = new CanvasRect (myCanvas.Root ());

			// lower to bottom
			selection.LowerToBottom ();
			// and raise just above basic rectangle
			selection.Raise (1);

			// set fill and stroke
			selection.FillColor = "gray";
			selection.OutlineColor = "black";

			// position
			selection.X1 = x1;
			selection.Y1 = y1;
			selection.X2 = x2;
			selection.Y2 = y2;
		}

		/**
		 * unselect all
		 */
		public void unselect ()
		{
			try {
				selection.Destroy ();
				selection = null;
			} catch (NullReferenceException) {
				// in case nothing is selected
			}
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

