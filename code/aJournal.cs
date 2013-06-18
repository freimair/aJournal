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
		CanvasRect overlay;
		public List<UiNoteElement> elements = new List<UiNoteElement> ();

		// TODO beware of the magic numbers
		public static int width = 1500, height = 1500;

		public UiNote ()
		{
			Init ();
			Refill ();
		}

		void Init ()
		{
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

			overlay = new CanvasRect (myCanvas.Root ());
			overlay.FillColorRgba = 0x44FF4400;
			overlay.X1 = 0;
			overlay.Y1 = 0;
			overlay.X2 = width;
			overlay.Y2 = height;

			// add mouse trackers
			overlay.CanvasEvent += new Gnome.CanvasEventHandler (Event);
		}

		ElementFilter filter = new ElementFilter ();

		public List<Tag> TagFilter {
			get {
				return filter.Tags;
			}
			set {
				filter.Tags = value;
				Refill ();
			}
		}

		void Refill ()
		{
			foreach (UiNoteElement current in elements) 
				current.Hide ();
			elements.Clear ();
			foreach (NoteElement current in NoteElement.GetElements(filter)) {
				UiNoteElement tmp = UiNoteElement.Recreate (myCanvas, current);
				AdjustSheetHeight (tmp.BoundingBox ().bottom);
				elements.Add (tmp);
			}
		}

		public double ScrollTo (UiText selected)
		{
			return selected.Y * myCanvas.PixelsPerUnit;
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

		void AdjustSheetHeight (double lasty)
		{
			if (UiNote.height < lasty + UiNote.width) {
				UiNote.height += Convert.ToInt32 (lasty) + UiNote.width - UiNote.height;
				drawingArea.Y2 = UiNote.height;
				overlay.Y2 = drawingArea.Y2;
				myCanvas.SetScrollRegion (drawingArea.X1, drawingArea.Y1, drawingArea.X2, drawingArea.Y2);
				myCanvas.HeightRequest = (int)Math.Round (UiNote.height * myCanvas.PixelsPerUnit);
				myCanvas.UpdateNow ();
			}
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

						// check if we have to extends the sheet
						AdjustSheetHeight (ev.Y);
						break;
					}
				} catch (NullReferenceException) {
				}
			}

			// raise overlay to top whenever a mouse input ended. That
			// way the receiving overlay layer is always on top. Is it?
			overlay.RaiseToTop ();
		}
	}

	// TODO where to put this?
	public class TagDialog : Dialog
	{
		TagTree myTagTree;

		public TagDialog (List<Tag> active) : base("edit Note Metadata", aJournal.win, DialogFlags.Modal | DialogFlags.DestroyWithParent, ButtonsType.OkCancel)
		{
			myTagTree = new TagTree (true);
			myTagTree.Selection = active;
			myTagTree.ShowAll ();
			VBox.Add (myTagTree);

			Button newButton = new Button (Gtk.Stock.New);
			newButton.Clicked += delegate(object sender, EventArgs e) {
				CreateTagDialog dialog = new CreateTagDialog (this);
				if (ResponseType.Ok == (ResponseType)dialog.Run ()) {
					Tag newTag = Tag.Create (dialog.TagName);
					newTag.Parent = dialog.Parenttag;
					myTagTree.Update ();
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

		public CreateTagDialog (Gtk.Window parent) : base("Create Tag", parent, DialogFlags.Modal | DialogFlags.DestroyWithParent, ButtonsType.OkCancel)
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

	class HeadingTree : VBox
	{
		TreeView myTreeView;
		TreeStore textList;
		List<UiNoteElement> elements;

		public delegate void SelectionChangedHandler (UiText heading);

		public event SelectionChangedHandler SelectionChanged;

		public HeadingTree (List<UiNoteElement> items)
		{
			elements = items;
			ScrolledWindow myScrolledContainer = new ScrolledWindow ();

			myTreeView = new TreeView ();
			myTreeView.HeadersVisible = false;
			myTreeView.EnableTreeLines = true;
			myTreeView.SetSizeRequest (300, 200);


			TreeViewColumn col = new TreeViewColumn ();
			CellRendererText myCellRendererText = new CellRendererText ();
			col.PackStart (myCellRendererText, true);

			myTreeView.AppendColumn (col);
			myTreeView.CursorChanged += delegate(object sender, EventArgs e) {
				if (null != SelectionChanged)
					SelectionChanged (Selection);
			};

			col.SetCellDataFunc (myCellRendererText, new TreeCellDataFunc (RenderHeading));

			Fill ();
			myTreeView.Model = textList;

			myScrolledContainer.Add (myTreeView);
			this.Add (myScrolledContainer);
		}

		private void RenderHeading (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			UiText uiText = (UiText)model.GetValue (iter, 1);
			(cell as Gtk.CellRendererText).Text = uiText.Text;
		}

		Dictionary<string, TreeIter> iters;

		void Fill ()
		{
			textList = new TreeStore (typeof(bool), typeof(UiText));

			iters = new Dictionary<string, TreeIter> ();

			foreach (UiNoteElement current in elements) {
				if (current is UiText) {
					UiText uiText = (UiText)current;
					if (uiText.IsH1 ()) {
						TreeIter candidate;
						try {
							candidate = textList.AppendValues (false, uiText);
							iters.Add ("h1", candidate);
						} catch (Exception) {
							iters ["h1"] = candidate;
						}
					} else if (uiText.IsH2 ()) {
						TreeIter candidate;
						try {
							candidate = textList.AppendValues (iters ["h1"], false, uiText);
							iters.Add ("h2", candidate);
						} catch (Exception) {
							iters ["h2"] = candidate;
						}
					} else if (uiText.IsH3 ()) {
						try {
							textList.AppendValues (iters ["h2"], false, uiText);
						} catch (Exception) {
							try {
								textList.AppendValues (iters ["h1"], false, uiText);
							} catch (Exception) {
								textList.AppendValues (false, uiText);
							}
						}
					}
				}
			}
			myTreeView.Model = textList;
			myTreeView.ExpandAll ();
		}

		public UiText Selection {
			get {
				TreeIter selectedIter;
				myTreeView.Selection.GetSelected (out selectedIter);

				return (UiText)textList.GetValue (selectedIter, 1);
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
			col.SetCellDataFunc (myCellRendererText, new TreeCellDataFunc (RenderTag));

			Update ();
			myTreeView.Model = tagList;

			myScrolledContainer.Add (myTreeView);
			this.Add (myScrolledContainer);
		}

		private void RenderTag (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			Tag tag = (Tag)model.GetValue (iter, 1);
			(cell as Gtk.CellRendererText).Text = tag.Name.Contains (".") ? tag.Name.Substring (tag.Name.LastIndexOf (".") + 1) : tag.Name;
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
		List<Tag> myPreset = new List<Tag> ();

		public void Fill (List<Tag> preset)
		{
			myPreset = preset;
			tagList = new TreeStore (typeof(bool), typeof(Tag));

			Tag[] tags = Tag.Tags.ToArray ();
			Array.Sort (tags, new MyComparer ());

			iters = new Dictionary<Tag, TreeIter> ();

			foreach (Tag current in tags) {
				if (null == current.Parent)
					iters.Add (current, tagList.AppendValues (myPreset.Contains (current), current));
				else
					iters.Add (current, tagList.AppendValues (iters [current.Parent], myPreset.Contains (current), current));
			}
			myTreeView.Model = tagList;
			myTreeView.ExpandAll ();
		}

		public void Update ()
		{
			Fill (myPreset);
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

					result.Add ((Tag)tagList.GetValue (selectedIter, 1));
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
		HeadingTree myHeadingView;
		public static Gtk.Window win;
		UiNote notesWidget;
		ScrolledWindow myScrolledNotesContainer;
		// static because we only want one tool active in the whole app
		public static Tool currentTool;
		RadioToolButton penToolButton, selectionToolButton, eraserToolButton, textToolButton, imageToolButton, verticalSpaceToolButton, tagToolButton;

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
			// and a toggle button to hide the treeview below
			ToggleToolButton showTagTreeButton = new ToggleToolButton ();
			showTagTreeButton.IconWidget = new Gtk.Image (new Pixbuf ("taglist.png"));
			showTagTreeButton.TooltipText = "toggle the taglist visibility";
			showTagTreeButton.Active = false;
			showTagTreeButton.Clicked += ShowTagTreeButton_Clicked;
			myToolbar.Add (showTagTreeButton);
			// and a toggle button to hide the heading tree below
			ToggleToolButton showHeadingTreeButton = new ToggleToolButton (Gtk.Stock.Index);
			showHeadingTreeButton.TooltipText = "toggle the headinglist visibility";
			showHeadingTreeButton.Active = false;
			showHeadingTreeButton.Clicked += ShowHeadingTreeButton_Clicked;
			myToolbar.Add (showHeadingTreeButton);

			// add zoom buttons
			ToolButton zoomInButton = new ToolButton (Gtk.Stock.ZoomIn);
			zoomInButton.TooltipText = "zoom in";
			zoomInButton.Clicked += ZoomInButton_Clicked;
			myToolbar.Add (zoomInButton);
			ToolButton zoomOutButton = new ToolButton (Gtk.Stock.ZoomOut);
			zoomOutButton.TooltipText = "zoom out";
			zoomOutButton.Clicked += ZoomOutButton_Clicked;
			myToolbar.Add (zoomOutButton);
			ToolButton zoomFitButton = new ToolButton (Gtk.Stock.ZoomFit);
			zoomFitButton.TooltipText = "zoom fit";
			zoomFitButton.Clicked += ZoomFitButton_Clicked;
			myToolbar.Add (zoomFitButton);

			// add tool buttons
			penToolButton = new RadioToolButton (new GLib.SList (IntPtr.Zero));
			penToolButton.IconWidget = new Gtk.Image (new Pixbuf ("pencil.png"));
			penToolButton.TooltipText = "Pen";
			penToolButton.Clicked += SelectTool_Clicked;
			myToolbar.Add (penToolButton);
			// preselect pen
			SelectTool_Clicked (penToolButton, null);
			selectionToolButton = new RadioToolButton (penToolButton, Gtk.Stock.About);
			selectionToolButton.IconWidget = new Gtk.Image (new Pixbuf ("rect-select.png"));
			selectionToolButton.TooltipText = "Selection";
			selectionToolButton.Clicked += SelectTool_Clicked;
			myToolbar.Add (selectionToolButton);
			eraserToolButton = new RadioToolButton (penToolButton, Gtk.Stock.About);
			eraserToolButton.IconWidget = new Gtk.Image (new Pixbuf ("eraser.png"));
			eraserToolButton.TooltipText = "Eraser";
			eraserToolButton.Clicked += SelectTool_Clicked;
			myToolbar.Add (eraserToolButton);
			textToolButton = new RadioToolButton (penToolButton, Gtk.Stock.About);
			textToolButton.IconWidget = new Gtk.Image (new Pixbuf ("text-tool.png"));
			textToolButton.TooltipText = "Text";
			textToolButton.Clicked += SelectTool_Clicked;
			myToolbar.Add (textToolButton);
			imageToolButton = new RadioToolButton (penToolButton, Gtk.Stock.OrientationPortrait);
			imageToolButton.TooltipText = "Image";
			imageToolButton.Clicked += SelectTool_Clicked;
			myToolbar.Add (imageToolButton);
			verticalSpaceToolButton = new RadioToolButton (penToolButton);
			verticalSpaceToolButton.IconWidget = new Gtk.Image (new Pixbuf ("stretch.png"));
			verticalSpaceToolButton.TooltipText = "vertical space";
			verticalSpaceToolButton.Clicked += SelectTool_Clicked;
			myToolbar.Add (verticalSpaceToolButton);
			tagToolButton = new RadioToolButton (penToolButton);
			tagToolButton.IconWidget = new Gtk.Image (new Pixbuf ("tag.png"));
			tagToolButton.TooltipText = "tag items";
			tagToolButton.Clicked += SelectTool_Clicked;
			myToolbar.Add (tagToolButton);


			// insert the toolbar into the layoutpen
			toolbarContentLayout.PackStart (myToolbar, false, false, 0);

			// add a column-like layout into the second row
			HPaned sidebarLayout = new HPaned ();
			toolbarContentLayout.Add (sidebarLayout);

			HBox sidebarContentLayout = new HBox ();
			sidebarLayout.Add1 (sidebarContentLayout);



			// add canvas container
			myScrolledNotesContainer = new ScrolledWindow ();
			myScrolledNotesContainer.SetPolicy (Gtk.PolicyType.Automatic, Gtk.PolicyType.Always);
			sidebarLayout.Add2 (myScrolledNotesContainer);

			Viewport myViewport = new Viewport ();
			myScrolledNotesContainer.Add (myViewport);

			notesWidget = new UiNote ();
			myViewport.Add (notesWidget);

			// create tag tree
			myTreeView = new TagTree (true);
			myTreeView.Selection = Tag.Tags;
			myTreeView.SelectionChanged += Filter_Changed;
			sidebarContentLayout.Add (myTreeView);

			// create the heading tree
			myHeadingView = new HeadingTree (notesWidget.elements);
			myHeadingView.SelectionChanged += HeadingSelection_Changed;
			sidebarContentLayout.Add (myHeadingView);

			Filter_Changed (Tag.Tags);

			win.ShowAll ();

			myScrolledNotesContainer.Vadjustment.Value = myScrolledNotesContainer.Vadjustment.Upper;
			myTreeView.Visible = false;
			myHeadingView.Visible = false;
			notesWidget.Fit (400);
			if (null != Scaled)
				Scaled ();
		}

		void Filter_Changed (List<Tag> selection)
		{
			notesWidget.TagFilter = selection;
		}

		void HeadingSelection_Changed (UiText selected)
		{
			myScrolledNotesContainer.Vadjustment.Value = notesWidget.ScrollTo (selected);
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
			else if (obj == tagToolButton)
				currentTool = new TagTool ();
		}

		/**
		 * callback for toggeling the tagtree visibility
		 */
		void ShowTagTreeButton_Clicked (object obj, EventArgs args)
		{
			myTreeView.Visible = ((ToggleToolButton)obj).Active;
		}

		/**
		 * callback for toggeling the tagtree visibility
		 */
		void ShowHeadingTreeButton_Clicked (object obj, EventArgs args)
		{
			myHeadingView.Visible = ((ToggleToolButton)obj).Active;
		}

		/**
		 * callback for zooming in
		 */
		void ZoomInButton_Clicked (object obj, EventArgs args)
		{
			notesWidget.Scale ((double)10 / 9);
			if (null != Scaled)
				Scaled ();
		}

		/**
		 * callback for zooming out
		 */
		void ZoomOutButton_Clicked (object obj, EventArgs args)
		{
			notesWidget.Scale ((double)9 / 10);
			if (null != Scaled)
				Scaled ();
		}

		/**
		 * callback for zooming out
		 */
		void ZoomFitButton_Clicked (object obj, EventArgs args)
		{
			notesWidget.Fit ();
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

