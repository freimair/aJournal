using System;
using System.Linq;
using System.Collections.Generic;
using Gnome;
using Gtk;
using Gdk;
using Pango;
using backend;

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

			public List<int> Points { get { return linemodel.Points; } }

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
				// TODO destroy in backend as well
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

			CanvasWidget canvasText;
			TextView view;
			CanvasRE itemize;

			bool controlModifierActive = false;
			bool shiftModifierActive = false;

			public UiText (Canvas canvas)
			{


				canvasText = new CanvasWidget (canvas.Root ());

				view = new TextView ();
				// reset text style
				FontDescription fontDescription = view.Style.FontDescription;
				fontDescription.Size = FontSize.Normal;
				fontDescription.Weight = Weight.Normal;
				view.ModifyFont (fontDescription);

				// do further configuration
				view.CursorVisible = true;
				view.ResizeMode = ResizeMode.Immediate;

				canvasText.Widget = view;
				view.Editable = true;
				view.Show ();

				view.SizeRequested += delegate(object o, SizeRequestedArgs args) {
					canvasText.Width = args.Requisition.Width / canvas.PixelsPerUnit + 20;
					canvasText.Height = args.Requisition.Height / canvas.PixelsPerUnit;
				};
				view.GrabFocus ();

				view.KeyPressEvent += delegate(object o, KeyPressEventArgs args) {
					EventKey ev = new EventKey (args.Event.Handle);
					if (controlModifierActive) {
						fontDescription = view.Style.FontDescription;
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
					}

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
				};

				// initialize new text item below the current one if the Enter-Key was pressed
				view.Buffer.Changed += delegate(object sender, EventArgs e) {
					if (!shiftModifierActive && view.Buffer.Text.EndsWith ("\n")) {
						// trim the newline at the end of the string
						view.Buffer.Text = view.Buffer.Text.Substring (0, view.Buffer.Text.Length - 1);

						// trigger new textbox
						aJournal.currentTool.Reset ();
						aJournal.currentTool.Start (canvasText.X, canvasText.Y + canvasText.Height + 30);
					}

					// itemize
					if (!shiftModifierActive && view.Buffer.Text.Contains ("\t")) {
						// trim the newline at the end of the string
						view.Buffer.Text = view.Buffer.Text.Replace ("\t", "");

						if (null == itemize) { // fresh itemize
							itemize = new CanvasEllipse (canvas.Root ());
							itemize.X1 = canvasText.Height / 3;
							itemize.X2 = 2 * canvasText.Height / 3;
							itemize.Y1 = canvasText.Height / 3;
							itemize.Y2 = 2 * canvasText.Height / 3;
							itemize.WidthUnits = 2;
							itemize.FillColor = "black";
							itemize.Move (canvasText.X, canvasText.Y);
							canvasText.Move (80, 0);
						} else if (itemize is CanvasEllipse) {
							itemize.Destroy ();
							itemize = new CanvasRect (canvas.Root ());
							itemize.X1 = canvasText.Height / 3;
							itemize.X2 = 2 * canvasText.Height / 3;
							itemize.Y1 = canvasText.Height / 3;
							itemize.Y2 = 2 * canvasText.Height / 3;
							itemize.WidthUnits = 2;
							itemize.FillColor = "black";
							itemize.Move (canvasText.X + 80, canvasText.Y);
							canvasText.Move (80, 0);
						}
					} else if (shiftModifierActive && view.Buffer.Text.Contains ("\t")) {
						// trim the newline at the end of the string
						view.Buffer.Text = view.Buffer.Text.Replace ("\t", "");

						if (itemize is CanvasEllipse) {
							itemize.Destroy ();
							itemize = null;
							canvasText.Move (-80, 0);
						} else if (itemize is CanvasRect) {
							itemize.Destroy ();
							itemize = new CanvasEllipse (canvas.Root ());
							itemize.X1 = canvasText.Height / 3;
							itemize.X2 = 2 * canvasText.Height / 3;
							itemize.Y1 = canvasText.Height / 3;
							itemize.Y2 = 2 * canvasText.Height / 3;
							itemize.WidthUnits = 2;
							itemize.FillColor = "black";
							itemize.Move (canvasText.X, canvasText.Y);
							canvasText.Move (-80, 0);
						}
					}
				};

				view.KeyReleaseEvent += delegate(object o, KeyReleaseEventArgs args) {
					EventKey ev = new EventKey (args.Event.Handle);
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
				};
			}

			public bool Empty {
				get { return view.Buffer.CharCount == 0;}
			}

			public override BoundingBox BoundingBox ()
			{
				double cx1, cx2, cy1, cy2;
				canvasText.GetBounds (out cx1, out cy1, out cx2, out cy2);

				return new BoundingBox (cx1, cy1, cx2, cy2);
			}

			public override void Move (double diffx, double diffy)
			{
				canvasText.X += diffx;
				canvasText.Y += diffy;

				try {
					itemize.Move (diffx, diffy);
				} catch (NullReferenceException) {
				}
			}

			public override void Destroy ()
			{
				canvasText.Destroy ();
				// TODO delete in backend
			}
		}
	}
}
