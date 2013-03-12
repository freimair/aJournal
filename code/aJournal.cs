using System;
using System.Collections;
using System.Collections.Generic;
using Gnome;
using Gtk;
using Gdk;

namespace code
{
	public class aJournal
	{
		TreeView myTreeView;
		Canvas myCanvas;
		CanvasLine currentStroke;
		ArrayList currentStrokePoints;
		const int canvasWidth = 1500, canvasHeight = 1500;
		Gtk.Window win;

		// TODO find a way to read elements back from the canvas itself
		List<CanvasItem> elements = new List<CanvasItem> ();

		public aJournal ()
		{
			win = new Gtk.Window ("aJournal");
			win.SetSizeRequest (600, 600);
			win.DeleteEvent += new DeleteEventHandler (Window_Delete);

			// add row-like layout
			VBox toolbarContentLayout = new VBox (false, 0);
			win.Add (toolbarContentLayout);

			// create a toolbar
			Toolbar myToolbar = new Toolbar ();
			// with a very simple button
			ToolButton myToolButton = new ToolButton (Gtk.Stock.About);
			myToolbar.Insert (myToolButton, 0);
			// and a toggle button to hide the treeview below
			ToggleToolButton showTagTreeButton = new ToggleToolButton (Gtk.Stock.Index);
			showTagTreeButton.TooltipText = "toggle the taglist visibility";
			showTagTreeButton.Active = false;
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
			ToolButton zoomFitButton = new ToolButton (Gtk.Stock.ZoomFit);
			zoomFitButton.TooltipText = "zoom fit";
			zoomFitButton.Clicked += ZoomFitButton_Clicked;
			myToolbar.Insert (zoomFitButton, 3);

			// insert the toolbar into the layout
			toolbarContentLayout.PackStart (myToolbar, false, false, 0);

			// add a column-like layout into the second row
			HBox taglistContentLayout = new HBox (false, 0);
			toolbarContentLayout.Add (taglistContentLayout);

			// add an empty treeview to the first column
			myTreeView = new TreeView ();
			taglistContentLayout.Add (myTreeView);
			
			// add canvas container
			ScrolledWindow myScrolledNotesContainer = new ScrolledWindow ();
			myScrolledNotesContainer.SetPolicy (Gtk.PolicyType.Automatic, Gtk.PolicyType.Always);
			taglistContentLayout.Add (myScrolledNotesContainer);
			
			Viewport myViewport = new Viewport ();
			myScrolledNotesContainer.Add (myViewport);

			VBox myNotesContainer = new VBox (false, 0);
			myViewport.Add (myNotesContainer);

			// add a canvas to the second column
			myCanvas = Canvas.NewAa ();
			// TODO find out why this somehow centers the axis origin.
			myCanvas.SetScrollRegion (0.0, 0.0, canvasWidth, canvasHeight);

			myNotesContainer.Add (myCanvas);

			// indicate that there will somewhen be the option to add another notes area
			Button addNotesButton = new Button (Gtk.Stock.Add);
			myNotesContainer.Add (addNotesButton);
			win.ShowAll ();

			myTreeView.Visible = false;

			MyCanvas_Fit (400);

			// draw a filled rectangle to represent drawing area
			CanvasRE item = new CanvasRect (myCanvas.Root ());
			item.FillColor = "white";
			item.OutlineColor = "black";
			item.X1 = 0;
			item.Y1 = 0;
			item.X2 = canvasWidth;
			item.Y2 = canvasHeight;

			// add mouse trackers
			item.CanvasEvent += new Gnome.CanvasEventHandler (MyCanvas_Event);
		}

		/**
		 * change the canvas scale
		 */
		void MyCanvas_Scale (double factor)
		{
			int width = (int)Math.Round (myCanvas.PixelsPerUnit * factor * canvasWidth);

			MyCanvas_Fit (width);
		}

		/**
		 * fit the canvas scale to a certain width
		 */
		void MyCanvas_Fit (int width)
		{
			myCanvas.PixelsPerUnit = ((double)width) / canvasWidth;

			myCanvas.SetSizeRequest (width, (int)Math.Round (canvasHeight * myCanvas.PixelsPerUnit));
			myCanvas.UpdateNow ();
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
			MyCanvas_Scale ((double)10 / 9);
		}

		/**
		 * callback for zooming out
		 */
		void ZoomOutButton_Clicked (object obj, EventArgs args)
		{
			MyCanvas_Scale ((double)9 / 10);
		}

		/**
		 * callback for zooming out
		 */
		void ZoomFitButton_Clicked (object obj, EventArgs args)
		{
			uint width, height;
			myCanvas.GetSize (out width, out height);
			MyCanvas_Fit ((int)width);
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
			if (1 == args.Button)
				StartStroke (args.X, args.Y);
			else if (3 == args.Button)
				StartSelection (args.X, args.Y);
		}

		void StartStroke (double x, double y)
		{
			unselect ();
			currentStroke = new CanvasLine (myCanvas.Root ());
			currentStroke.WidthUnits = 2;
			currentStroke.FillColor = "black";
			currentStroke.CanvasEvent += new Gnome.CanvasEventHandler (Line_Event);
			currentStrokePoints = new ArrayList ();
			currentStrokePoints.Add (x);
			currentStrokePoints.Add (y);
		}

