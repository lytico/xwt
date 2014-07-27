﻿//
// TextEntryMultiLineBackend.cs
//
// Author:
//       Lytico (http://www.limada.org)
//       Vsevolod Kukol <sevo@sevo.org>
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

namespace Xwt.GtkBackend
{
	/// <summary>
	/// MultiLine-TextEntryBackend
	/// uses Gtk.TextView instead of Gtk.Entry
	/// </summary>
	public class TextEntryMultiLineBackend : WidgetBackend, ITextEntryBackend
	{
		string placeholderText;
		Pango.Layout layout;
		GtkMultilineTextEntry textView;

		public string Text {
			get { return TextView.Buffer.Text; }
			set { TextView.Buffer.Text = value; }
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
			get { return TextView.MultiLine; }
			set { TextView.MultiLine = value; }
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

		protected virtual GtkMultilineTextEntry TextView {
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
				layout = null;
			}
		}

		public override void Initialize ()
		{
			textView = new GtkMultilineTextEntry ();
			Widget = new Gtk.Frame ();
			((Gtk.Frame)Widget).Add (textView);
			ShowFrame = true;
			Widget.ShowAll ();
		}

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
				Gtk.TextIter start, end;
				TextView.Buffer.GetSelectionBounds (out start, out end);
				return start.Offset;
			}
			set {
				Gtk.TextIter start, end;
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
				Gtk.TextIter start, end;
				if (!TextView.Buffer.GetSelectionBounds (out start, out end))
					return 0;
				return end.Offset - start.Offset;

			}
			set {
				Gtk.TextIter start, end;
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
				Gtk.TextIter start, end;
				if (!TextView.Buffer.GetSelectionBounds (out start, out end))
					return String.Empty;
				return TextView.Buffer.GetText (start, end, true);
			}
			set {
				Gtk.TextIter start, end;
				int cachedOffset;
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
						TextView.KeyPressEvent += HandleKeyPress;
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
						TextView.KeyPressEvent -= HandleKeyPress;
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

		[GLib.ConnectBefore]
		void HandleKeyPress (object sender, Gtk.KeyPressEventArgs e)
		{
			if ((e.Event.Key == Gdk.Key.Return ||
			     e.Event.Key == Gdk.Key.ISO_Enter ||
			     e.Event.Key == Gdk.Key.KP_Enter) && !MultiLine)
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
				if (layout != null)
					layout.Dispose ();
				layout = null;
			}
			base.Dispose (disposing);
		}
	}

	public class GtkMultilineTextEntry : Gtk.TextView
	{
		bool multiline;
		int lastHeight = -1;
		int lineHeight = -1;
		Pango.Layout xLayout;

		public bool MultiLine {
			get {
				return multiline;
			}
			set {
				if (multiline != value) {
					multiline = value;
					if (!value) {
						WrapMode = Gtk.WrapMode.None;
						PixelsAboveLines = 3;
						PixelsBelowLines = 3;
					} else {
						WrapMode = Gtk.WrapMode.Word;
						PixelsAboveLines = 0;
						PixelsBelowLines = 0;
					}
				}
			}
		}

		public GtkMultilineTextEntry()
		{
			Indent = 3;
			PixelsAboveLines = 3;
			PixelsBelowLines = 3;
			int w;
			XLayout.GetPixelSize (out w, out lineHeight);
			lineHeight += 6;
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			requisition.Width = 150; // mimic Gtk.Entry, which has a min width
			if (!MultiLine)
				requisition.Height = lineHeight;
		}

		protected Pango.Layout XLayout {
			get { return xLayout ?? (xLayout = CreatePangoLayout ("X")); }
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			int width;
			if (!MultiLine && lastHeight != allocation.Height) {
				lastHeight = allocation.Height;
				XLayout.GetPixelSize (out width, out lineHeight);
				lineHeight += 6;
			}
			base.OnSizeAllocated (allocation);
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if ((evnt.Key == Gdk.Key.Return ||
			     evnt.Key == Gdk.Key.ISO_Enter ||
			     evnt.Key == Gdk.Key.KP_Enter) && !MultiLine)
				return true;
			return base.OnKeyPressEvent (evnt);
		}

		public override void Dispose ()
		{
			if (xLayout != null)
				xLayout.Dispose ();
			xLayout = null;
			base.Dispose ();
		}
	}
}