using System;
using System.Linq;
using System.Collections.Generic;
using Gnome;
using Gtk;
using Gdk;
using Pango;
using backend;
using backend.NoteElements;

namespace ui_gtk_gnome
{
	namespace NoteElements
	{
		public class BoundingBox
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

		public abstract class UiNoteElement
		{
			public abstract BoundingBox BoundingBox ();

			public abstract void Move (double diffx, double diffy);

			public abstract void Destroy ();
		}

		public class UiLine : UiNoteElement
		{
			PolylineElement linemodel;
			CanvasLine line;
			Note myNote;

			public UiLine (Canvas canvas, Note note)
			{
				myNote = note;

				line = new CanvasLine (canvas.Root ());
				line.WidthUnits = 2;
				line.FillColor = "black";

				linemodel = new PolylineElement ();

				myNote.AddElement (linemodel);
			}

			public void Add (double x, double y)
			{
				myNote.RemoveElement (linemodel);

				linemodel.Points.Add (Convert.ToInt32 (x));
				linemodel.Points.Add (Convert.ToInt32 (y));

				if (linemodel.Points.Count > 2)
					line.Points = new CanvasPoints (linemodel.Points.Select (element => Convert.ToDouble (element)).ToArray ());

				myNote.AddElement (linemodel);
			}

			public override BoundingBox BoundingBox ()
			{
				double cx1, cx2, cy1, cy2;
				line.GetBounds (out cx1, out cy1, out cx2, out cy2);

				return new BoundingBox (cx1, cy1, cx2, cy2);
			}

			public List<int> Points { get { return linemodel.Points; } }

			public override void Move (double diffx, double diffy)
			{
				myNote.RemoveElement (linemodel);

				for (int i = 0; i < linemodel.Points.Count; i += 2) {
					linemodel.Points [i] += Convert.ToInt32 (diffx);
					linemodel.Points [i + 1] += Convert.ToInt32 (diffy);
				}

				line.Move (diffx, diffy);

				myNote.AddElement (linemodel);
			}

			public override void Destroy ()
			{
				line.Destroy ();
				myNote.RemoveElement (linemodel);
			}
		}

		public class UiText : UiNoteElement
		{
			public class FontSize
			{
				public static int Normal = 10000;
				public static int Larger = 17500;
				public static int Large = 25000;
			}

			Note myNote;
			TextElement myText;

			CanvasWidget canvasWidget;
			TextView view;
			CanvasRE itemize;

			bool controlModifierActive = false;
			bool shiftModifierActive = false;

			public UiText (Canvas canvas, Note note)
			{
				myNote = note;
				myText = new TextElement ();
				canvasWidget = new CanvasWidget (canvas.Root ());

				// use Gtk TextView widget for text input
				view = new TextView ();
				// reset text style
				FontDescription fontDescription = view.Style.FontDescription;
				fontDescription.Size = FontSize.Normal;
				fontDescription.Weight = Weight.Normal;
				view.ModifyFont (fontDescription);
				myText.FontSize = Convert.ToInt32 (fontDescription.Size / (canvasWidget.Canvas.PixelsPerUnit * 1000));
				myText.FontStrong = fontDescription.Weight == Weight.Bold;

				// do further configuration
				view.CursorVisible = true;
				view.Editable = true;
				view.Show ();

				// create canvas container for text view widget
				canvasWidget.Widget = view;

				// autoresize the canvasWidget to match the textviews size
				view.SizeRequested += delegate(object o, SizeRequestedArgs args) {
					canvasWidget.Width = args.Requisition.Width / canvas.PixelsPerUnit + 20;
					canvasWidget.Height = args.Requisition.Height / canvas.PixelsPerUnit;
				};

				// set focus to textview for keyboard input
				view.GrabFocus ();

				// handle CTRL/SHIFT/0/1/2/3 keys
				view.KeyPressEvent += TextView_KeyPress;
				view.KeyReleaseEvent += TextView_KeyRelease;

				// handle TAB key and ENTER key
				view.Buffer.Changed += TextView_TextChanged;

				myNote.AddElement (myText);
			}

			void TextView_KeyPress (object o, KeyPressEventArgs args)
			{
				EventKey ev = new EventKey (args.Event.Handle);
				if (controlModifierActive) {
					FontDescription fontDescription = view.Style.FontDescription;
					switch (ev.Key) {
					case Gdk.Key.Key_0: // standard
						fontDescription.Size = FontSize.Normal;
						fontDescription.Weight = Weight.Normal;
						break;
					case Gdk.Key.Key_1: // h1
						fontDescription.Size = FontSize.Large;
						fontDescription.Weight = Weight.Bold;
						break;
					case Gdk.Key.Key_2: // h2
						fontDescription.Size = FontSize.Larger;
						fontDescription.Weight = Weight.Bold;
						break;
					case Gdk.Key.Key_3: // h3
						fontDescription.Size = FontSize.Normal;
						fontDescription.Weight = Weight.Bold;
						break;
					}

					view.ModifyFont (fontDescription);

					myNote.RemoveElement (myText);
					myText.FontSize = Convert.ToInt32 (fontDescription.Size / (canvasWidget.Canvas.PixelsPerUnit * 1000));
					myText.FontStrong = fontDescription.Weight == Weight.Bold;
					myNote.AddElement (myText);
					myNote.Persist (); // no mouse_up here to persist
				}

				// track modifier keys
				switch (ev.Key) {
				case Gdk.Key.Control_L:
				case Gdk.Key.Control_R:
					controlModifierActive = true;
					break;
				case Gdk.Key.Shift_L:
				case Gdk.Key.Shift_R:
					shiftModifierActive = true;
					break;
				}
			}

