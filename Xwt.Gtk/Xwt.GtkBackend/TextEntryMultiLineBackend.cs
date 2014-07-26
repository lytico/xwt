﻿//
// TextEntryMultiLineBackend.cs
//
// Author:
//       Lytico (http://www.limada.org)
//
// Copyright (c) 2014 http://www.limada.org
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
using Xwt.Backends;
using Xwt.Drawing;

namespace Xwt.GtkBackend
{
	/// <summary>
	/// MultiLine-TextEntryBackend
	/// uses Gtk.TextView instead of Gtk.Entry
	/// </summary>
	public class TextEntryMultiLineBackend : WidgetBackend, ITextEntryBackend
	{
		string placeholderText;
		Pango.Layout layout = null;
		Gtk.TextView textView;
		bool multiLine;

		public string Text {
			get { return TextView.Buffer.Text; }
			set {
				bufferSizeRequest = true;
				TextView.Buffer.Text = value;
			}
		}

		public Alignment TextAlignment {
			get { return TextView.Justification.ToXwtValue (); }
			set { TextView.Justification = value.ToGtkValue (); }
		}

		public bool ReadOnly {
			get { return TextView.Editable; }
			set {
				TextView.Editable = value;
				TextView.CursorVisible = !value;
			}
		}

		public bool MultiLine {
			get { return multiLine; }
			set {
				if (value != multiLine) {
					multiLine = value;
					if (!value) {
						TextView.WrapMode = Gtk.WrapMode.None;
					} else {
						TextView.WrapMode = Gtk.WrapMode.Word;
					}

				}
			}
		}

		public bool ShowFrame {
			get { return ((Gtk.Frame)Widget).ShadowType != Gtk.ShadowType.None; }
			set {
				if (value)
					((Gtk.Frame)Widget).ShadowType = Gtk.ShadowType.In;
				else
					((Gtk.Frame)Widget).ShadowType = Gtk.ShadowType.None;
			}
		}

		protected virtual Gtk.TextView TextView {
			get { return textView; }
		}

		public string PlaceholderText {
			get { return placeholderText; }
			set {
				if (placeholderText != value) {
					if (placeholderText == null)
						TextView.ExposeEvent += RenderPlaceholderText;
					else if (value == null)
						TextView.ExposeEvent -= RenderPlaceholderText;
				}
				placeholderText = value;
			}
		}

		protected Pango.Layout Layout {
			get { return layout ?? (layout = new Pango.Layout (TextView.PangoContext)); }
		}

		protected virtual void RenderPlaceholderText (object o, Gtk.ExposeEventArgs args)
		{
			TextView.RenderPlaceholderText (args, PlaceholderText, ref layout);
		}

		public override object Font {
			get { return base.Font; }
			set {
				base.Font = value;
				TextView.ModifyFont ((Pango.FontDescription)value);
				xLayout = null;
				layout = null;
			}
		}

		public override void Initialize ()
		{
			textView = new Gtk.TextView ();
			Widget = new Gtk.Frame ();
			((Gtk.Frame)Widget).Add (textView);
			((Gtk.Frame)Widget).ShadowType = Gtk.ShadowType.In;
			Widget.ShowAll ();
			ShowFrame = true;
			InitializeMultiLine ();
		}

		#region Multiline-Handling

		bool bufferSizeRequest = false;
		int lineHeight = -1;
		Pango.Layout xLayout = null;

		protected Pango.Layout XLayout {
			get { return xLayout ?? (xLayout = TextView.CreatePangoLayout ("X")); }
		}

		protected virtual void InitializeMultiLine ()
		{
			var w = 0;
			var lastHeight = -1;

			TextView.SizeRequested += (s, args) => {
				if (!MultiLine)
					args.Requisition = new Gtk.Requisition {
						Width = -1,
						Height = lineHeight
					};
			};

			TextView.SizeAllocated += (s, e) => {
				if (!MultiLine && lastHeight != e.Allocation.Height) {
					lastHeight = e.Allocation.Height;
					XLayout.GetPixelSize (out w, out lineHeight);
					if (lastHeight > lineHeight)
						TextView.PixelsAboveLines = (int)((lastHeight - lineHeight) / 2d);
				}
			};

			TextView.Buffer.Changed += (s, e) => {
				bufferSizeRequest = true;
			};
		}

		public override Size GetPreferredSize (SizeConstraint widthConstraint, SizeConstraint heightConstraint)
		{
			if (MultiLine)
				return base.GetPreferredSize (widthConstraint, heightConstraint);

			if (bufferSizeRequest) {
				bufferSizeRequest = !widthConstraint.IsConstrained;
				return new Size (Widget.Allocation.Width, Widget.Allocation.Height);
			}

			return base.GetPreferredSize (widthConstraint, heightConstraint);
		}

		#endregion

		#region Cursor and Selection

		public int CursorPosition {
			get {
				var iter = TextView.Buffer.GetIterAtMark (TextView.Buffer.InsertMark);
				return iter.Offset;
			}
			set {
				var iter = TextView.Buffer.GetIterAtOffset (value);
				TextView.Buffer.PlaceCursor (iter);
			}
		}

		public int SelectionStart {
			get {
				var start = new Gtk.TextIter ();
				var end = start;
				TextView.Buffer.GetSelectionBounds (out start, out end);
				return start.Offset;

			}
			set {
				var start = new Gtk.TextIter ();
				var end = start;
				TextView.Buffer.GetSelectionBounds (out start, out end);
				var cacheLength = end.Offset - start.Offset;
				start.Offset = value;
				end.Offset = value + cacheLength;
				TextView.GrabFocus ();
				TextView.Buffer.SelectRange (start, end);
				HandleSelectionChanged ();
			}
		}

