using System;
using System.Collections;
using System.Collections.Generic;
using Gnome;
using Gtk;
using Gdk;
using backend;
using backend.Tags;
using backend.NoteElements;
using ui_gtk_gnome.Tools;
using ui_gtk_gnome.NoteElements;
using System.Linq;

namespace ui_gtk_gnome
{
	class UiNote : VBox
	{
		Canvas myCanvas;
		CanvasRect drawingArea;
		List<UiNoteElement> elements = new List<UiNoteElement> ();

		// TODO beware of the magic numbers
		public static int width = 1500, height = 1500;

		public UiNote ()
		{
			Init ();
//			Draw ();
		}

		void Init ()
		{
//			myButton.Clicked += delegate(object o, EventArgs args) {
//				NoteSettings tmp = new NoteSettings (myNote);
//				if (ResponseType.Ok == (ResponseType)tmp.Run ()) {
//					// TODO provide setter for tags in backend
//					foreach (Tag tag in myNote.GetTags())
//						myNote.RemoveTag (tag);
//					foreach (Tag tag in tmp.Selection)
//						myNote.AddTag (tag);
//					myNote.Persist ();
//				}
//				tmp.Hide ();
//				myLabel.Text = myNote.ModificationTimestamp + " - ";
//				foreach (Tag current in myNote.GetTags())
//					myLabel.Text += current.ToString () + " ";
//			};

			// add a canvas to the second column
			myCanvas = Canvas.NewAa ();
			myCanvas.SetScrollRegion (0.0, 0.0, width, height);
			this.Add (myCanvas);

			// draw a filled rectangle to represent drawing area
			drawingArea = new CanvasRect (myCanvas.Root ());
			drawingArea.FillColor = "white";
			drawingArea.X1 = 0;
			drawingArea.Y1 = 0;
			drawingArea.X2 = width;
			drawingArea.Y2 = height;

			// add mouse trackers
			drawingArea.CanvasEvent += new Gnome.CanvasEventHandler (Event);
		}

		public int Width ()
		{
			return (int)Math.Round (myCanvas.PixelsPerUnit * width);
		}

		/**
		 * change the canvas scale
		 */
		public void Scale (double factor)
		{
			int width = (int)Math.Round (myCanvas.PixelsPerUnit * factor * UiNote.width);

			Fit (width);
		}