			void TextView_KeyRelease (object o, KeyReleaseEventArgs args)
			{
				EventKey ev = new EventKey (args.Event.Handle);

				// track modifier keys
				switch (ev.Key) {
				case Gdk.Key.Control_L:
				case Gdk.Key.Control_R:
					controlModifierActive = false;
					break;
				case Gdk.Key.Shift_L:
				case Gdk.Key.Shift_R:
					shiftModifierActive = false;
					break;
				}
			}

			void TextView_TextChanged (object sender, EventArgs e)
			{
				try {
					myNote.RemoveElement (myText);
				} catch (NullReferenceException) {
					/**
					 * in case of \n and \t we already removed the element from the note. Clipping the 
					 * \n/\t from the text and reassign to the text triggers this very callback again.
					 */
				}

				if (!shiftModifierActive && view.Buffer.Text.EndsWith ("\n")) {
					// trim the newline at the end of the string
					view.Buffer.Text = view.Buffer.Text.Substring (0, view.Buffer.Text.Length - 1);

					// the text assignment above triggered this method and with that created a new element in myNote
					myNote.RemoveElement (myText);

					// trigger new textbox
					aJournal.currentTool.Reset ();
					aJournal.currentTool.Start (canvasWidget.X, canvasWidget.Y + canvasWidget.Height + 30);
				}

				// itemize
				if (view.Buffer.Text.Contains ("\t")) {
					// trim the newline at the end of the string
					view.Buffer.Text = view.Buffer.Text.Replace ("\t", "");

					// the text assignment above triggered this method and with that created a new element in myNote
					myNote.RemoveElement (myText);

					int direction = 0;
					if (shiftModifierActive)
						direction = -1;
					else
						direction = 1;

					if (myText.IndentationLevel + direction < 0)
						direction = 0;

					myText.IndentationLevel += direction;

					// remove old bullet
					if (null != itemize)
						itemize.Destroy ();

					// check if there is a new bullet necessary
					if (myText.IndentationLevel > 0) {
						// create a circle or a rectangle
						if (myText.IndentationLevel % 2 == 1)
							itemize = new CanvasEllipse (canvasWidget.Canvas.Root ());
						else
							itemize = new CanvasRect (canvasWidget.Canvas.Root ());

						itemize.X1 = canvasWidget.Height / 3;
						itemize.X2 = 2 * canvasWidget.Height / 3;
						itemize.Y1 = canvasWidget.Height / 3;
						itemize.Y2 = 2 * canvasWidget.Height / 3;
						itemize.WidthUnits = 2;
						itemize.FillColor = "black";

						// move to appropriate position
						itemize.Move (canvasWidget.X + (myText.IndentationLevel - 1) * canvasWidget.Height * 1.5, canvasWidget.Y);
					}

					// indent text
					// TODO the relative movements only work as long as the same text size is used!
					canvasWidget.Move (direction * canvasWidget.Height * 1.5, 0);
				}

				myText.Text = view.Buffer.Text;
				myNote.AddElement (myText);
				myNote.Persist (); // no mouse_up here to persist
			}

			public bool Empty {
				get { return view.Buffer.CharCount == 0;}
			}

			public override BoundingBox BoundingBox ()
			{
				double cx1, cx2, cy1, cy2;
				canvasWidget.GetBounds (out cx1, out cy1, out cx2, out cy2);

				return new BoundingBox (cx1, cy1, cx2, cy2);
			}

			public override void Move (double diffx, double diffy)
			{
				myNote.RemoveElement (myText);

				canvasWidget.X += diffx;
				canvasWidget.Y += diffy;

				try {
					itemize.Move (diffx, diffy);
				} catch (NullReferenceException) {
				}

				myText.X += Convert.ToInt32 (diffx);
				myText.Y += Convert.ToInt32 (diffy);

				myNote.AddElement (myText);
				myNote.Persist (); // no mouse_up here to persist
			}

			public override void Destroy ()
			{
				canvasWidget.Destroy ();
				myNote.RemoveElement (myText);
			}
		}

		public class UiImage : UiNoteElement
		{
			CanvasPixbuf canvasPixbuf;
			Pixbuf myPixbuf;
			ImageElement myImage;
			Note myNote;

			public UiImage (Canvas canvas, Note note, String path)
			{
				myNote = note;

				canvasPixbuf = new CanvasPixbuf (canvas.Root ());

				myImage = new ImageElement ();
				myImage.LoadFromFile (path);

				Byte[] image = Convert.FromBase64String (myImage.Image);
				myPixbuf = new Pixbuf (image);

				myImage.Width = myPixbuf.Width;
				myImage.Height = myPixbuf.Height;

				canvasPixbuf.Pixbuf = myPixbuf.ScaleSimple (myImage.Width, myImage.Height, InterpType.Bilinear);
				myNote.AddElement (myImage);
			}

			public override BoundingBox BoundingBox ()
			{
				double cx1, cx2, cy1, cy2;
				canvasPixbuf.GetBounds (out cx1, out cy1, out cx2, out cy2);

				return new BoundingBox (cx1, cy1, cx2, cy2);
			}

			public override void Move (double diffx, double diffy)
			{
				myNote.RemoveElement (myImage);
				canvasPixbuf.Move (diffx, diffy);

				// the cavnvasPixbuf location is not updated by now so we have to do it manually
				myImage.X += Convert.ToInt32 (diffx);
				myImage.Y += Convert.ToInt32 (diffy);
				myNote.AddElement (myImage);
			}

			public override void Destroy ()
			{
				canvasPixbuf.Destroy ();
				myNote.RemoveElement (myImage);
			}
		}
	}
}
