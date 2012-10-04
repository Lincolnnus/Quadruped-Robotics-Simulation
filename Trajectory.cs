using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace QuadrupedRobot
{
    public partial class Trajectory : Form
    {
        public Trajectory()
        {
            InitializeComponent();
        }
        public void DrawChart(int i,int x1,int y1,int x2, int y2,int x3,int y3)
        {
            System.Drawing.Pen myPen = new System.Drawing.Pen(System.Drawing.Color.Blue);
            System.Drawing.Graphics formGraphics;
            formGraphics = this.CreateGraphics();
            if (i==1)
            {
                formGraphics.DrawLine(myPen, 77+x1,103-y1, 77+x2,103- y2);
                formGraphics.DrawLine(myPen, 77 + x2, 103-y2, 77 + x3, 103-y3);
            }
            else if (i == 3)
            {
                formGraphics.DrawLine(myPen,77+ x1, 200-y1, 77+x2, 200-y2);
                formGraphics.DrawLine(myPen, 77 + x2, 200 - y2, 77 + x3, 200 - y3);
            }
            else if (i == 0)
            {
                formGraphics.DrawLine(myPen, 77+x1, 296 - y1, 77+x2, 296 - y2);
                formGraphics.DrawLine(myPen, 77 + x2, 296 - y2, 77 + x3, 296 - y3);
            }
            else if (i == 2)
            {
                formGraphics.DrawLine(myPen, 77+x1, 392 - y1, 77+x2, 392 - y2);
                formGraphics.DrawLine(myPen, 77 + x2, 392 - y2, 77 + x3, 392 - y3);
            }
            myPen.Dispose();
            formGraphics.Dispose();
        }
    }
}
