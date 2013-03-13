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
		class Note : VBox
		{
			abstract class Tool
			{
				public abstract Tool getInstance ();

				public abstract void Start (double x, double y);

				public abstract void Continue (double x, double y);

				public abstract void Complete (double x, double y);

				public abstract void Reset ();
			}

			class SelectionTool : Tool
			{

				bool selectionInProgress = false;
				CanvasRE selectionRect;
				bool move = false;
				double lastX, lastY;
				Canvas myCanvas;

				public SelectionTool (Canvas canvas)
				{
					myCanvas = canvas;
				}

				public override void Start (double x, double y)
				{
					selectionRect = new CanvasRect (myCanvas.Root ());

					selectionRect.X1 = x;
					selectionRect.Y1 = y;
					selectionRect.X2 = x;
					selectionRect.Y2 = y;

					selectionRect.FillColorRgba = 0x88888830; // 0xRRGGBBAA
					selectionRect.OutlineColor = "black";

					selectionInProgress = true;
				}

				public override void Continue (double x, double y)
				{
					if (selectionInProgress) {
						selectionRect.X2 = x;
						selectionRect.Y2 = y;
					}
				}

				public override void Complete (double x, double y)
				{
					Continue (x, y);

					// enable key event recognition
					selectionRect.GrabFocus ();

					selectionRect.CanvasEvent += new Gnome.CanvasEventHandler (Selection_Event);

					selectionInProgress = false;

					//TODO find selected items
					selection.SelectItemsWithin (selectionRect.X1, selectionRect.X2, selectionRect.Y1, selectionRect.Y2);
				}

				public override void Reset ()
				{
					try {
						selectionRect.Destroy ();
						selectionRect = null;
						selection.unselect ();
					} catch (NullReferenceException) {
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

						foreach (CanvasItem current in selection.items)
							current.Move (diffx, diffy);
						selectionRect.Move (diffx, diffy);
					}
				}

				void Selection_KeyPress (object obj, EventKey args)
				{
					switch (args.Key) {
					case Gdk.Key.Delete: // delete selection
						foreach (CanvasItem current in selection.items) {
							current.Destroy ();
							elements.Remove (current);
						}
						selection.unselect ();
						Reset ();
						break;
					}
				}

				public override Tool getInstance ()
				{
					return new SelectionTool (myCanvas);
				}
			}

			class Stroke : Tool
			{
				CanvasLine currentStroke;
				ArrayList currentStrokePoints;
				Canvas myCanvas;

				public Stroke (Canvas canvas)
				{
					myCanvas = canvas;
				}

				public override void Start (double x, double y)
				{
					currentStroke = new CanvasLine (myCanvas.Root ());
					currentStroke.WidthUnits = 2;
					currentStroke.FillColor = "black";
					currentStroke.CanvasEvent += new Gnome.CanvasEventHandler (Line_Event);
					currentStrokePoints = new ArrayList ();
					currentStrokePoints.Add (x);
					currentStrokePoints.Add (y);
				}

				public override void Continue (double x, double y)
				{
					try {
						currentStrokePoints.Add (x);
						currentStrokePoints.Add (y);
						currentStroke.Points = new CanvasPoints (currentStrokePoints.ToArray (typeof(double)) as double[]);
					} catch (NullReferenceException) {
						// in case there was no line started
					}
				}

				public override void Complete (double x, double y)
				{
					currentStrokePoints.Add (x);
					currentStrokePoints.Add (y);
					currentStroke.Points = new CanvasPoints (currentStrokePoints.ToArray (typeof(double)) as double[]);

					// add the final stroke to the list of elements
					elements.Add (currentStroke);

					currentStroke = null;
					currentStrokePoints = null;
				}

				public override void Reset ()
				{

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
						selection.selectItem ((CanvasLine)obj);
						break;
					case 3:	// right mouse button deletes the line
						((CanvasLine)obj).Destroy ();
						break;
					}
				}

				public override Tool getInstance ()
				{
					return new Stroke (myCanvas);
				}
			}

			class Selection
			{
				Canvas myCanvas;

				public Selection (Canvas canvas)
				{
					myCanvas = canvas;
				}

				public List<CanvasItem> items = new List<CanvasItem> ();

				public void SelectItemsWithin (double x1, double x2, double y1, double y2)
				{
					//TODO find a way to read elements back from the canvas itself
					//foreach (CanvasItem current in myCanvas.AllChildren) {
					foreach (CanvasItem current in elements) {
						double cx1, cx2, cy1, cy2;
						current.GetBounds (out cx1, out cy1, out cx2, out cy2);
						if (x1 < cx1 && x2 > cx2 && y1 < cy1 && y2 > cy2)
							items.Add (current);
					}
				}

				/**
				 * select one item by placing a gray box around it
				 * TODO offer various selection methods
				 */
				public void selectItem (CanvasItem item)
				{
					double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
					item.GetBounds (out x1, out y1, out x2, out y2);

					// use these to setup the selection rectangle
					action = new SelectionTool (myCanvas);
					action.Start (x1, y1);
					action.Complete (x2, y2);
					// clear the selection cache because we only want one single element selected
					items.Clear ();

					items.Add (item);
				}

				/**
				 * unselect all
				 */
				public void unselect ()
				{
					try {
						items.Clear ();
					} catch (NullReferenceException) {
						// in case nothing is selected
					}
				}
			}

			const int canvasWidth = 1500, canvasHeight = 1500;
			Canvas myCanvas;
			static Tool action;
			static Selection selection;

			public Note ()
			{
				// add a canvas to the second column
				myCanvas = Canvas.NewAa ();
				// TODO find out why this somehow centers the axis origin.
				myCanvas.SetScrollRegion (0.0, 0.0, canvasWidth, canvasHeight);
				this.Add (myCanvas);
				selection = new Selection (myCanvas);

				// draw a filled rectangle to represent drawing area
				CanvasRE item = new CanvasRect (myCanvas.Root ());
				item.FillColor = "white";
				item.OutlineColor = "black";
				item.X1 = 0;
				item.Y1 = 0;
				item.X2 = canvasWidth;
				item.Y2 = canvasHeight;

				// add mouse trackers
				item.CanvasEvent += new Gnome.CanvasEventHandler (Event);
			}

			/**
			 * change the canvas scale
			 */
			public void Scale (double factor)
			{
				int width = (int)Math.Round (myCanvas.PixelsPerUnit * factor * canvasWidth);

				Fit (width);
			}

			/**
			 * fit the canvas scale to a certain width
			 */
			public void Fit (int width)
			{
				myCanvas.PixelsPerUnit = ((double)width) / canvasWidth;

				myCanvas.SetSizeRequest (width, (int)Math.Round (canvasHeight * myCanvas.PixelsPerUnit));
				myCanvas.UpdateNow ();
			}

			public void Fit ()
			{
				uint width, height;
				myCanvas.GetSize (out width, out height);
				Fit ((int)width);
			}

			/**
			 * callback for handling events from canvas drawing area
			 */
			void Event (object obj, Gnome.CanvasEventArgs args)
			{
				try {
					EventButton ev = new EventButton (args.Event.Handle);
					if (EventType.ButtonPress == ev.Type) {
						if (null != action)
							action.Reset ();
						if (1 == ev.Button)
							action = new Stroke (myCanvas);
						else if (3 == ev.Button)
							action = new SelectionTool (myCanvas);
					}

					switch (ev.Type) {
					case EventType.ButtonPress:
						action.Start (ev.X, ev.Y);
						break;
					case EventType.MotionNotify:
						action.Continue (ev.X, ev.Y);
						break;
					case EventType.ButtonRelease:
						action.Complete (ev.X, ev.Y);
						break;
					}
				} catch (NullReferenceException) {
				}
			}
		}

		TreeView myTreeView;
		static List<Note> notes = new List<Note> ();
		Gtk.Window win;

		// TODO find a way to read elements back from the canvas itself
		static List<CanvasItem> elements = new List<CanvasItem> ();

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

			VBox myContentContainer = new VBox (false, 0);
			myViewport.Add (myContentContainer);

			VBox myNotesContainer = new VBox (false, 0);
			myContentContainer.Add (myNotesContainer);

			Note note = new Note ();
			notes.Add (note);

			myNotesContainer.Add (note);

			// indicate that there will somewhen be the option to add another notes area
			Button addNotesButton = new Button (Gtk.Stock.Add);
			myContentContainer.Add (addNotesButton);
			win.ShowAll ();

			myTreeView.Visible = false;

			note.Fit (400);
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
			foreach (Note note in notes)
				note.Scale ((double)10 / 9);
		}

		/**
		 * callback for zooming out
		 */
		void ZoomOutButton_Clicked (object obj, EventArgs args)
		{
			foreach (Note note in notes)
				note.Scale ((double)9 / 10);
		}

		/**
		 * callback for zooming out
		 */
		void ZoomFitButton_Clicked (object obj, EventArgs args)
		{
			foreach (Note note in notes)
				note.Fit ();
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

