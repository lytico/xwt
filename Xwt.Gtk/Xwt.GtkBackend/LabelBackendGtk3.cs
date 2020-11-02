//
// LabelBackendGtk3.cs
//
// Author:
//       Vsevolod Kukol <v.kukol@rubologic.de>
//
// Copyright (c) 2014 Vsevolod Kukol
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using Gtk;
using Context = Cairo.Context;

namespace Xwt.GtkBackend
{
	public partial class LabelBackend
	{
		public class GtkSizedLabel : Gtk.Label {

			protected override bool OnDrawn (Context cr) {
				var bounds = cr.ClipExtents ();
				if (this.WidthRequest > 0) {
					bounds = new Cairo.Rectangle (bounds.X, bounds.Y, PixelWidth > 0 ? PixelWidth : bounds.Width, bounds.Height);
				    cr.Rectangle (bounds);
				    cr.Clip ();
				}

				return base.OnDrawn (cr);
			}

			private int _pixelWidth = -1;

			public int PixelWidth {
				get => _pixelWidth;
				set {
					_pixelWidth = value;
					MaxWidthChars = CalculateWidthChars (_pixelWidth);
					Ellipsize = Pango.EllipsizeMode.End;
					LineWrapMode = Pango.WrapMode.Char;
				}
			}

			public int CalculateWidthChars (int pixelWidth)
			{
				int LineWidth (Pango.LayoutLine l)
				{
					var i = new Pango.Rectangle ();
					var lo = new Pango.Rectangle ();
					l.GetExtents (ref i, ref lo);
					return i.Width;
				}

				IEnumerable<(int index, int width)> CharWidths (Pango.LayoutIter iter)
				{
					while (iter.NextChar ()) {
						var x = iter.CharExtents;
						yield return (iter.Index, x.Width);
					}
				}

				var max = this.Layout.LinesReadOnly.Aggregate ((i1, i2) => LineWidth (i1) > LineWidth (i2) ? i1 : i2);
				using var measure = Layout.Copy ();
				measure.Ellipsize = Pango.EllipsizeMode.None;
				measure.Wrap = Pango.WrapMode.Char;
				using var iter = measure.Iter;
				var lls = CharWidths (iter)
					.OrderBy (cw => cw.index)
					.ToArray ();
				var iLen = 0;
				foreach (var cwi in lls) {
					iLen += cwi.width;
					if (iLen > pixelWidth * Pango.Scale.PangoScale) {
						return cwi.index - 1;
					}
				}

				return -1;
			}
		}

		void SetAlignmentGtk ()
		{
			switch (TextAlignment) {
				case Alignment.Start:
					Label.Xalign = 0f;
					break;
				case Alignment.End:
					Label.Xalign = 1;
					break;
				case Alignment.Center:
					Label.Xalign = 0.5f;
					break;
			}
		}

		void ToggleSizeCheckEventsForWrap (WrapMode wrapMode)
		{}

		public override Size GetPreferredSize (SizeConstraint widthConstraint, SizeConstraint heightConstraint) {
			var result= base.GetPreferredSize (widthConstraint, heightConstraint);
			if (Widget.RequestMode == SizeRequestMode.ConstantSize && widthConstraint.IsConstrained && Widget is GtkSizedLabel sizedLabel) {
				sizedLabel.PixelWidth = (int)widthConstraint.AvailableSize;
			}

			return result;
		}
	}
}

