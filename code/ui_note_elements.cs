using System;
using System.Linq;
using System.Collections.Generic;
using Gnome;
using Gtk;
using Gdk;
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
			CanvasWidget canvasText;
			TextView view;

			public UiText (Canvas canvas)
			{

				canvasText = new CanvasWidget (canvas.Root ());
				canvasText.Width = 200;
				canvasText.Height = 200;

				view = new TextView ();
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
			}

			public override void Destroy ()
			{
				canvasText.Destroy ();
				// TODO delete in backend
			}
		}
	}
}