		/**
		 * fit the canvas scale to a certain width
		 */
		public void Fit (int width)
		{
			myCanvas.PixelsPerUnit = ((double)width) / UiNote.width;

			myCanvas.SetSizeRequest (width, (int)Math.Round (UiNote.height * myCanvas.PixelsPerUnit));
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
						aJournal.currentTool.Init (drawingArea, elements);
						aJournal.currentTool.Reset ();
						aJournal.currentTool.Start (ev.X, ev.Y);
						break;
					case EventType.MotionNotify:
						aJournal.currentTool.Continue (ev.X, ev.Y);
						break;
					case EventType.ButtonRelease:
						aJournal.currentTool.Complete (ev.X, ev.Y);
						aJournal.currentTool.Reset ();
						break;
					}
				} catch (NullReferenceException) {
				}
			}
		}
	}

	class NoteSettings : Dialog
	{
		TagTree myTagTree;

		public NoteSettings (Note myNote) : base("edit Note Metadata", aJournal.win, DialogFlags.Modal | DialogFlags.DestroyWithParent, ButtonsType.OkCancel)
		{
			myTagTree = new TagTree (true);
			myTagTree.Selection = myNote.GetTags ();
			myTagTree.ShowAll ();
			VBox.Add (myTagTree);

			Button newButton = new Button (Gtk.Stock.New);
			newButton.Clicked += delegate(object sender, EventArgs e) {
				CreateTagDialog dialog = new CreateTagDialog (myNote, this);
				if (ResponseType.Ok == (ResponseType)dialog.Run ()) {
					Tag newTag = Tag.Create (dialog.TagName);
					newTag.Parent = dialog.Parenttag;
					myNote.AddTag (newTag);
					myNote.Persist ();
					myTagTree.Selection = myNote.GetTags ();
					myNote.RemoveTag (newTag);
					myNote.Persist ();
				}
				dialog.Hide ();
			};
			newButton.Show ();
			VBox.Add (newButton);

			this.AddButton (Gtk.Stock.Cancel, ResponseType.Cancel);
			this.AddButton (Gtk.Stock.Ok, ResponseType.Ok);
		}

		public List<Tag> Selection {
			get{ return myTagTree.Selection; }
		}
	}

	class CreateTagDialog : Dialog
	{
		TagTree myTagTree;
		Gtk.Entry nameEntry;

		public CreateTagDialog (Note note, Gtk.Window parent) : base("Create Tag", parent, DialogFlags.Modal | DialogFlags.DestroyWithParent, ButtonsType.OkCancel)
		{
			VBox.Add (new Label ("Parent:"));
			myTagTree = new TagTree (false);
			myTagTree.Selection = new List<Tag> ();
			VBox.Add (myTagTree);

			VBox.Add (new Label ("Name:"));
			nameEntry = new Gtk.Entry ();
			VBox.Add (nameEntry);
			VBox.ShowAll ();

			this.AddButton (Gtk.Stock.Cancel, ResponseType.Cancel);
			this.AddButton (Gtk.Stock.Ok, ResponseType.Ok);
		}

		public string TagName {
			get { return nameEntry.Text; }
		}

		public Tag Parenttag {
			get {
				if (0 < myTagTree.Selection.Count)
					return myTagTree.Selection [0];
				else
					return null;
			}
		}
	}

	class TagTree : VBox
	{
		TreeView myTreeView;
		TreeStore tagList;
		bool checkboxes;

		public delegate void SelectionChangedHandler (List<Tag> tags);
		public event SelectionChangedHandler SelectionChanged;

		public TagTree (bool displayCheckboxes)
		{
			checkboxes = displayCheckboxes;

			ScrolledWindow myScrolledContainer = new ScrolledWindow ();

			myTreeView = new TreeView ();
			myTreeView.HeadersVisible = false;
			myTreeView.EnableTreeLines = true;
			myTreeView.SetSizeRequest (300, 200);


			TreeViewColumn col = new TreeViewColumn ();
			CellRendererToggle myCellRendererToggle = new CellRendererToggle ();
			myCellRendererToggle.Activatable = true;
			if (checkboxes)
				col.PackStart (myCellRendererToggle, false);
			myCellRendererToggle.Toggled += TreeItem_Toggle;
			CellRendererText myCellRendererText = new CellRendererText ();
			col.PackStart (myCellRendererText, true);

			myTreeView.AppendColumn (col);
			myTreeView.CursorChanged += delegate(object sender, EventArgs e) {
				if (null != SelectionChanged)
					SelectionChanged (Selection);
			};

			if (checkboxes)
				col.AddAttribute (myCellRendererToggle, "active", 0);
			col.AddAttribute (myCellRendererText, "text", 1);

			Fill (new List<Tag> ());
			myTreeView.Model = tagList;

			myScrolledContainer.Add (myTreeView);
			this.Add (myScrolledContainer);
		}

		void TreeItem_Toggle (object o, ToggledArgs args)
		{
			TreeIter iter;

			if (myTreeView.Model.GetIter (out iter, new TreePath (args.Path))) {
				bool old = (bool)myTreeView.Model.GetValue (iter, 0);
				myTreeView.Model.SetValue (iter, 0, !old);
			}
		}

		class MyComparer : IComparer
		{
			public int Compare (object x, object y)
			{
				Tag tag1 = (Tag)x;
				Tag tag2 = (Tag)y;

				return tag1.ToString ().CompareTo (tag2.ToString ());
			}
		}

		Dictionary<Tag, TreeIter> iters;

		void Fill (List<Tag> preset)
		{
			tagList = new TreeStore (typeof(bool), typeof(string));

			Tag[] tags = Note.AllTags.ToArray ();
			Array.Sort (tags, new MyComparer ());

			iters = new Dictionary<Tag, TreeIter> ();

			foreach (Tag current in tags) {
				if (null == current.Parent)
					iters.Add (current, tagList.AppendValues (preset.Contains (current), current.Name));
				else
					iters.Add (current, tagList.AppendValues (iters [current.Parent], preset.Contains (current), current.Name));
			}
			myTreeView.Model = tagList;
		}

		public List<Tag> Selection {
			get {
				List<Tag> result = new List<Tag> ();
				if (checkboxes) {
					foreach (KeyValuePair<Tag, TreeIter> current in iters)
						if ((bool)myTreeView.Model.GetValue (current.Value, 0))
							result.Add (current.Key);
				} else {
					TreeIter selectedIter;
					myTreeView.Selection.GetSelected (out selectedIter);

					foreach (KeyValuePair<Tag, TreeIter> current in iters)
						if (selectedIter.Equals (current.Value))
							result.Add (current.Key);
				}

				return result;
			}

			set {
				Fill (value);
			}
		}
	}

	public class aJournal
	{
		TagTree myTreeView;
		static List<UiNote> notes = new List<UiNote> ();
		public static Gtk.Window win;
		VBox myNotesContainer;
		// static because we only want one tool active in the whole app
		public static Tool currentTool;
		RadioToolButton penToolButton, selectionToolButton, eraserToolButton, textToolButton, imageToolButton, verticalSpaceToolButton;

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
			verticalSpaceToolButton = new RadioToolButton (penToolButton);
			verticalSpaceToolButton.IconWidget = new Gtk.Image (new Pixbuf ("stretch.png"));
			verticalSpaceToolButton.TooltipText = "vertical space";
			verticalSpaceToolButton.Clicked += SelectTool_Clicked;
			myToolbar.Insert (verticalSpaceToolButton, 9);

			// insert the toolbar into the layoutpen
			toolbarContentLayout.PackStart (myToolbar, false, false, 0);

			// add a column-like layout into the second row
			HPaned taglistContentLayout = new HPaned ();
			toolbarContentLayout.Add (taglistContentLayout);

			// add an empty treeview to the first column

			// create tag tree
			myTreeView = new TagTree (true);
			myTreeView.SelectionChanged += Filter_Changed;
			taglistContentLayout.Add1 (myTreeView);

			// add canvas container
			ScrolledWindow myScrolledNotesContainer = new ScrolledWindow ();
			myScrolledNotesContainer.SetPolicy (Gtk.PolicyType.Automatic, Gtk.PolicyType.Always);
			taglistContentLayout.Add2 (myScrolledNotesContainer);

			Viewport myViewport = new Viewport ();
			myScrolledNotesContainer.Add (myViewport);

			VBox myContentContainer = new VBox (false, 0);
			myViewport.Add (myContentContainer);

			myNotesContainer = new VBox (false, 0);
			myContentContainer.Add (myNotesContainer);

			UiNote notesWidget = new UiNote ();
			myNotesContainer.Add (notesWidget);
			notesWidget.Fit (400);

			Filter_Changed (new List<Tag> ());

			// indicate that there will somewhen be the option to add another notes area
			Button addNotesButton = new Button (Gtk.Stock.Add);
			addNotesButton.Clicked += AddNote;
			myContentContainer.Add (addNotesButton);
			win.ShowAll ();

			myTreeView.Visible = false;
		}

		void Filter_Changed (List<Tag> selection)
		{
			// TODO notesWidget.UpdateFilter();
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
			else if (obj == verticalSpaceToolButton)
				currentTool = new VerticalSpaceTool ();
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

