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
			public static UiNoteElement Recreate (Canvas canvas, NoteElement element)
			{
				if (element is PolylineElement)
					return new UiLine (canvas, (PolylineElement)element);
				if (element is TextElement)
					return new UiText (canvas, (TextElement)element);
				if (element is ImageElement)
					return new UiImage (canvas, (ImageElement)element);
				return null;
			}

			public abstract BoundingBox BoundingBox ();

			public abstract void Move (double diffx, double diffy);

			public abstract void EditComleted ();

			public abstract void Destroy ();

			public abstract NoteElement Model { get; }

			public abstract void Hide ();
		}

		public class UiLine : UiNoteElement
		{
			PolylineElement linemodel;
			CanvasLine line;

			public UiLine (Canvas canvas, PolylineElement noteElement)
			{
				linemodel = noteElement;

				Init (canvas);
				line.Points = new CanvasPoints (linemodel.Points.Select (element => Convert.ToDouble (element)).ToArray ());
				line.Move (linemodel.X, linemodel.Y);
			}

			public UiLine (Canvas canvas)
			{
				linemodel = new PolylineElement ();
				Init (canvas);
			}

			void Init (Canvas canvas)
			{
				line = new CanvasLine (canvas.Root ());
				line.WidthUnits = 2;
				line.FillColor = "black";
			}

			public void Add (double x, double y)
			{
				if (0 == linemodel.X) {
					linemodel.X = Convert.ToInt64 (x);
					linemodel.Y = Convert.ToInt64 (y);
					line.Move (x, y);
				} else {
					linemodel.Points.Add (Convert.ToInt32 (x) - Convert.ToInt32 (linemodel.X));
					linemodel.Points.Add (Convert.ToInt32 (y) - Convert.ToInt32 (linemodel.Y));
				}

				if (linemodel.Points.Count > 2)
					line.Points = new CanvasPoints (linemodel.Points.Select (element => Convert.ToDouble (element)).ToArray ());
			}

			public override void EditComleted ()
			{
				linemodel.Persist ();
			}

			public override BoundingBox BoundingBox ()
			{
				double cx1, cx2, cy1, cy2;
				line.GetBounds (out cx1, out cy1, out cx2, out cy2);

				return new BoundingBox (cx1, cy1, cx2, cy2);
			}

			public List<int> Points { get { return linemodel.Points; } }

			public long XOffset { get { return linemodel.X; } }

			public long YOffset { get { return linemodel.Y; } }

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
				linemodel.Remove ();
			}

			public override NoteElement Model {
				get { return linemodel;}
			}

			public override void Hide ()
			{
				line.Destroy ();
			}
		}

		/**
		 * beware of the magic numbers
		 */
		public class UiText : UiNoteElement
		{

			TextElement myText;
			CanvasWidget canvasWidget;
			TextView view;
			CanvasRE itemize;
			Canvas myCanvas;
			bool controlModifierActive = false;
			bool shiftModifierActive = false;

			public UiText (Canvas canvas)
			{
				myText = new TextElement ();

				Init (canvas);

				Update ();
			}

			void Init (Canvas canvas)
			{
				canvasWidget = new CanvasWidget (canvas.Root ());
				myCanvas = canvas;

				// use Gtk TextView widget for text input
				view = new TextView ();
				// reset text style
//				Scaled ();

				// do further configuration
				view.CursorVisible = true;
				view.Editable = true;
				view.Show ();

				// create canvas container for text view widget
				canvasWidget.Widget = view;

				// autoresize the canvasWidget to match the textviews size
				view.SizeRequested += delegate(object o, SizeRequestedArgs args) {
					canvasWidget.Width = args.Requisition.Width / canvasWidget.Canvas.PixelsPerUnit + 20;
					canvasWidget.Height = args.Requisition.Height / canvasWidget.Canvas.PixelsPerUnit;
				};

				// set focus to textview for keyboard input
				view.GrabFocus ();

				// handle CTRL/SHIFT/0/1/2/3 keys
				view.KeyPressEvent += TextView_KeyPress;
				view.KeyReleaseEvent += TextView_KeyRelease;

				// handle TAB key and ENTER key
				view.Buffer.Changed += TextView_TextChanged;
				view.FocusOutEvent += delegate(object o, FocusOutEventArgs args) {
					myText.Persist ();
				};

				aJournal.Scaled += Scaled;
			}

			void Update ()
			{
				Scaled ();
				Indent ();
			}

			void Scaled ()
			{
				FontDescription fontDescription = view.Style.FontDescription;
				fontDescription.AbsoluteSize = 1000 * GetFontSizeFromStyle (myText.Style);
				fontDescription.Weight = GetFontWeightFromStyle (myText.Style);
				view.ModifyFont (fontDescription);
			}

			int[] fontSize = new int[] {2, 2, 3, 4};

			int GetStyleFromFontSize (int fontSize, bool fontWeight)
			{
				throw new NotImplementedException ();
			}

			int GetFontSizeFromStyle (uint style)
			{
				return GetFontSizeFromStyle (style, true);
			}

			int GetFontSizeFromStyle (uint style, bool scaled)
			{
				return Convert.ToInt32 (fontSize [style] * 20 * (scaled ? myCanvas.PixelsPerUnit : 1));
			}

			Pango.Weight GetFontWeightFromStyle (uint style)
			{
				return myText.Style == 0 ? Weight.Normal : Weight.Bold;
			}

			public UiText (Canvas canvas, TextElement noteElement)
			{
				myText = noteElement;

				Init (canvas);

				view.Buffer.Text = myText.Text;
				view.CheckResize ();
				canvasWidget.X = myText.X;
				canvasWidget.Y = myText.Y;

				Update ();
			}

			public bool IsH1 ()
			{
				return myText.Style == 3;
			}

			public bool IsH2 ()
			{
				return myText.Style == 2;
			}

			public bool IsH3 ()
			{
				return myText.Style == 1;
			}

			public string Text {
				get { return myText.Text; }
			}

			void TextView_KeyPress (object o, KeyPressEventArgs args)
			{
				EventKey ev = new EventKey (args.Event.Handle);
				if (controlModifierActive) {
					switch (ev.Key) {
					case Gdk.Key.Key_0: // standard
						myText.Style = 0;
						break;
					case Gdk.Key.Key_1: // h1
						myText.Style = 3;
						break;
					case Gdk.Key.Key_2: // h2
						myText.Style = 2;
						break;
					case Gdk.Key.Key_3: // h3
						myText.Style = 1;
						break;
					}

					Update ();
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
				case Gdk.Key.Up: // TODO we need the whole list of elements here. unfortunately, we do not have it. call someone who has.
					if (null != MoveFocus)
						MoveFocus (this, true);
					break;
				case Gdk.Key.Down:
					if (null != MoveFocus)
						MoveFocus (this, false);
					break;
				}
			}

			public delegate void MoveFocusRequest (UiText sender,bool up);

			public event MoveFocusRequest MoveFocus;

			public void ForceFocus ()
			{
				view.GrabFocus ();
			}

			void TextView_TextChanged (object sender, EventArgs e)
			{
				if (!shiftModifierActive && view.Buffer.Text.EndsWith ("\n")) {
					// trim the newline at the end of the string
					view.Buffer.Text = view.Buffer.Text.Substring (0, view.Buffer.Text.Length - 1);

					// trigger new textbox
					aJournal.currentTool.Reset ();
					aJournal.currentTool.Start (myText.X, myText.Y + GetFontSizeFromStyle (myText.Style) * 2 + 30);
				}

				// itemize
				if (view.Buffer.Text.Contains ("\t")) {
					// trim the newline at the end of the string
					view.Buffer.Text = view.Buffer.Text.Replace ("\t", "");

					int direction = 0;
					if (shiftModifierActive)
						direction = -1;
					else
						direction = 1;

					if (myText.IndentationLevel + direction < 0)
						direction = 0;

					myText.IndentationLevel += direction;

					Indent ();
				}

				myText.Text = view.Buffer.Text;
			}

			void Indent ()
			{
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

					// move to appropriate position
					double offsetx = myText.X + (myText.IndentationLevel - 1) * GetFontSizeFromStyle (myText.Style, false) * 2;
					double offsety = myText.Y;

					itemize.X1 = offsetx + GetFontSizeFromStyle (myText.Style, false) * 1 / 3;
					itemize.X2 = offsetx + GetFontSizeFromStyle (myText.Style, false) * 2 / 3;
					itemize.Y1 = offsety + GetFontSizeFromStyle (myText.Style, false) * 1 / 3;
					itemize.Y2 = offsety + GetFontSizeFromStyle (myText.Style, false) * 2 / 3;
					itemize.FillColor = "black";
				}

				// indent text
				canvasWidget.X = myText.X + myText.IndentationLevel * GetFontSizeFromStyle (myText.Style, false) * 2;
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
				canvasWidget.X += diffx;
				canvasWidget.Y += diffy;

				try {
					itemize.X1 += diffx;
					itemize.X2 += diffx;
					itemize.Y1 += diffy;
					itemize.Y2 += diffy;
				} catch (NullReferenceException) {
				}

				myText.X += Convert.ToInt32 (diffx);
				myText.Y += Convert.ToInt32 (diffy);
			}

			public double Y {
				get{ return myText.Y;}
			}

			public override void EditComleted ()
			{
				myText.Persist ();
			}

			public override void Destroy ()
			{
				canvasWidget.Destroy ();
				if (null != itemize)
					itemize.Destroy ();

				myText.Remove ();
			}

			public override NoteElement Model {
				get { return myText;}
			}

			public override void Hide ()
			{
				canvasWidget.Destroy ();
				if (null != itemize)
					itemize.Destroy ();
			}
		}

		public class UiImage : UiNoteElement
		{
			CanvasPixbuf canvasPixbuf;
			Pixbuf myPixbuf;
			ImageElement myImage;

			UiImage ()
			{
				myImage = new ImageElement ();
			}

			public UiImage (Canvas canvas, ImageElement imageElement) : this()
			{
				myImage = imageElement;

				Byte[] image = Convert.FromBase64String (myImage.Image);
				myPixbuf = new Pixbuf (image);

				canvasPixbuf = new CanvasPixbuf (canvas.Root ());
				canvasPixbuf.X = myImage.X;
				canvasPixbuf.Y = myImage.Y;
				canvasPixbuf.Pixbuf = myPixbuf.ScaleSimple (myImage.Width, myImage.Height, InterpType.Bilinear);
			}

			public UiImage (Canvas canvas, String path, double x, double y) : this()
			{
				myImage.LoadFromFile (path);

				Byte[] image = Convert.FromBase64String (myImage.Image);
				myPixbuf = new Pixbuf (image);

				myImage.Width = myPixbuf.Width;
				myImage.Height = myPixbuf.Height;
				myImage.X = Convert.ToInt32 (x);
				myImage.Y = Convert.ToInt32 (y);

				canvasPixbuf = new CanvasPixbuf (canvas.Root ());
				canvasPixbuf.X = myImage.X;
				canvasPixbuf.Y = myImage.Y;

				// fit image in remaining horizontal space if too large
				if (myImage.X + myImage.Width > UiNote.width) {
					double newWidth = (UiNote.width - myImage.X) * 0.9;
					myImage.Height = Convert.ToInt32 (myImage.Height * newWidth / myImage.Width);
					myImage.Width = Convert.ToInt32 (newWidth);
				}
				canvasPixbuf.Pixbuf = myPixbuf.ScaleSimple (myImage.Width, myImage.Height, InterpType.Bilinear);

				myImage.Persist ();
			}

			public override BoundingBox BoundingBox ()
			{
				double cx1, cx2, cy1, cy2;
				canvasPixbuf.GetBounds (out cx1, out cy1, out cx2, out cy2);

				return new BoundingBox (cx1, cy1, cx2, cy2);
			}

			public void Resize (double scale)
			{
				myImage.Width = Convert.ToInt32 (myImage.Width * scale);
				myImage.Height = Convert.ToInt32 (myImage.Height * scale);

				canvasPixbuf.Pixbuf = myPixbuf.ScaleSimple (myImage.Width, myImage.Height, InterpType.Bilinear);
			}

			public override void Move (double diffx, double diffy)
			{
				canvasPixbuf.Move (diffx, diffy);

				// the cavnvasPixbuf location is not updated by now so we have to do it manually
				myImage.X += Convert.ToInt32 (diffx);
				myImage.Y += Convert.ToInt32 (diffy);
			}

			public override void EditComleted ()
			{
				myImage.Persist ();
			}

			public override void Destroy ()
			{
				canvasPixbuf.Destroy ();
				myImage.Remove ();
			}

			public override NoteElement Model {
				get { return myImage;}
			}

			public override void Hide ()
			{
				canvasPixbuf.Destroy ();
			}
		}
	}
}