		/**
		 * callback for mouse move in canvas
		 */
		void MyCanvas_MouseMove (object obj, EventButton args)
		{
			ContinueStroke (args.X, args.Y);
			ContinueSelection (args.X, args.Y);
		}

		void ContinueStroke (double x, double y)
		{
			try {
				currentStrokePoints.Add (x);
				currentStrokePoints.Add (y);
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
			if (1 == args.Button)
				CompleteStroke (args.X, args.Y);
			else if (3 == args.Button)
				CompleteSelection (args.X, args.Y);
		}

		void CompleteStroke (double x, double y)
		{
			currentStrokePoints.Add (x);
			currentStrokePoints.Add (y);
			currentStroke.Points = new CanvasPoints (currentStrokePoints.ToArray (typeof(double)) as double[]);

			// add the final stroke to the list of elements
			elements.Add (currentStroke);

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

		bool selectionInProgress = false;
		CanvasRE selection;
		List<CanvasItem> selectedItems = new List<CanvasItem> ();
		bool move = false;
		double lastX, lastY;

		void StartSelection (double x, double y)
		{
			unselect ();
			selection = new CanvasRect (myCanvas.Root ());

			selection.X1 = x;
			selection.Y1 = y;
			selection.X2 = x;
			selection.Y2 = y;

			selection.FillColorRgba = 0x88888830; // 0xRRGGBBAA
			selection.OutlineColor = "black";

			selectionInProgress = true;
		}

		void ContinueSelection (double x, double y)
		{
			if (selectionInProgress) {
				selection.X2 = x;
				selection.Y2 = y;
			}
		}

		void CompleteSelection (double x, double y)
		{
			ContinueSelection (x, y);

			// enable key event recognition
			selection.GrabFocus ();

			selection.CanvasEvent += new Gnome.CanvasEventHandler (Selection_Event);

			selectionInProgress = false;

			//TODO find selected items
			//TODO find a way to read elements back from the canvas itself
//			foreach (CanvasItem current in myCanvas.AllChildren) {
			foreach (CanvasItem current in elements) {
				double x1, x2, y1, y2;
				current.GetBounds (out x1, out y1, out x2, out y2);
				if (selection.X1 < x1 && selection.X2 > x2 && selection.Y1 < y1 && selection.Y2 > y2)
					selectedItems.Add (current);
			}
		}

		/**
		 * select one item by placing a gray box around it
		 * TODO offer various selection methods
		 */
		public void select (CanvasItem item)
		{
			double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
			item.GetBounds (out x1, out y1, out x2, out y2);

			selectedItems.Add (item);

			// draw a filled rectangle to represent the selection
			selection = new CanvasRect (myCanvas.Root ());

			// set fill and stroke
			selection.FillColorRgba = 0x88888830; // 0xRRGGBBAA
			selection.OutlineColor = "black";

			// position
			selection.X1 = x1;
			selection.Y1 = y1;
			selection.X2 = x2;
			selection.Y2 = y2;

			// enable key event recognition
			selection.GrabFocus ();

			selection.CanvasEvent += new Gnome.CanvasEventHandler (Selection_Event);
		}

		/**
		 * unselect all
		 */
		public void unselect ()
		{
			try {
				selection.Destroy ();
				selection = null;
				selectedItems.Clear ();
			} catch (NullReferenceException) {
				// in case nothing is selected
			}
		}

		void Selection_Event (object obj, Gnome.CanvasEventArgs args)
		{
			EventButton ev = new EventButton (args.Event.Handle);
			EventKey key = new EventKey (args.Event.Handle);

			switch (ev.Type) {
			case EventType.ButtonPress:
				Selection_MouseDown (obj, ev);
				break;
			case EventType.MotionNotify:
				Selection_MouseMove (obj, ev);
				break;
			case EventType.ButtonRelease:
				Selection_MouseUp (obj, ev);
				break;
			case EventType.KeyPress:
				Selection_KeyPress (obj, key);
				break;
			}
		}

		void Selection_MouseDown (object obj, EventButton args)
		{

			switch (args.Button) {
			case 1: // left mouse button
				move = true;
				lastX = args.X;
				lastY = args.Y;
				break;
			case 3:	// right mouse button
				break;
			}
		}

		void Selection_MouseUp (object obj, EventButton args)
		{

			switch (args.Button) {
			case 1: // left mouse button
				move = false;
				break;
			case 3:	// right mouse button
				break;
			}
		}

		void Selection_MouseMove (object obj, EventButton args)
		{
			if (move) {
				// get diff
				double diffx = args.X - lastX;
				double diffy = args.Y - lastY;
				lastX = args.X;
				lastY = args.Y;

				foreach (CanvasItem current in selectedItems)
					current.Move (diffx, diffy);
				selection.Move (diffx, diffy);
			}

		}

		void Selection_KeyPress (object obj, EventKey args)
		{
			switch (args.Key) {
			case Gdk.Key.Delete:
				// delete selection
				foreach (CanvasItem current in selectedItems) {
					current.Destroy ();
					elements.Remove (current);
				}
				unselect ();
				break;
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