		public int SelectionLength {
			get {
				var start = new Gtk.TextIter ();
				var end = start;
				if (!TextView.Buffer.GetSelectionBounds (out start, out end))
					return 0;
				return end.Offset - start.Offset;

			}
			set {
				var start = new Gtk.TextIter ();
				var end = start;
				if (!TextView.Buffer.GetSelectionBounds (out start, out end)) {
					start = TextView.Buffer.GetIterAtMark (TextView.Buffer.InsertMark);
					end = start;
				}
				end.Offset = start.Offset + value;
				TextView.GrabFocus ();
				TextView.Buffer.SelectRange (start, end);
				HandleSelectionChanged ();
			}
		}

		public string SelectedText {
			get {
				var start = new Gtk.TextIter ();
				var end = start;

				if (!TextView.Buffer.GetSelectionBounds (out start, out end))
					return "";
				return TextView.Buffer.GetText (start, end, true);
			}
			set {
				var start = new Gtk.TextIter ();
				var end = start;
				var cachedOffset = 0;
				if (!TextView.Buffer.GetSelectionBounds (out start, out end)) {
					start = TextView.Buffer.GetIterAtMark (TextView.Buffer.InsertMark);
					cachedOffset = start.Offset;
				} else {
					cachedOffset = start.Offset;
					TextView.Buffer.DeleteSelection (true, true);
					start = TextView.Buffer.GetIterAtOffset (cachedOffset);
				}
				TextView.Buffer.Insert (ref start, value);
				start.Offset = cachedOffset;
				end = start;
				end.Offset = start.Offset + value.Length;
				TextView.GrabFocus ();
				TextView.Buffer.SelectRange (start, end);
			}
		}

		#endregion

		#region Eventhandling

		protected new ITextEntryEventSink EventSink {
			get { return (ITextEntryEventSink)base.EventSink; }
		}

		public override void EnableEvent (object eventId)
		{
			base.EnableEvent (eventId);
			if (eventId is TextEntryEvent) {
				switch ((TextEntryEvent)eventId) {
				case TextEntryEvent.Changed:
					TextView.Buffer.Changed += HandleChanged;
					break;
				case TextEntryEvent.Activated:
						TextView.KeyPressEvent += HandleActivated;
					break;
				case TextEntryEvent.SelectionChanged:
					enableSelectionChangedEvent = true;
						TextView.MoveCursor += HandleMoveCursor;
						TextView.ButtonPressEvent += HandleButtonPressEvent;
						TextView.ButtonReleaseEvent += HandleButtonReleaseEvent;
						TextView.MotionNotifyEvent += HandleMotionNotifyEvent;
					break;
				}
			}
		}

		public override void DisableEvent (object eventId)
		{
			base.DisableEvent (eventId);
			if (eventId is TextEntryEvent) {
				switch ((TextEntryEvent)eventId) {
				case TextEntryEvent.Changed:
					TextView.Buffer.Changed -= HandleChanged;
					break;
				case TextEntryEvent.Activated:
						TextView.KeyPressEvent -= HandleActivated;
					break;
				case TextEntryEvent.SelectionChanged:
					enableSelectionChangedEvent = false;
						TextView.MoveCursor -= HandleMoveCursor;
						TextView.ButtonPressEvent -= HandleButtonPressEvent;
						TextView.ButtonReleaseEvent -= HandleButtonReleaseEvent;
						TextView.MotionNotifyEvent -= HandleMotionNotifyEvent;
					break;
				}
			}
		}

		void HandleChanged (object sender, EventArgs e)
		{
			ApplicationContext.InvokeUserCode (delegate {
				EventSink.OnChanged ();
				EventSink.OnSelectionChanged ();
			});
		}

		void HandleActivated (object sender, Gtk.KeyPressEventArgs e)
		{
			if (e.Event.Key == Gdk.Key.Return || e.Event.Key == Gdk.Key.ISO_Enter)
				ApplicationContext.InvokeUserCode (delegate {
					EventSink.OnActivated ();
				});
		}

		bool enableSelectionChangedEvent;

		void HandleSelectionChanged ()
		{
			if (enableSelectionChangedEvent)
				ApplicationContext.InvokeUserCode (delegate {
					EventSink.OnSelectionChanged ();
				});
		}

		void HandleMoveCursor (object sender, EventArgs e)
		{
			HandleSelectionChanged ();
		}

		int cacheSelectionStart, cacheSelectionLength;
		bool isMouseSelection;

		[GLib.ConnectBefore]
		void HandleButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			if (args.Event.Button == 1) {
				HandleSelectionChanged ();
				cacheSelectionStart = SelectionStart;
				cacheSelectionLength = SelectionLength;
				isMouseSelection = true;
			}
		}

		[GLib.ConnectBefore]
		void HandleMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{
			if (isMouseSelection)
			if (cacheSelectionStart != SelectionStart || cacheSelectionLength != SelectionLength)
				HandleSelectionChanged ();
			cacheSelectionStart = SelectionStart;
			cacheSelectionLength = SelectionLength;
		}

		[GLib.ConnectBefore]
		void HandleButtonReleaseEvent (object o, Gtk.ButtonReleaseEventArgs args)
		{
			if (args.Event.Button == 1) {
				isMouseSelection = false;
				HandleSelectionChanged ();
			}
		}

		#endregion

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				if (xLayout != null)
					xLayout.Dispose ();
				xLayout = null;

				if (layout != null)
					layout.Dispose ();
				layout = null;
			}
			base.Dispose (disposing);
		}
	}
}