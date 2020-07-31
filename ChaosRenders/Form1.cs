using FastBitmapLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChaosRenders {
    public partial class Form1 : Form {
        Graphics gr;
        public Form1()
        {
            InitializeComponent();

            this.textBox1.Text = "-1.7";
            this.textBox2.Text = "1.8";
            this.textBox3.Text = "-0.9";
            this.textBox4.Text = "-0.4";
            this.textBox5.Text = ".01";

            //gr = Graphics.FromImage(bitmap);
        }

        // handle equation shift
        PointF lastPoint = new PointF(1, 1);
        double a = -1.7, b = 1.8, c = -.9, d = -.4;
        double newa = -1.7, newb = 1.8, newc = -.9, newd = -.4;
        double newah = -1.5, newbh = 2, newch = -.8, newdh = -.3;
        bool aup = false, bup = false, cup = false, dup = false;
        double newDelta = .01;
        int currPos = 0;
        bool useGumowskiMira = false;
        bool useHopalong = false;
        double w = 0;
        void HandleShiftLorenz()
        {
            double x = lastPoint.X;
            double y = lastPoint.Y;

            double xnew;
            double ynew;

            if (useHopalong)
            {
                xnew = y - 1 - Math.Sqrt(Math.Abs(b * x - 1 - c)) * Math.Sign(x - 1);
                ynew = a - x - 1;
            }
            else if (useGumowskiMira)
            {
                double t = x;
                xnew = b * y + w;
                w = a * x + (1 - a) * 2 * x * x / (1 + x * x);
                ynew = w - t;
            }
            else
            {
                xnew = Math.Sin(a * y) + c * Math.Cos(a * x);
                ynew = Math.Sin(b * x) + d * Math.Cos(b * y);
            }


            lastPoint = new PointF((float)xnew, (float)ynew);
            points[currPos] = lastPoint;

            if (++currPos > IterationCount - 1)
            {
                currPos = 0;
            }

            filled = ++filled > IterationCount ? IterationCount : filled;
        }

        int IterationCount = 10000;
        static PointF[] points = new PointF[100000];
        int filled = 0;
        Bitmap bitmap = new Bitmap(1920, 1200);
        int[] lines = Enumerable.Range(0, 1200).ToArray();
        int lastDiff = 30;
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            filled = 0;

            DateTime startTime = DateTime.UtcNow;

            if (dontDraw)
                return;

            aup = (aup || a < newa) && (a < newah);
            bup = (bup || b < newb) && (b < newbh);
            cup = (cup || c < newc) && (c < newch);
            dup = (dup || d < newd) && (d < newdh);

            a = aup ? a + newDelta : a - newDelta;
            b = bup ? b + newDelta : b - newDelta;
            c = cup ? c + newDelta : c - newDelta;
            d = dup ? d + newDelta : d - newDelta;

            this.label1.Text = a.ToString("0.###");
            this.label2.Text = b.ToString("0.###");
            this.label3.Text = c.ToString("0.###");
            this.label4.Text = d.ToString("0.###");

            for (int i = 0; i < IterationCount; i++)
                HandleShiftLorenz();

            var g = e.Graphics;

            Brush brush = new SolidBrush(Color.FromArgb(60, 255, 100, 120));
            Brush drbrush = new SolidBrush(Color.FromArgb(60, 0, 0, 0));

            var pen = new Pen(Color.FromArgb(255, 232, 232));

            var width = this.pictureBox1.Width;
            var height = this.pictureBox1.Height;

            using (var fastBitmap = bitmap.FastLock())
            {

                Parallel.ForEach(lines, (line) =>
                {
                    for (int i = 0; i < 1920; i++)
                    {
                        var tempColor = fastBitmap.GetPixel(i, line);

                    if (!(tempColor.A == 0))
                        fastBitmap.SetPixel(i, line, Color.FromArgb((tempColor.A - (tempColor.A/20)) < 20 ? 0 : tempColor.A - (tempColor.A / 20), 
                            (tempColor.R + 20) >255 ? 0 : tempColor.R +20,
                            (tempColor.G + 10) > 200 ? 0 : tempColor.G + 10,
                            (tempColor.B - 10) > 0 ? tempColor.B -10 : 0));
                    }
                }
                );

                for(int j = 0; j < IterationCount; j++)
                {
                    var point = points[j];
                    var tempx = (int)(width / 2 + 300* point.X);
                    var tempy = (int)(height / 2 + 300*point.Y);
                    if (!(tempx > 0 && tempx < 1920) || !(tempy > 0 && tempy < 1200))
                        continue;

                    var tempColor = fastBitmap.GetPixel(tempx, tempy);

                    fastBitmap.SetPixel(tempx, tempy, Color.FromArgb(tempColor.A + 100>255 ? 255:tempColor.A + 100, 50, 50, 255));
                }
            }
            var startTime1 = DateTime.UtcNow;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.DrawImage(bitmap, 0, 0);
            lastDiff = (DateTime.UtcNow - startTime1).Milliseconds;

            g.DrawString($"points drawn: {filled}", new Font("Arial", 12), brush, 20, 20);

            loaded = true;

            lastDiff = (DateTime.UtcNow - startTime).Milliseconds;

            if (lastDiff < 42 && IterationCount < 100000)
                IterationCount += 10000;
            else if (lastDiff > 40 && IterationCount > 10000)
                IterationCount -= 10000;
        }

        bool loaded = false;

        private void bHigh_TextChanged(object sender, EventArgs e)
        {
            if (!loaded)
                return;
            try
            {
                this.newbh = double.Parse(bHigh.Text);
            }
            catch { }
        }

        private void cHigh_TextChanged(object sender, EventArgs e)
        {
            if (!loaded)
                return;
            try
            {
                this.newch = double.Parse(cHigh.Text);
            }
            catch { }
        }

        private void dHigh_TextChanged(object sender, EventArgs e)
        {
            if (!loaded)
                return;
            try
            {
                this.newdh = double.Parse(dHigh.Text);
            }
            catch { }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (!loaded)
                return;
            try
            {
                this.newa = double.Parse(textBox1.Text);
            }
            catch { }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (!loaded)
                return;
            try
            {
                this.newb = double.Parse(textBox2.Text);
            }
            catch { }
        }

        private void aHigh_TextChanged(object sender, EventArgs e)
        {
            if (!loaded)
                return;
            try
            {
                this.newah = double.Parse(aHigh.Text);
            }
            catch { }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            if (!loaded)
                return;
            try
            {
                this.newc = double.Parse(textBox3.Text);
            }
            catch { }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            if (!loaded)
                return;
            try
            {
                this.newd = double.Parse(textBox4.Text);
            }
            catch { }
        }


        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            if (!loaded)
                return;
            try
            {
                this.newDelta = double.Parse(textBox5.Text);
            }
            catch { }
        }

        public double DegreeToRadian(float angle)
        {
            return Math.PI * angle / 180.0;
        }

        bool dontDraw = true;
        private void Form1_Shown(object sender, EventArgs e)
        {
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            int i = 0;
            while (i++ < 30)
            {
                Application.DoEvents();
                Thread.Sleep(30);
            }
            dontDraw = false;

            while (true)
            {
                this.pictureBox1.Invalidate();
                Application.DoEvents();
            }
        }
    }
}
