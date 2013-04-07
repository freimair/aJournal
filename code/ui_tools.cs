using System;
using System.Collections.Generic;
using Gnome;
using Gtk;
using Gdk;
using backend;
using ui_gtk_gnome.NoteElements;

namespace ui_gtk_gnome
{
	namespace Tools
	{
		public abstract class Tool
		{
			public abstract void Init (CanvasRect sheet, List<UiNoteElement> items);

			public abstract void Start (double x, double y);

			public abstract void Continue (double x, double y);

			public abstract void Complete (double x, double y);

			public abstract void Reset ();
		}

		public class VerticalSpaceTool : Tool
		{
			CanvasRect mySheet;
			List<UiNoteElement> myItems;
			CanvasRect canvasVisualization;
			List<UiNoteElement> affectedItems = new List<UiNoteElement> ();
			double oldHeight;

			public override void Init (CanvasRect sheet, List<UiNoteElement> items)
			{
				mySheet = sheet;
				myItems = items;
			}

			public override void Start (double x, double y)
			{
//				canvasVisualization = new CanvasRect (mySheet.Canvas.Root ());
//				canvasVisualization.X1 = 0;
//				canvasVisualization.X2 = UiNote.width;
//				canvasVisualization.Y1 = y;
//				canvasVisualization.Y2 = y;
//
//				oldHeight = mySheet.Y2;
//
//				canvasVisualization.FillColorRgba = 0x88888830; // 0xRRGGBBAA
//				canvasVisualization.OutlineColor = "black";
//
//				// fetch items to be moved
//				foreach (UiNoteElement current in myItems)
//					if (y < current.BoundingBox ().top)
//						affectedItems.Add (current);
			}

			public override void Continue (double x, double y)
			{
//				try {
//					// memorize old y
//					double oldY = canvasVisualization.Y2;
//
//					canvasVisualization.Y2 = y;
//					// adjust sheet size
//					mySheet.Y2 = Convert.ToUInt32 (oldHeight + Convert.ToInt32 (canvasVisualization.Y2 - canvasVisualization.Y1));
//
//					// move affected items
//					foreach (UiNoteElement current in affectedItems)
//						// move by diffy
//						current.Move (0, y - oldY);
//				} catch (NullReferenceException) {
//				}
			}

			public override void Complete (double x, double y)
			{
//				try {
//					// adjust canvas size
//					myNote.Height += Convert.ToInt32 (canvasVisualization.Y2 - canvasVisualization.Y1);
//					mySheet.Canvas.HeightRequest = Convert.ToInt32 (mySheet.Canvas.PixelsPerUnit * myNote.Height);
//					mySheet.Canvas.SetScrollRegion (0.0, 0.0, myNote.Width, myNote.Height);
//
//					myNote.Persist ();
//
//					canvasVisualization.Destroy ();
//					canvasVisualization = null;
//				} catch (NullReferenceException) {
//				}
			}

			public override void Reset ()
			{
//				affectedItems.Clear ();
			}
		}

		public class SelectionTool : Tool
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
					// sort selection
					if (x1 > x2) {
						double tmp = x1;
						x1 = x2;
						x2 = tmp;
					}

					if (y1 > y2) {
						double tmp = y1;
						y1 = y2;
						y2 = tmp;
					}

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
			CanvasRect mySheet;
			List<UiNoteElement> elements;
			Selection selection;

			public override void Init (CanvasRect sheet, List<UiNoteElement> items)
			{
				mySheet = sheet;
				elements = items;
				selection = new Selection (items);
			}

			public override void Start (double x, double y)
			{
				selectionRect = new CanvasRect (mySheet.Canvas.Root ());
				selectionRect.RaiseToTop (); // TODO does not work.

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

				foreach (UiNoteElement current in selection.items)
					current.EditComleted ();
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

				foreach (UiNoteElement current in selection.items)
					current.EditComleted ();
			}
		}

		public class StrokeTool : Tool
		{
			UiLine currentStroke;
			CanvasRect mySheet;
			List<UiNoteElement> elements;

			public override void Init (CanvasRect sheet, List<UiNoteElement> items)
			{
				mySheet = sheet;
				elements = items;
			}

			public override void Start (double x, double y)
			{
				currentStroke = new UiLine (mySheet.Canvas);
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
				currentStroke.EditComleted ();

				// add the final stroke to the list of elements
				elements.Add (currentStroke);

				currentStroke = null;
			}

