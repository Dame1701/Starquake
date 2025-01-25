/*
using System.Drawing;

namespace StarquakeXNA
{
    public partial class SplashProgressBar : UserControl
    {
        private int value = 0;
        public int Value
        {
            get { return this.value; }
            set 
            { 
                this.value = value;
                this.Invalidate();
            }
        }

        private Color colourGradient1 = Color.FromArgb(200, 0, 0, 60);
        public Color ColourGradient1
        {
            get { return colourGradient1; }
            set 
            { 
                colourGradient1 = value;
                Invalidate();
            }
        }

        private Color colourGradient2 = Color.FromArgb(200, 87, 40, 255);
        public Color ColourGradient2
        {
            get { return colourGradient2; }
            set 
            { 
                colourGradient2 = value;
                Invalidate();
            }
        }

        public SplashProgressBar()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            int barWidth = (int)((((double)this.Width - 7) / 100) * value);

            if (barWidth > 0)
            {
                using (LinearGradientBrush sb = new LinearGradientBrush(
                    new Rectangle(
                            4,
                            4,
                            this.Width - 7,
                            this.Height - 7), colourGradient1, colourGradient2, 0f))
                {
                    e.Graphics.FillRectangle(sb, new Rectangle(
                            4,
                            4,
                            barWidth,
                            this.Height - 7));

                }
            }

            using (SolidBrush sb = new SolidBrush(Color.FromArgb(60,60,100)))
            {
                using (Pen p = new Pen(sb, 1))
                {
                    e.Graphics.DrawRectangle(p, new Rectangle(
                        2,
                        2,
                        this.Width - 4,
                        this.Height - 4));

                }
            }

            base.OnPaint(e);
        }
    }
}
*/