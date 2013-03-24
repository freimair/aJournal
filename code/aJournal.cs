using System;
using System.Collections;
using System.Collections.Generic;
using Gnome;
using Gtk;
using Gdk;
using backend;
using backend.NoteElements;
using ui_gtk_gnome.Tools;
using ui_gtk_gnome.NoteElements;
using System.Linq;

namespace ui_gtk_gnome
{
	class UiNote : VBox
	{
		const int canvasWidth = 1500, canvasHeight = 1500;
		Canvas myCanvas;
		CanvasRect drawingArea;
		List<UiNoteElement> elements = new List<UiNoteElement> ();
		Note myNote;

		public UiNote ()
		{
			myNote = Note.Create ();
			Init ();
		}

		public UiNote (Note note)
		{
			myNote = note;
			Init ();

			foreach (NoteElement current in note.GetElements())
				elements.Add (UiNoteElement.Recreate (myCanvas, note, current));
		}

		void Init ()
		{
			// add a canvas to the second column
			myCanvas = Canvas.NewAa ();
			// TODO find out why this somehow centers the axis origin.
			myCanvas.SetScrollRegion (0.0, 0.0, canvasWidth, canvasHeight);
			this.Add (myCanvas);

			// draw a filled rectangle to represent drawing area
			drawingArea = new CanvasRect (myCanvas.Root ());
			drawingArea.FillColor = "white";
			drawingArea.OutlineColor = "black";
			drawingArea.X1 = 0;
			drawingArea.Y1 = 0;
			drawingArea.X2 = canvasWidth;
			drawingArea.Y2 = canvasHeight;

			// add mouse trackers
			drawingArea.CanvasEvent += new Gnome.CanvasEventHandler (Event);
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

		static object myLock = new ImageTool ();

		/**
		 * callback for handling events from canvas drawing area
		 */
		void Event (object obj, Gnome.CanvasEventArgs args)
		{
			EventButton ev = new EventButton (args.Event.Handle);

			lock (myLock) { // TODO does not fix the FIXME in ImageTool.Start. why?
				try {
					switch (ev.Type) {
					case EventType.ButtonPress:
						aJournal.currentTool.Init (myCanvas, myNote, elements);
						aJournal.currentTool.Reset ();
						aJournal.currentTool.Start (ev.X, ev.Y);
						break;
					case EventType.MotionNotify:
						aJournal.currentTool.Continue (ev.X, ev.Y);
						break;
					case EventType.ButtonRelease:
						aJournal.currentTool.Complete (ev.X, ev.Y);
						myNote.Persist ();
						break;
					}
				} catch (NullReferenceException) {
				}
			}
		}
	}

	public class aJournal
	{
		TreeView myTreeView;
		static List<UiNote> notes = new List<UiNote> ();
		public static Gtk.Window win;
		VBox myNotesContainer;
		// static because we only want one tool active in the whole app
		public static Tool currentTool;
		RadioToolButton penToolButton, selectionToolButton, eraserToolButton, textToolButton, imageToolButton;

		// event fired when some zooming occurs
		public static event ScaledEventHandler Scaled;

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

			// add tool buttons
			penToolButton = new RadioToolButton (new GLib.SList (IntPtr.Zero));
			penToolButton.IconWidget = new Gtk.Image (new Pixbuf ("pencil.png"));
			penToolButton.TooltipText = "Pen";
			penToolButton.Clicked += SelectTool_Clicked;
			myToolbar.Insert (penToolButton, 4);
			// preselect pen
			SelectTool_Clicked (penToolButton, null);
			selectionToolButton = new RadioToolButton (penToolButton, Gtk.Stock.About);
			selectionToolButton.IconWidget = new Gtk.Image (new Pixbuf ("rect-select.png"));
			selectionToolButton.TooltipText = "Selection";
			selectionToolButton.Clicked += SelectTool_Clicked;
			myToolbar.Insert (selectionToolButton, 5);
			eraserToolButton = new RadioToolButton (penToolButton, Gtk.Stock.About);
			eraserToolButton.IconWidget = new Gtk.Image (new Pixbuf ("eraser.png"));
			eraserToolButton.TooltipText = "Eraser";
			eraserToolButton.Clicked += SelectTool_Clicked;
			myToolbar.Insert (eraserToolButton, 6);
			textToolButton = new RadioToolButton (penToolButton, Gtk.Stock.About);
			textToolButton.IconWidget = new Gtk.Image (new Pixbuf ("text-tool.png"));
			textToolButton.TooltipText = "Text";
			textToolButton.Clicked += SelectTool_Clicked;
			myToolbar.Insert (textToolButton, 7);
			imageToolButton = new RadioToolButton (penToolButton, Gtk.Stock.OrientationPortrait);
			imageToolButton.TooltipText = "Image";
			imageToolButton.Clicked += SelectTool_Clicked;
			myToolbar.Insert (imageToolButton, 8);

			// insert the toolbar into the layoutpen
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

			List<Note> noteList = Note.GetEntries ();
			foreach (Note note in noteList) {
				UiNote current = new UiNote (note);
				notes.Add (current);

				myNotesContainer.Add (current);
				current.Fit (400);
			}

			// indicate that there will somewhen be the option to add another notes area
			Button addNotesButton = new Button (Gtk.Stock.Add);
			addNotesButton.Clicked += AddNote;
			myContentContainer.Add (addNotesButton);
			win.ShowAll ();

			myTreeView.Visible = false;
		}

		void SelectTool_Clicked (object obj, EventArgs args)
		{
			if (null != currentTool)
				currentTool.Reset ();

			if (obj == penToolButton)
				currentTool = new StrokeTool ();
			else if (obj == selectionToolButton)
				currentTool = new SelectionTool ();
			else if (obj == eraserToolButton)
				currentTool = new EraserTool ();
			else if (obj == textToolButton)
				currentTool = new TextTool ();
			else if (obj == imageToolButton)
				currentTool = new ImageTool ();
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
			if (null != Scaled)
				Scaled ();
		}

		/**
		 * callback for zooming out
		 */
		void ZoomOutButton_Clicked (object obj, EventArgs args)
		{
			foreach (UiNote note in notes)
				note.Scale ((double)9 / 10);
			if (null != Scaled)
				Scaled ();
		}

		/**
		 * callback for zooming out
		 */
		void ZoomFitButton_Clicked (object obj, EventArgs args)
		{
			foreach (UiNote note in notes)
				note.Fit ();
			if (null != Scaled)
				Scaled ();
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

	public delegate void ScaledEventHandler ();
}

