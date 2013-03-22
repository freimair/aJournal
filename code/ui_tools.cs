using System;
using System.Collections.Generic;
using Gnome;
using Gtk;
using Gdk;
using ui_gtk_gnome.NoteElements;

namespace ui_gtk_gnome
{
	namespace Tools
	{
		public abstract class Tool
		{
			public abstract void Init (Canvas canvas, List<UiNoteElement> items);

			public abstract void Start (double x, double y);

			public abstract void Continue (double x, double y);

			public abstract void Complete (double x, double y);

			public abstract void Reset ();
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

			public override void Init (Canvas canvas, List<UiNoteElement> items)
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

		public class StrokeTool : Tool
		{
			UiLine currentStroke;
			Canvas myCanvas;
			List<UiNoteElement> elements;

			public override void Init (Canvas canvas, List<UiNoteElement> items)
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

		public class EraserTool : Tool
		{
			List<UiNoteElement> elements;
			public override void Init (Canvas canvas, List<UiNoteElement> items)
			{
				elements = items;
			}

			public override void Start (double x, double y)
			{
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

//		public class ResizeDrawingAreaTool : Tool
//		{
////								if (ev.Y > canvasHeight - canvasHeight * 5 / 100 && ev.Y < canvasHeight) {
////						GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.DoubleArrow);
////						currentTool = new ResizeDrawingAreaTool (myCanvas, drawingArea);
////					} else {
////						GdkWindow.Cursor = new Gdk.Cursor (Gdk.CursorType.Arrow);
////						currentTool = null;
////					}
//
//			CanvasRect myDrawingArea;
//			bool active = false;
//
//			public ResizeDrawingAreaTool (CanvasRect drawingArea)
//			{
//				myDrawingArea = drawingArea;
//			}
//
//			public override void Start (double x, double y)
//			{
//				active = true;
//			}
//
//			public override void Continue (double x, double y)
//			{
//				if (active)
//					myDrawingArea.Y2 = y;
//			}
//
//			public override void Complete (double x, double y)
//			{
//				active = false;
//			}
//
//			public override void Reset ()
//			{
//				active = false;
//			}
//		}
	}
}