			public override void Reset ()
			{
				// delete if empty
				try {
					if (2 > currentStroke.Points.Count)
						currentStroke.Destroy ();
				} catch (NullReferenceException) {
				}
			}
		}

		public class EraserTool : Tool
		{
			List<UiNoteElement> elements;

			public override void Init (CanvasRect sheet, List<UiNoteElement> items)
			{
				elements = items;
			}

			public override void Start (double x, double y)
			{
				Do (x, y);
			}

			public override void Continue (double x, double y)
			{
				Do (x, y);
			}

			public override void Complete (double x, double y)
			{
			}

			void Do (double x, double y)
			{
				foreach (UiNoteElement current in elements) {
					BoundingBox bb = current.BoundingBox ();
					if (!(x > bb.left && y > bb.top && x < bb.right && y < bb.bottom))
						continue;

					if (current is UiLine) {
						UiLine tmp = (UiLine)current;

						for (int i = 0; i < tmp.Points.Count; i += 2) {
							int radius = 10; // TODO do we want a configurable eraser (and pen) radius?
							int cx = tmp.Points [i], cy = tmp.Points [i + 1];
							if (x > cx - radius && y > cy - radius && x < cx + radius && y < cy + radius) {
								// we have a match
								tmp.Destroy (); // TODO make sure the line is destroyed in the backend as well
							}


//							TODO prune line instead of deleting the whole thing
//							if(i == 0 && eraser matches)
							// remove point from UiLine
//							if(i == tmp.Points.Count - 1 && eraser matches)
							// remove point from UiLine
//							else
							// split line and treat each subline as above
						}
					}

				}
			}

			public override void Reset ()
			{
			}
		}

		public class TextTool : Tool
		{
			CanvasRect mySheet;
			List<UiNoteElement> elements;
			UiText myText;

			public override void Init (CanvasRect sheet, List<UiNoteElement> items)
			{
				mySheet = sheet;
				elements = items;
			}

			public override void Start (double x, double y)
			{
				myText = new UiText (mySheet.Canvas);
				elements.Add (myText);

				// place empty text on (x,y)
				myText.Move (x, y);

				// TODO unfortunately we cannot hook to the event if an element is recreated
				myText.MoveFocus += MoveFocusUp_EventHandler;
			}

			void MoveFocusUp_EventHandler (UiText sender, bool up)
			{
				UiText winner = null;
				foreach (UiNoteElement element in elements) {
					if (element is UiText && sender != element) {
						if (sender.Y > ((UiText)element).Y && up) {
							if (null == winner)
								winner = (UiText)element;
							if (sender.Y - ((UiText)element).Y < sender.Y - winner.Y)
								winner = (UiText)element;
						} else if (sender.Y < ((UiText)element).Y && !up) {
							if (null == winner)
								winner = (UiText)element;
							if (((UiText)element).Y - sender.Y < winner.Y - sender.Y)
								winner = (UiText)element;
						}
					}
				}
				winner.ForceFocus ();
			}

			public override void Continue (double x, double y)
			{
				// do nothing since this is mouse move
			}

			public override void Complete (double x, double y)
			{
				// do nothing since this is mouse up
			}

			public override void Reset ()
			{
				// delete text if empty
				try {
					if (myText.Empty) {
						myText.Destroy ();
						elements.Remove (myText);
					}
				} catch (NullReferenceException) {
				}
			}
		}

		public class ImageTool : Tool
		{
			UiImage myImage;
			List<UiNoteElement> elements;
			CanvasRect mySheet;

			public override void Init (CanvasRect sheet, List<UiNoteElement> items)
			{
				elements = items;
				mySheet = sheet;
			}

			public override void Start (double x, double y)
			{
				FileChooserDialog fc = new FileChooserDialog ("Choose the file to open", aJournal.win, FileChooserAction.Open,
				                                              "Cancel", ResponseType.Cancel, "Open", ResponseType.Accept);

				// FIXME Run() does not block. so the mouse up action is performed before we select an image.
				// therefore, nothing gets persisted.
				if (fc.Run () == (int)ResponseType.Accept) {
					myImage = new UiImage (mySheet.Canvas, fc.Filename, x, y);
					elements.Add (myImage);
				}

				//Don't forget to call Destroy() or the FileChooserDialog window won't get closed.
				fc.Destroy ();
			}

			public override void Continue (double x, double y)
			{
			}

			public override void Complete (double x, double y)
			{
			}

			public override void Reset ()
			{
			}
		}
	}
}