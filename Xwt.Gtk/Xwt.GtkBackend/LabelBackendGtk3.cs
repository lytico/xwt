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
using Cairo;

namespace Xwt.GtkBackend
{
	public partial class LabelBackend
	{
		public class GtkSizedLabel : Gtk.Label {

			protected override bool OnDrawn (Context cr) {
				var bounds = cr.ClipExtents ();
				if (this.WidthRequest > 0) {
				    bounds = new Cairo.Rectangle (bounds.X, bounds.Y, WidthRequest, bounds.Height);
				    cr.Rectangle (bounds);
				    cr.Clip ();
				}


				return base.OnDrawn (cr);
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
	}
}

