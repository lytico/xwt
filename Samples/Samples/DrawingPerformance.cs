// 
// DrawingText.cs
//  
// Author:
//       Lytico (http://limada.sourceforge.net)
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc
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
using Xwt;
using Xwt.Drawing;
using System.Diagnostics;

namespace Samples
{
	public class DrawingPerformance: VBox
	{
		public class Painter:Drawings
		{
			public Painter(){
				Stopwatch = new Stopwatch();
				Iterations = 200;
			}
			protected override void OnDraw (Context ctx)
			{
				base.OnDraw (ctx);
				
				Stopwatch.Start();
				
				SpeedTest (ctx, 5, 5);
				
				Stopwatch.Stop();
				Frames++;
				Trace.WriteLine(string.Format("DrawingPerformance Frames {0}",Frames));
			}
		
			public string Run ()
			{
				//Frames = 1;
				//Stopwatch.Reset();
				Stopwatch.Start();
				int i=0;
				while(i++<Iterations){
					//?? how to force redraw??
//					this.Hide();
//					this.Show();
					this.QueueDraw();
				}
				var ms = Stopwatch.ElapsedMilliseconds;
				if(ms==0)
					ms = 1;
				return string.Format("Time in sec {1}\tFrames per sec {2}\tIterations {0}\tFrames {3}",
				                     Iterations,
				                     ms/1000d,
				                     Frames/(ms/1000d),
				                     Frames);
				
				//Frames = 1;
				Stopwatch.Reset();
			}
			
			public int Frames { get; set; }
			public int Iterations { get; set; }
			public Stopwatch Stopwatch { get; set; }

			public virtual void SpeedTest (Xwt.Drawing.Context ctx, double sx, double sy)
			{
				ctx.Save ();
            
				ctx.Translate (sx, sy);
		
			
				var n = 1000;
				var alpha = Math.PI * 2 / n;
				var radius = 50;
				for (int i = 1; i<n; i++) {
					ctx.MoveTo (100, 100);
					var theta = alpha * i;
					ctx.SetColor (Colors.Black);
					var p = new Point (Math.Cos (theta) * radius, Math.Sin (theta) * radius);
					ctx.LineTo (100 + p.X, 100 + p.Y);
					ctx.Stroke ();
				
				}
			
				ctx.Restore ();

			}		
		}
		
		public DrawingPerformance ()
		{
			var drawing = new Painter();
			var b1 = new Button ("start");
			b1.Clicked += delegate {
				b1.Label = drawing.Run();
			};
			
			PackEnd (b1);
			PackStart(drawing,BoxMode.FillAndExpand);
			
		}
		
		

	}
}

