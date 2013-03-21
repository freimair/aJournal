using System;
using System.Collections;
using System.Collections.Generic;
using Gnome;
using Gtk;
using Gdk;
using backend;
using System.Linq;

namespace ui_gtk_gnome
{
	class BoundingBox
	{
		public BoundingBox (double left, double top, double right, double bottom)
		{
			this.left = left;
			this.right = right;
			this.top = top;
			this.bottom = bottom;
		}

		public double left;
		public double right;
		public double top;
		public double bottom;
	}

	abstract class UiNoteElement
	{
		public abstract BoundingBox BoundingBox ();

		public abstract void Move (double diffx, double diffy);

		public abstract void Destroy ();
	}

	class UiLine : UiNoteElement
	{
		Polyline linemodel;
		CanvasLine line;

		public UiLine (Canvas canvas)
		{
			line = new CanvasLine (canvas.Root ());
			line.WidthUnits = 2;
			line.FillColor = "black";

			linemodel = new Polyline ();
		}

		public void Add (double x, double y)
		{
			linemodel.Points.Add (Convert.ToInt32 (x));
			linemodel.Points.Add (Convert.ToInt32 (y));

			if (linemodel.Points.Count > 2)
				line.Points = new CanvasPoints (linemodel.Points.Select (element => Convert.ToDouble (element)).ToArray ());
		}

		public override BoundingBox BoundingBox ()
		{
			double cx1, cx2, cy1, cy2;
			line.GetBounds (out cx1, out cy1, out cx2, out cy2);

			return new BoundingBox (cx1, cy1, cx2, cy2);
		}

		public override void Move (double diffx, double diffy)
		{
			for (int i = 0; i < linemodel.Points.Count; i += 2) {
				linemodel.Points [i] += Convert.ToInt32 (diffx);
				linemodel.Points [i + 1] += Convert.ToInt32 (diffy);
			}

			line.Move (diffx, diffy);
		}

		public override void Destroy ()
		{
			line.Destroy ();
		}
	}

	class UiNote : VBox
	{
		abstract class Tool
		{
			public abstract void Start (double x, double y);

			public abstract void Continue (double x, double y);

			public abstract void Complete (double x, double y);

			public abstract void Reset ();
		}

		class SelectionTool : Tool
		{
			class Selection
			{
				public Selection (List<UiNoteElement> items)
				{
					elements = items;
				}

				public List<UiNoteElement> items = new List<UiNoteElement> ();
				public List<UiNoteElement> elements;

				public void SelectItemsWithin (double x1, double x2, double y1, double y2)
				{
					foreach (UiNoteElement current in elements) {
						BoundingBox box = current.BoundingBox ();
						if (x1 < box.left && x2 > box.right && y1 < box.top && y2 > box.bottom)
							items.Add (current);
					}
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

			bool selectionInProgress = false;
			CanvasRE selectionRect;
			bool move = false;
			double lastX, lastY;
			Canvas myCanvas;
			List<UiNoteElement> elements;
			Selection selection;

			public SelectionTool (Canvas canvas, List<UiNoteElement> items)
			{
				myCanvas = canvas;
				elements = items;
				selection = new Selection (items);
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

					foreach (UiNoteElement current in selection.items)
						current.Move (diffx, diffy);
					selectionRect.Move (diffx, diffy);
				}
			}

			void Selection_KeyPress (object obj, EventKey args)
			{
				switch (args.Key) {
				case Gdk.Key.Delete: // delete selection
					foreach (UiNoteElement current in selection.items) {
						current.Destroy ();
						elements.Remove (current);
					}
					selection.unselect ();
					Reset ();
					break;
				}
			}
		}

		class StrokeTool : Tool
		{
			UiLine currentStroke;
			Canvas myCanvas;
			List<UiNoteElement> elements;

			public StrokeTool (Canvas canvas, List<UiNoteElement> items)
			{
				myCanvas = canvas;
				elements = items;
			}

			public override void Start (double x, double y)
			{
				currentStroke = new UiLine (myCanvas);
			}

			public override void Continue (double x, double y)
			{
				try {
					currentStroke.Add (x, y);
				} catch (NullReferenceException) {
					// in case there was no line started
				}
			}

			public override void Complete (double x, double y)
			{
				currentStroke.Add (x, y);

				// add the final stroke to the list of elements
				elements.Add (currentStroke);

				currentStroke = null;
			}

			public override void Reset ()
			{

			}
		}

		const int canvasWidth = 1500, canvasHeight = 1500;
		Canvas myCanvas;

		// static because we only want one tool active in the whole app
		static Tool currentTool;
		List<UiNoteElement> elements = new List<UiNoteElement> ();

		public UiNote ()
		{
			// add a canvas to the second column
			myCanvas = Canvas.NewAa ();
			// TODO find out why this somehow centers the axis origin.
			myCanvas.SetScrollRegion (0.0, 0.0, canvasWidth, canvasHeight);
			this.Add (myCanvas);

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

		public int Width ()
		{
			return (int)Math.Round (myCanvas.PixelsPerUnit * canvasWidth);
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
			// ((CanvasItem) obj).Canvas may get us the canvas if we ever plan to work cross canvas
			try {
				EventButton ev = new EventButton (args.Event.Handle);
				if (EventType.ButtonPress == ev.Type) {
					if (null != currentTool)
						currentTool.Reset ();
					if (1 == ev.Button)
						currentTool = new StrokeTool (myCanvas, elements);
					else if (3 == ev.Button)
						currentTool = new SelectionTool (myCanvas, elements);
				}

				switch (ev.Type) {
				case EventType.ButtonPress:
					currentTool.Start (ev.X, ev.Y);
					break;
				case EventType.MotionNotify:
					currentTool.Continue (ev.X, ev.Y);
					break;
				case EventType.ButtonRelease:
					currentTool.Complete (ev.X, ev.Y);
					break;
				}
			} catch (NullReferenceException) {
			}
		}
	}

	public class aJournal
	{
		TreeView myTreeView;
		static List<UiNote> notes = new List<UiNote> ();
		Gtk.Window win;
		VBox myNotesContainer;

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

			myNotesContainer = new VBox (false, 0);
			myContentContainer.Add (myNotesContainer);

			UiNote note = new UiNote ();
			notes.Add (note);

			myNotesContainer.Add (note);

			// indicate that there will somewhen be the option to add another notes area
			Button addNotesButton = new Button (Gtk.Stock.Add);
			addNotesButton.Clicked += AddNote;
			myContentContainer.Add (addNotesButton);
			win.ShowAll ();

			myTreeView.Visible = false;

			note.Fit (400);
		}

		void AddNote (object obj, EventArgs args)
		{
			UiNote note = new UiNote ();
			notes.Add (note);
			note.Fit (notes [0].Width ());
			myNotesContainer.Add (note);
			note.ShowAll ();
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
			foreach (UiNote note in notes)
				note.Scale ((double)10 / 9);
		}

		/**
		 * callback for zooming out
		 */
		void ZoomOutButton_Clicked (object obj, EventArgs args)
		{
			foreach (UiNote note in notes)
				note.Scale ((double)9 / 10);
		}

		/**
		 * callback for zooming out
		 */
		void ZoomFitButton_Clicked (object obj, EventArgs args)
		{
			foreach (UiNote note in notes)
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

