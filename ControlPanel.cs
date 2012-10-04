using System;
using System.Collections.Generic;
using System.ComponentModel;
//using System.Data;
//using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace QuadrupedRobot
{
    public partial class ControlPanel : Form
    {
        FromWinformEvents _fromWinformPort;
        float _bodyweight;//body weight
        public float Bodyweight
        {
            get { return _bodyweight; }
            set { _bodyweight = value; }
        }
        float _ulegweight;//upper leg weight

        public float Ulegweight
        {
            get { return _ulegweight; }
            set { _ulegweight = value; }
        }
        float _llegweight;//lower leg weight

        public float Llegweight
        {
            get { return _llegweight; }
            set { _llegweight = value; }
        }
        float _uleglength;//upper leg length

        public float Uleglength
        {
            get { return _uleglength; }
            set { _uleglength = value; }
        }
        float _lleglength;//lower leg length

        public float Lleglength
        {
            get { return _lleglength; }
            set { _lleglength = value; }
        }
        float _height;
        public float Height1
        {
            get { return _height; }
            set { _height = value; }
        }
        float _length;
        public float Length
        {
            get { return _length; }
            set { _length = value; }
        }
        float _width;
        public float Width1
        {
            get { return _width; }
            set { _width = value; }
        }
        float _hipradius;

        public float Hipradius
        {
            get { return _hipradius; }
            set { _hipradius = value; }
        }
        float _kneeradius;
        public float Kneeradius
        {
            get { return _kneeradius; }
            set { _kneeradius = value; }
        }
        float _ankleweight;
        public float Ankleweight
        {
            get { return _ankleweight; }
            set { _ankleweight = value; }
        }
        float _ankleradius;
        public float Ankleradius
        {
            get { return _ankleradius; }
            set { _ankleradius = value; }
        }
        float _hipweight;
        public float Hipweight
        {
            get{return _hipweight;}
            set { _hipweight = value; }
        }
        float _baseweight;
        public float Baseweight
        {
            get { return _baseweight; }
            set { _baseweight = value; }
        }
        float _staticf;
        public float Staticf
        {
            get {return _staticf;}
            set {_staticf=value; }
        }
        float _dynamicf;
        public float Dynamicf
        {
            get { return _dynamicf; }
            set { _dynamicf = value; }
        }
        float _restitution;
        public float Restitution
        {
            get { return _restitution; }
            set { _restitution = value; }
        }
        float _anklex;
        public float Anklex
        {
            get { return _anklex; }
            set { _anklex = value; }
        }
        float _ankley;
        public float Ankley
        {
            get { return _ankley; }
            set { _ankley = value; }
        }
        float _anklez;
        public float Anklez
        {
            get { return _anklez; }
            set { _anklez = value; }
        }
        float _timesec2;
        public float Timesec2
        {
            get { return _timesec2; }
            set { _timesec2 = value; }
        }
        string _leglabel2;
        public string Leglabel2
        {
            get { return _leglabel2; }
            set { _leglabel2 = value; }
        }
        int _relative;
        public int Relative
        {
            get { return _relative; }
            set { _relative = value; }
        }
        float _x;
        public float X
        {
            get { return _x; }
            set { _x = value; }
        }
        float _y;
        public float Y
        {
            get { return _y; }
            set { _y = value; }
        }
        float _z;
        public float Z
        {
            get { return _z; }
            set { _z = value; }
        }
        float _timesec;
        public float Timesec
        {
            get { return _timesec; }
            set { _timesec = value; }
        }
        string _leglabel;
        public string Leglabel
        {
            get { return _leglabel; }
            set { _leglabel = value; }
        }
        bool _usetrajectory;//lower leg weight

        public bool Usetrajectory
        {
            get { return _usetrajectory; }
            set { _usetrajectory = value; }
        }
        float _period;

        public float Period
        {
            get { return _period; }
            set { _period = value; }
        }
        float _interval;
        public float Interval
        {
            get { return _interval; }
            set { _interval = value; }
               
        }
        float _velocity;
        public float Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }
        bool _usemesh;
        public bool Usemesh
        {
            get { return _usemesh; }
            set { _usemesh = value; }
        }
        public ControlPanel(FromWinformEvents EventsPort)
        {
            _fromWinformPort = EventsPort;

            InitializeComponent();
            _bodyweight = float.Parse(bw.Text);
            _uleglength = float.Parse(ull.Text);
            _lleglength = float.Parse(lll.Text);
            _ulegweight = float.Parse(ulw.Text);
            _llegweight = float.Parse(llw.Text);
            _height = float.Parse(H.Text);
            _width = float.Parse(W.Text);
            _length = float.Parse(L.Text);
            _hipradius = float.Parse(R1.Text);
            _kneeradius = float.Parse(R2.Text);
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Loaded, null, this));
        }

        private void _resetButton_Click(object sender, EventArgs e)
        {
            _interval = float.Parse(interval.Text);
            _period = float.Parse(period.Text);
            _velocity = float.Parse(v.Text);
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Set, null));
        }


        private void _parkButton_Click(object sender, EventArgs e)
        {
            _period = float.Parse(period2.Text);
            _velocity = float.Parse(v2.Text);
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Trotting, null, null));
        }

        private void _forwards_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Pacing, null, null));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _period = float.Parse(period.Text);
            _velocity = float.Parse(v.Text);
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Crawling, null, null));
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Bouncing, null, null));
        }

        private void Build_Click(object sender, EventArgs e)
        {
            _bodyweight = float.Parse(bw.Text);
            _uleglength = float.Parse(ull.Text);
            _lleglength = float.Parse(lll.Text);
            _ulegweight = float.Parse(ulw.Text);
            _llegweight = float.Parse(llw.Text);
            _height = float.Parse(H.Text);
            _width = float.Parse(W.Text);
            _length = float.Parse(L.Text);
            _hipradius = float.Parse(R1.Text);
            _kneeradius = float.Parse(R2.Text);
            _baseweight = float.Parse(basew.Text);
            _hipweight = float.Parse(HW.Text);
            _ankleradius = float.Parse(R3.Text);
            _ankleweight = float.Parse(AW.Text);
            if (mesh.Checked) _usemesh = true;
            else _usemesh = false;
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Build, null, this));

        }

        private void pictureBox3_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Background1, null, this));
        }

        private void pictureBox4_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Background2, null, this));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Trotting2, null, this));
        }

        private void Kinematics_Click(object sender, EventArgs e)
        {
            _x =float.Parse(x.Text);
            _y = float.Parse(y.Text);
            _z = float.Parse(z.Text);
            _timesec = float.Parse(sec.Text);
            _leglabel = leglist.Text;
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Move, null, this));
        }
        private void button4_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Reset, null));
        }

        private void button11_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Background1, null, this));
        }

        private void button12_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Background2, null, this));
        }

        private void button13_Click(object sender, EventArgs e)
        {
            Staticf = float.Parse(staticf.Text);
            Dynamicf = float.Parse(dynamicf.Text);
            Restitution = float.Parse(restitution.Text);
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Background3, null, this));
        }

        private void button14_Click_1(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Ankleposition, null, this));
        }

        private void button16_Click(object sender, EventArgs e)
        {
            Anklex = float.Parse(ankle1.Text);
            Ankley = float.Parse(ankle2.Text);
            Anklez = float.Parse(ankle3.Text);
            Timesec2 = float.Parse(sec2.Text);
            Leglabel2 = leglist2.Text;
            if (frame.SelectedIndex==0) _relative = 0;
            else _relative = 1;
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Movetoposition, null, this));
        }

        private void button15_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Reset, null, this));
        }

        private void button17_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Ankleposition, null, this));
        }

        private void button18_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Hipposition, null, this));
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (usetrajectory.Checked) _usetrajectory = true;
            else _usetrajectory = false;
            _interval = float.Parse(interval.Text);
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Test, null, this));
        }
        private void button7_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Stop, null, this));
        }

        private void button20_Click(object sender, EventArgs e)
        {
            _fromWinformPort.Post(new FromWinformMsg(FromWinformMsg.MsgEnum.Stop, null, this));
        }

    }
}