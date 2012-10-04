//------------------------------------------------------------------------------
// Simulated QuadrupedRobt Service
//
// Professional Microsoft Robotics Developer Studio
//
//
//------------------------------------------------------------------------------
using Microsoft.Ccr.Core;
using Microsoft.Ccr.Adapters.WinForms;
using Microsoft.Dss.Core;
using Microsoft.Dss.Core.Attributes;
using Microsoft.Dss.ServiceModel.Dssp;
using Microsoft.Dss.ServiceModel.DsspServiceBase;
using System.Drawing;

using System;
using System.Collections.Generic;

using Microsoft.Robotics.Simulation;
using Microsoft.Robotics.Simulation.Engine;
using engineproxy = Microsoft.Robotics.Simulation.Engine.Proxy;
using Microsoft.Robotics.Simulation.Physics;
using Microsoft.Robotics.PhysicalModel;
using System.ComponentModel;
using System.Windows.Forms;

using submgr = Microsoft.Dss.Services.SubscriptionManager;
using armproxy = Microsoft.Robotics.Services.ArticulatedArm.Proxy;
using W3C.Soap;

using xna = Microsoft.Xna.Framework;

namespace QuadrupedRobot
{
    [DisplayName("QuadrupedRobot Simulation")]
    [Description("Simulation of a Quadruped Robot")]
    [Contract(Contract.Identifier)]

    public class QuadrupedRobotService : DsspServiceBase
    {
        #region Simulation Variables
        Oneleg[] _leg = new Oneleg[4];
        bool stillmoving;
        // This port receives events from the user interface
        FromWinformEvents _fromWinformPort = new FromWinformEvents();
        ControlPanel _UI = null;
        #endregion

        [Partner("Engine",
            Contract = engineproxy.Contract.Identifier,
            CreationPolicy = PartnerCreationPolicy.UseExistingOrCreate)]
        private engineproxy.SimulationEnginePort _engineStub =
            new engineproxy.SimulationEnginePort();

        // Main service port
        [ServicePort("/QuadrupedRobot", AllowMultipleInstances = false)]
        private QuadrupedRobotOperations _mainPort =
            new QuadrupedRobotOperations();

        [Partner("SubMgr", Contract = submgr.Contract.Identifier, CreationPolicy = PartnerCreationPolicy.CreateAlways)]
        private submgr.SubscriptionManagerPort _submgrPort = new submgr.SubscriptionManagerPort();

        [AlternateServicePort("/arm", AlternateContract = armproxy.Contract.Identifier)]
        private armproxy.ArticulatedArmOperations _armPort = new armproxy.ArticulatedArmOperations();

        public QuadrupedRobotService(DsspServiceCreationPort creationPort)
            : base(creationPort)
        {
        }

        protected override void Start()
        {
            base.Start();
            WinFormsServicePort.Post(new Microsoft.Ccr.Adapters.WinForms.RunForm(CreateForm1));
            // Add the winform message handler to the interleave
            MainPortInterleave.CombineWith(new Interleave(
                new TeardownReceiverGroup(),
                new ExclusiveReceiverGroup
                (
                    Arbiter.ReceiveWithIterator<FromWinformMsg>(true, _fromWinformPort, OnWinformMessageHandler)
                ),
                new ConcurrentReceiverGroup
                ()
            ));

            // Set the initial viewpoint
            SetupCamera();

            // Set up the world
            PopulateWorld();

        }

        // Set up initial view
        private void SetupCamera()
        {
            CameraView view = new CameraView();
            view.EyePosition = new Vector3(15f, 10f, 15f);
            view.LookAtPoint = new Vector3(0, 4f, 0);
            SimulationEngine.GlobalInstancePort.Update(view);
        }
        public BoxShape body;
        public SingleShapeSegmentEntity bodyEntity;
        public float L1, L2, L3, L, H, W, BW, L1W, L2W, R1, R2, R3, HW, Basew, AW;
        private void PopulateWorld()
        {
            AddSky();
            // AddGround();
        }
        void AddSky()
        {
            // Add a sky using a static texture. We will use the sky texture
            // to do per pixel lighting on each simulation visual entity
            SkyDomeEntity sky = new SkyDomeEntity("skydome.dds", "sky_diff.dds");
            SimulationEngine.GlobalInstancePort.Insert(sky);

            // Add a directional light to simulate the sun.
            LightSourceEntity sun = new LightSourceEntity();
            sun.State.Name = "Sun";
            sun.Type = LightSourceEntityType.Directional;
            sun.Color = new Vector4(0.8f, 0.8f, 0.8f, 1);
            sun.Direction = new Vector3(0.5f, -.75f, 0.5f);
            SimulationEngine.GlobalInstancePort.Insert(sun);
        }

        void AddGround3()
        {
            // create a large horizontal plane, at zero elevation.
            HeightFieldEntity ground = new HeightFieldEntity(
                "simple ground", // name
                "nus.jpg", // texture image
                new MaterialProperties("ground",
                    _UI.Restitution, // restitution
                    _UI.Dynamicf, // dynamic friction
                    _UI.Staticf) // static friction
                );
            SimulationEngine.GlobalInstancePort.Insert(ground);
        }
        void AddGround2()
        {
            TerrainEntity ground = new TerrainEntity(
                             "terrain.bmp",
                             "terrain_tex.jpg",
                             new MaterialProperties("ground",
                                 0.9f, // restitution
                                 0.9f, // dynamic friction
                                 0.9f) // static friction
                             );

            SimulationEngine.GlobalInstancePort.Insert(ground);
        }
        void AddGround1()
        {
            // create a large horizontal plane, at zero elevation.
            HeightFieldEntity ground = new HeightFieldEntity(
                "simple ground", // name
                "grass.jpg", // texture image
                new MaterialProperties("ground",
                    0.5f, // restitution
                    0.2f, // dynamic friction
                    0.4f) // static friction
                );
            SimulationEngine.GlobalInstancePort.Insert(ground);
        }

        void Getankleposition()
        {
            string s = "\nFeet positions of the four legs are shown as follows:\n\n";
            s += "LeftFront Leg:(" + _leg[1].Ankle.Position.X + "," + _leg[1].Ankle.Position.Y + "," + _leg[1].Ankle.Position.Z + ")\n";
            s += "RightFront Leg:(" + _leg[0].Ankle.Position.X + "," + _leg[0].Ankle.Position.Y + "," + _leg[0].Ankle.Position.Z + ")\n";
            s += "LeftBack Leg:(" + _leg[3].Ankle.Position.X + "," + _leg[3].Ankle.Position.Y + "," + _leg[3].Ankle.Position.Z + ")\n";
            s += "RightBack Leg:(" + _leg[2].Ankle.Position.X + "," + _leg[2].Ankle.Position.Y + "," + _leg[2].Ankle.Position.Z + ")\n";
            s += "\nTheir reletive positions to each hip joint are as follows:\n\n";
            s += "LeftFront Leg:(" + (_leg[1].Ankle.Position.X - _leg[1].HipJoint.Position.X) + "," + (_leg[1].Ankle.Position.Y - _leg[1].HipJoint.Position.Y) + "," + (_leg[1].Ankle.Position.Z - _leg[1].HipJoint.Position.Z) + ")\n";
            s += "RightFront Leg:(" + (_leg[0].Ankle.Position.X - _leg[0].HipJoint.Position.X) + "," + (_leg[0].Ankle.Position.Y - _leg[0].HipJoint.Position.Y) + "," + (_leg[0].Ankle.Position.Z - _leg[0].HipJoint.Position.Z) + ")\n";
            s += "LeftBack Leg:(" + (_leg[3].Ankle.Position.X - _leg[3].HipJoint.Position.X) + "," + (_leg[3].Ankle.Position.Y - _leg[3].HipJoint.Position.Y) + "," + (_leg[3].Ankle.Position.Z - _leg[3].HipJoint.Position.Z) + ")\n";
            s += "RightBack Leg:(" + (_leg[2].Ankle.Position.X - _leg[2].HipJoint.Position.X) + "," + (_leg[2].Ankle.Position.Y - _leg[2].HipJoint.Position.Y) + "," + (_leg[2].Ankle.Position.Z - _leg[2].HipJoint.Position.Z) + ")\n";
            Console.WriteLine(s);
            MessageBox.Show(s);
        }
        void Gethipposition()
        {
            string s = "\nHip joint positions of the four legs are shown as follows:\n\n";
            s += "LeftFront Leg:(" + _leg[1].HipJoint.Position.X + "," + _leg[1].HipJoint.Position.Y + "," + _leg[1].HipJoint.Position.Z + ")\n";
            s += "RightFront Leg:(" + _leg[0].HipJoint.Position.X + "," + _leg[0].HipJoint.Position.Y + "," + _leg[0].HipJoint.Position.Z + ")\n";
            s += "LeftBack Leg:(" + _leg[3].HipJoint.Position.X + "," + _leg[3].HipJoint.Position.Y + "," + _leg[3].HipJoint.Position.Z + ")\n";
            s += "RightBack Leg:(" + _leg[2].HipJoint.Position.X + "," + _leg[2].HipJoint.Position.Y + "," + _leg[2].HipJoint.Position.Z + ")\n";
            Console.WriteLine(s);
            MessageBox.Show(s);
        }

        // This method is executed if the MoveTo method fails
        void ShowError(Exception e)
        {
            Console.WriteLine(e.Message);
        }

        // This method puts the arm into its parked positionc
        public IEnumerator<ITask> Trotting()
        {
            if (stillmoving == true)
                stillmoving = false;
            float t = _UI.Period / 4;
            MoveToPosition(0, -_UI.Velocity * t, -L1 - 2 * L2 / 3, 0, 0, 0, 0, 2*t);
            MoveToPosition(3, -_UI.Velocity * t, -L1 - 2 * L2 / 3, 0, 0, 0, 0, 2*t);
            MoveToPosition(1, _UI.Velocity * t, -L1 - 2 * L2 / 3, 0, 0, 0, 0, 2*t);
            MoveToPosition(2, _UI.Velocity * t, -L1 - 2 * L2 / 3, 0, 0, 0, 0, 2*t);
            yield return Arbiter.Receive(false, TimeoutPort((int)(t * 2000 + 50)), delegate(DateTime dt) { });
            stillmoving = true;
            bodyEntity.PhysicsEntity.IsKinematic = false;
            for (int i = 0; i < 4; i++)
                _leg[i].PhysicsEntity.IsKinematic = false;
            while (stillmoving)
            {
                /* _leg[0].MoveTo(0, 0, 0, 0.6f);
                 _leg[3].MoveTo(0, 0, 0, 0.6f);
                 _leg[1].MoveTo(0, -40, 70, 0.6f);
                 _leg[2].MoveTo(0, 40, -30, 0.6f);
                 if (stillmoving == false) yield break;
                 yield return Arbiter.Receive(false, TimeoutPort(700), delegate(DateTime dt) { });
                 _leg[0].MoveTo(0, -40, 70, 0.6f);
                 _leg[3].MoveTo(0, 40, -30, 0.6f);
                 _leg[1].MoveTo(0, 0, 0, 0.6f);
                 _leg[2].MoveTo(0, 0, 0, 0.6f);
                 if (stillmoving == false) yield break;
                 yield return Arbiter.Receive(false, TimeoutPort(700), delegate(DateTime dt) { });
                 */
                MoveToPosition(0, -_UI.Velocity * t, -L1 - 2 * L2 / 3, 0, 0, 0, 0, t);
                MoveToPosition(3, -_UI.Velocity * t, -L1 - 2 * L2 / 3, 0, 0, 0, 0, t);
                MoveToPosition(1, _UI.Velocity * t, -L1 - 2 * L2 / 3, 0, 0, 0, 0, t);
                MoveToPosition(2, _UI.Velocity * t, -L1 - 2 * L2 / 3, 0, 0, 0, 0, t);
                if (stillmoving == false) yield break;
                yield return Arbiter.Receive(false, TimeoutPort((int)(t * 1000 + 50)), delegate(DateTime dt) { });
                MoveToPosition(0, 0, -L1, 0, 0, 0, 0, t);
                MoveToPosition(3, 0, -L1, 0, 0, 0, 0, t);
                MoveToPosition(1, 0, -L1 - 2 * L2 / 3, 0, 0, 0, 0, t);
                MoveToPosition(2, 0, -L1 - 2 * L2 / 3, 0, 0, 0, 0, t);
                if (stillmoving == false) yield break;
                yield return Arbiter.Receive(false, TimeoutPort((int)(t * 1000 + 50)), delegate(DateTime dt) { });
                MoveToPosition(0, _UI.Velocity * t, -L1 - 2 * L2 / 3, 0, 0, 0, 0, t);
                MoveToPosition(3, _UI.Velocity * t, -L1 - 2 * L2 / 3, 0, 0, 0, 0, t);
                MoveToPosition(1, -_UI.Velocity * t, -L1 - 2 * L2 / 3, 0, 0, 0, 0, t);
                MoveToPosition(2, -_UI.Velocity * t, -L1 - 2 * L2 / 3, 0, 0, 0, 0, t);
                if (stillmoving == false) yield break;
                yield return Arbiter.Receive(false, TimeoutPort((int)(t * 1000 + 50)), delegate(DateTime dt) { });
                MoveToPosition(0, 0, -L1 - 2 * L2 / 3, 0, 0, 0, 0, t);
                MoveToPosition(3, 0, -L1 - 2 * L2 / 3, 0, 0, 0, 0, t);
                MoveToPosition(1, 0, -L1, 0, 0, 0, 0, t);
                MoveToPosition(2, 0, -L1, 0, 0, 0, 0, t);
                if (stillmoving == false) yield break;
                yield return Arbiter.Receive(false, TimeoutPort((int)(t * 1000 + 50)), delegate(DateTime dt) { });

            }
        }
        public IEnumerator<ITask> Trotting2()
        {
            bodyEntity.PhysicsEntity.IsKinematic = false;
            for (int i = 0; i < 4; i++)
                _leg[i].PhysicsEntity.IsKinematic = false;
            if (stillmoving == true)
                stillmoving = false;
            yield return Arbiter.Receive(false, TimeoutPort(800), delegate(DateTime dt) { });
            stillmoving = true;
            _leg[0].MoveTo(0, 20, 10, 0.7f);
            _leg[3].MoveTo(0, 20, -20, 0.7f);
            _leg[1].MoveTo(0, -20, 10, 0.7f);
            _leg[2].MoveTo(0, -20, -20, 0.7f);
            if (stillmoving == false) yield break;
            yield return Arbiter.Receive(false, TimeoutPort(800), delegate(DateTime dt) { });
            while (stillmoving)
            {
                _leg[0].MoveTo(0, -20, 10, 0.6f);
                _leg[3].MoveTo(0, -20, -20, 0.6f);
                _leg[1].MoveTo(0, 20, 10, 0.6f);
                _leg[2].MoveTo(0, 20, -20, 0.6f);
                if (stillmoving == false) yield break;
                yield return Arbiter.Receive(false, TimeoutPort(700), delegate(DateTime dt) { });
                _leg[0].MoveTo(0, 20, 10, 0.6f);
                _leg[3].MoveTo(0, 20, -20, 0.6f);
                _leg[1].MoveTo(0, -20, 10, 0.6f);
                _leg[2].MoveTo(0, -20, -20, 0.6f);
                if (stillmoving == false) yield break;
                yield return Arbiter.Receive(false, TimeoutPort(700), delegate(DateTime dt) { });
            }
        }
        public IEnumerator<ITask> Bouncing()
        {
            bodyEntity.PhysicsEntity.IsKinematic = false;
            for (int i = 0; i < 4; i++)
                _leg[i].PhysicsEntity.IsKinematic = false;
            if (stillmoving == true)
                stillmoving = false;
            yield return Arbiter.Receive(false, TimeoutPort(800), delegate(DateTime dt) { });
            stillmoving = true;
            _leg[2].MoveTo(0, -10, 0, 0.7f);
            _leg[3].MoveTo(0, -10, 0, 0.7f);
            _leg[0].MoveTo(0, 10, 0, 0.7f);
            _leg[1].MoveTo(0, 10, 0, 0.7f);
            if (stillmoving == false) yield break;
            yield return Arbiter.Receive(false, TimeoutPort(800), delegate(DateTime dt) { });
            while (stillmoving)
            {
                _leg[2].MoveTo(0, 10, 0, 0.2f);
                _leg[3].MoveTo(0, 10, 0, 0.2f);
                _leg[0].MoveTo(0, -10, 0, 0.3f);
                _leg[1].MoveTo(0, -10, 0, 0.3f);
                if (stillmoving == false) yield break;
                yield return Arbiter.Receive(false, TimeoutPort(400), delegate(DateTime dt) { });
                _leg[2].MoveTo(0, -10, 0, 0.2f);
                _leg[3].MoveTo(0, -10, 0, 0.2f);
                _leg[0].MoveTo(0, 10, 0, 0.3f);
                _leg[1].MoveTo(0, 10, 0, 0.3f);
                if (stillmoving == false) yield break;
                yield return Arbiter.Receive(false, TimeoutPort(800), delegate(DateTime dt) { });

            }
        }
        public IEnumerator<ITask> Pacing()
        {
            bodyEntity.PhysicsEntity.IsKinematic = false;
            for (int i = 0; i < 4; i++)
                _leg[i].PhysicsEntity.IsKinematic = false;
            if (stillmoving == true)
                stillmoving = false;
            yield return Arbiter.Receive(false, TimeoutPort(800), delegate(DateTime dt) { });
            stillmoving = true;
            _leg[0].MoveTo(0, -10, -20, 0.7f);
            _leg[2].MoveTo(0, -10, -20, 0.7f);
            _leg[1].MoveTo(0, 10, 20, 0.7f);
            _leg[3].MoveTo(0, 10, 20, 0.7f);
            if (stillmoving == false) yield break;
            yield return Arbiter.Receive(false, TimeoutPort(900), delegate(DateTime dt) { });
            while (stillmoving)
            {
                _leg[0].MoveTo(0, 10, 20, 0.7f);
                _leg[2].MoveTo(0, 10, 20, 0.7f);
                _leg[1].MoveTo(0, -10, -20, 0.7f);
                _leg[3].MoveTo(0, -10, -20, 0.7f);
                if (stillmoving == false) yield break;
                yield return Arbiter.Receive(false, TimeoutPort(900), delegate(DateTime dt) { });
                _leg[0].MoveTo(0, -10, -20, 0.7f);
                _leg[2].MoveTo(0, -10, -20, 0.7f);
                _leg[1].MoveTo(0, 10, 20, 0.7f);
                _leg[3].MoveTo(0, 10, 20, 0.7f);
                if (stillmoving == false) yield break;
                yield return Arbiter.Receive(false, TimeoutPort(900), delegate(DateTime dt) { });
            }

        }
        public IEnumerator<ITask> Crawling()
        {
            if (stillmoving == true)
                stillmoving = false;
            bodyEntity.PhysicsEntity.IsKinematic = false;
            for (int i = 0; i < 4; i++)
                _leg[i].PhysicsEntity.IsKinematic = false;
            float _interval = _UI.Period / 8;
            MoveToPosition(0, -3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
            MoveToPosition(3, -_UI.Velocity * _UI.Period / 8, -L1 - L2 * 2 / 3, 0, 0, 0, 0, _interval);
            MoveToPosition(1, _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
            MoveToPosition(2, 3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
            yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
            stillmoving = true;
            if (_UI.Velocity > 0)
            {
                while (stillmoving)
                {
                   
                    MoveToPosition(0, -3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(3, -_UI.Velocity * _UI.Period / 8, -L1 - L2 * 2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(1, _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(2, 3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
                    if (stillmoving == false) yield break;
                    MoveToPosition(0, 0, -L1, 0, 0, 0, 0, _interval);
                    MoveToPosition(3, -_UI.Velocity * _UI.Period / 4, -L1 - L2 * 2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(1, 0, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(2, _UI.Velocity * _UI.Period / 4, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
                    if (stillmoving == false) yield break;
                    MoveToPosition(3, -3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(1, -_UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(2, _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(0, 3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
                    if (stillmoving == false) yield break;
                    MoveToPosition(3, 0, -L1, 0, 0, 0, 0, _interval);
                    MoveToPosition(1, -_UI.Velocity * _UI.Period / 4, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(2, 0, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(0, _UI.Velocity * _UI.Period / 4, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
                    if (stillmoving == false) yield break;
                    MoveToPosition(1, -3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(2, -_UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(0, _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(3, 3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
                    if (stillmoving == false) yield break;
                    MoveToPosition(1, 0, -L1, 0, 0, 0, 0, _interval);
                    MoveToPosition(2, -_UI.Velocity * _UI.Period / 4, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(0, 0, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(3, _UI.Velocity * _UI.Period / 4, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
                    if (stillmoving == false) yield break;
                    MoveToPosition(2, -3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(0, -_UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(3, _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(1, 3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
                    if (stillmoving == false) yield break;
                    MoveToPosition(2, 0, -L1, 0, 0, 0, 0, _interval);
                    MoveToPosition(0, -_UI.Velocity * _UI.Period / 4, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(3, 0, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(1, _UI.Velocity * _UI.Period / 4, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
                    if (stillmoving == false) yield break;

                    /*yield return Arbiter.Choice(_leg[0].MoveTo(0, 80, -50, 0.8f),
        delegate(SuccessResult s) { },
        ShowError);
                    if (stillmoving == false) yield break;
                    yield return Arbiter.Choice(_leg[0].MoveTo(0, -20, 80, 0.6f),
                     delegate(SuccessResult s) { },
                     ShowError);
                    if (stillmoving == false) yield break;
                    yield return Arbiter.Choice(_leg[2].MoveTo(0, 80, -90, 0.6f),
                     delegate(SuccessResult s) { },
                     ShowError);
                    if (stillmoving == false) yield break;
                    yield return Arbiter.Choice(_leg[2].MoveTo(0, 20, -40, 0.4f),
    delegate(SuccessResult s) { },
    ShowError);
                    if (stillmoving == false) yield break;
                    yield return Arbiter.Choice(_leg[1].MoveTo(0, 80, 40, 0.8f),
        delegate(SuccessResult s) { },
        ShowError);
                    if (stillmoving == false) yield break;
                    yield return Arbiter.Choice(_leg[1].MoveTo(0, -20, 80, 0.6f),
                     delegate(SuccessResult s) { },
                     ShowError);
                    if (stillmoving == false) yield break;
                    yield return Arbiter.Choice(_leg[3].MoveTo(0, 80, -90, 0.6f),
                       delegate(SuccessResult s) { },
                       ShowError);
                    if (stillmoving == false) yield break;
                    yield return Arbiter.Choice(_leg[3].MoveTo(0, 20, -40, 0.4f),
                       delegate(SuccessResult s) { },
                       ShowError);*/
                }
            }
            else
            {
                while (stillmoving)
                {
                    if (stillmoving == false) yield break;
                    MoveToPosition(0, -3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(2, -_UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(1, _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(3, 3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
                    if (stillmoving == false) yield break;
                    MoveToPosition(0, 0, -L1, 0, 0, 0, 0, _interval);
                    MoveToPosition(2, -_UI.Velocity * _UI.Period / 4, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(1, 0, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(3, _UI.Velocity * _UI.Period / 4, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
                    if (stillmoving == false) yield break;
                    MoveToPosition(2, -3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(1, -_UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(3, _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(0, 3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
                    if (stillmoving == false) yield break;
                    MoveToPosition(2, 0, -L1, 0, 0, 0, 0, _interval);
                    MoveToPosition(1, -_UI.Velocity * _UI.Period / 4, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(3, 0, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(0, _UI.Velocity * _UI.Period / 4, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
                    if (stillmoving == false) yield break;

                    MoveToPosition(1, -3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(3, -_UI.Velocity * _UI.Period / 8, -L1 - L2 * 2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(0, _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(2, 3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
                    if (stillmoving == false) yield break;
                    MoveToPosition(1, 0, -L1, 0, 0, 0, 0, _interval);
                    MoveToPosition(3, -_UI.Velocity * _UI.Period / 4, -L1 - L2 * 2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(0, 0, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(2, _UI.Velocity * _UI.Period / 4, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
                    if (stillmoving == false) yield break;
                    MoveToPosition(3, -3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(0, -_UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(2, _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(1, 3 * _UI.Velocity * _UI.Period / 8, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
                    if (stillmoving == false) yield break;
                    MoveToPosition(3, 0, -L1, 0, 0, 0, 0, _interval);
                    MoveToPosition(0, -_UI.Velocity * _UI.Period / 4, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(2, 0, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    MoveToPosition(1, _UI.Velocity * _UI.Period / 4, -L1 - 2 * L2 / 3, 0, 0, 0, 0, _interval);
                    yield return Arbiter.Receive(false, TimeoutPort((int)(_interval * 1000 + 50)), delegate(DateTime dt) { });
                }
            }
        }
        public IEnumerator<ITask> BuildRobot()
        {
            body = new BoxShape(new BoxShapeProperties(
            BW, new Pose(), new Vector3(L, H, W)));
            body.State.DiffuseColor = new Vector4(0.8f, 0.8f, 0.8f, 1);
            float center = R3 + R1 + L1 + L2 + H / 2;
            bodyEntity = new SingleShapeSegmentEntity(body, new Vector3(0, center, 0));
            bodyEntity.State.Pose.Orientation = new Quaternion(0, 0, 0, 1);
            bodyEntity.State.Name = "body";
            if (_UI.Usemesh) bodyEntity.State.Assets.Mesh = "body.obj";
            bodyEntity.State.Flags = EntitySimulationModifiers.Kinematic;
            SimulationEngine.GlobalInstancePort.Insert(bodyEntity);
            AttachedCameraEntity bodyCam = new AttachedCameraEntity();
            bodyCam.State.Name = "Body Camera";
            bodyCam.State.Pose = new Pose(new Vector3(L / 2 + 0.5f, 3.25f, 0),
                Quaternion.FromAxisAngle(0, 1, 0, -(float)(Math.PI / 2)) *
                Quaternion.FromAxisAngle(0, 0, 1, 0));
            // the bodycam coordinates are relative to the bodyEntity, don't use InsertEntityGlobal
            bodyEntity.InsertEntity(bodyCam);
            for (int i = 0; i < 4; i++)
            {
                _leg[i] = new Oneleg("leg" + i, new Vector3((1 - i / 2 * 2) * (L / 2 - R1), center - H / 2 + R1, (1 - i % 2 * 2) * (W / 2 - R1)));
                _leg[i].State.Flags = EntitySimulationModifiers.Kinematic;
                JointAngularProperties hipAngular = new JointAngularProperties();
                EntityJointConnector[] hipConnectors = new EntityJointConnector[2]
            {
                new EntityJointConnector(_leg[i], new Vector3(0,1,0), new Vector3(1,0,0), new Vector3(0, 0, 0)),
                new EntityJointConnector(bodyEntity, new Vector3(0,1,0), new Vector3(1,0,0), new Vector3((1 - i / 2 * 2) *(L/2-R1),-H/2+R1, (1 - i % 2 * 2) *(W/2-R1)))
            };
                _leg[i].CustomJoint = new Joint();
                _leg[i].CustomJoint.State = new JointProperties(hipAngular, hipConnectors);
                _leg[i].CustomJoint.State.Name = "hip" + i;
                bodyEntity.InsertEntityGlobal(_leg[i]);
                if (_UI.Usemesh)
                {
                    _leg[i].State.Assets.Mesh = "hip.obj";
                    _leg[i].UpperLeg.State.Assets.Mesh = "upperleg.obj";
                    _leg[i].LowerLeg.State.Assets.Mesh = "lowerleg.obj";
                    _leg[i].Ankle.State.Assets.Mesh = "foot.obj";
                }
            }
            yield break;
        }
        public IEnumerator<ITask> Set()
        {
            bodyEntity.PhysicsEntity.IsKinematic = false;
            for (int i = 0; i < 4; i++)
                _leg[i].PhysicsEntity.IsKinematic = false;
            stillmoving = false;
            for (int i = 0; i < 4; i++)
                _leg[i].Deactivate();
            Activate(Arbiter.Receive(false, TimeoutPort(600), delegate(DateTime now)
            {
                _leg[2].MoveTo(0, 20, -40, 0.5f);
                _leg[1].MoveTo(0, -20, 40, 0.5f);
                _leg[3].MoveTo(0, 20, -40, 0.5f);
                _leg[0].MoveTo(0, -20, 40, 0.5f);
            }));
            yield break;
        }
        public IEnumerator<ITask> Reset()
        {
            Activate(Arbiter.Receive(false, TimeoutPort(600), delegate(DateTime now)
            {
                _leg[2].MoveTo(0, 0, 0, 0.5f);
                _leg[1].MoveTo(0, 0, 0, 0.5f);
                _leg[3].MoveTo(0, 0, 0, 0.5f);
                _leg[0].MoveTo(0, 0, 0, 0.5f);
            }));
            yield break;
        }
        public IEnumerator<ITask> Move()
        {
            float x, y, z, t;
            x = _UI.X;
            y = _UI.Y;
            z = _UI.Z;
            t = _UI.Timesec;
            bodyEntity.PhysicsEntity.IsKinematic = true;
            for (int i = 0; i < 4; i++)
                _leg[i].PhysicsEntity.IsKinematic = true;
            switch (_UI.Leglabel)
            {
                case "leftfront":
                    yield return Arbiter.Choice(_leg[1].MoveTo(x, y, z, t),
    delegate(SuccessResult s) { },
    ShowError);
                    // MoveToPosition(0,_UI.X, _UI.Y, _UI.Z, _UI.Timesec);
                    break;
                case "leftback":
                    yield return Arbiter.Choice(_leg[3].MoveTo(x, y, z, t),
    delegate(SuccessResult s) { },
    ShowError);
                    //  MoveToPosition(1, _UI.X, _UI.Y, _UI.Z, _UI.Timesec);
                    break;
                case "rightfront":
                    //  MoveToPosition(2, _UI.X, _UI.Y, _UI.Z, _UI.Timesec);
                    yield return Arbiter.Choice(_leg[0].MoveTo(x, y, z, t),
    delegate(SuccessResult s) { },
    ShowError);
                    break;
                case "rightback":
                    //  MoveToPosition(3, _UI.X, _UI.Y, _UI.Z, _UI.Timesec);
                    yield return Arbiter.Choice(_leg[2].MoveTo(x, y, z, t),
    delegate(SuccessResult s) { },
    ShowError);
                    break;
                default:
                    MessageBox.Show("Please select a leg");
                    break;

            }
            yield break;
        }
        public IEnumerator<ITask> Movetoposition()
        {
            float x, y, z, t;
            x = _UI.Anklex;
            y = _UI.Ankley;
            z = _UI.Anklez;
            t = _UI.Timesec2;
            bodyEntity.PhysicsEntity.IsKinematic = true;
            for (int i = 0; i < 4; i++)
                _leg[i].PhysicsEntity.IsKinematic = true;
            switch (_UI.Leglabel2)
            {
                case "leftfront":
                    if (_UI.Relative == 1)
                        MoveToPosition(1, x, y, z, _leg[1].HipJoint.Position.X, _leg[1].HipJoint.Position.Y, _leg[1].HipJoint.Position.Z, t);
                    else
                        MoveToPosition(1, x, y, z, 0, 0, 0, t);
                    break;
                case "leftback":
                    if (_UI.Relative == 1)
                        MoveToPosition(3, x, y, z, _leg[3].HipJoint.Position.X, _leg[3].HipJoint.Position.Y, _leg[3].HipJoint.Position.Z, t);
                    else
                        MoveToPosition(3, x, y, z, 0, 0, 0, t);
                    break;
                case "rightfront":
                    if (_UI.Relative == 1)
                        MoveToPosition(0, x, y, z, _leg[0].HipJoint.Position.X, _leg[0].HipJoint.Position.Y, _leg[0].HipJoint.Position.Z, t);
                    else
                        MoveToPosition(0, x, y, z, 0, 0, 0, t);
                    break;
                case "rightback":
                    if (_UI.Relative == 1)
                        MoveToPosition(2, x, y, z, _leg[2].HipJoint.Position.X, _leg[2].HipJoint.Position.Y, _leg[2].HipJoint.Position.Z, t);
                    else
                        MoveToPosition(2, x, y, z, 0, 0, 0, t);
                    break;
                default:
                    MessageBox.Show("Please select a leg");
                    break;

            }
            yield break;
        }
        public IEnumerator<ITask> Test()
        {
            WinFormsServicePort.Post(new Microsoft.Ccr.Adapters.WinForms.RunForm(CreateForm2));
            bodyEntity.PhysicsEntity.IsKinematic = false;
            for (int i = 0; i < 4; i++)
                _leg[i].PhysicsEntity.IsKinematic = false;
            if (_UI.Usetrajectory == true)
            {
                stillmoving = true;
                string[] lines = System.IO.File.ReadAllLines(@"C:\Trajectory\Planned.txt");
                int j;
                string s;
                string _ankleposition, _hipposition, _relativeposition;
                float x1, y1, z1, x2, y2, z2, x3, y3, z3, x4, y4, z4;
                while (stillmoving)
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        j = 0;
                        s = "";
                        _ankleposition = "";
                        _hipposition = "";
                        _relativeposition = "";
                        while ((lines[i][j] != ' ') && (j < lines[i].Length))
                        {
                            s += lines[i][j];
                            j++;
                        }
                        x1 = float.Parse(s);
                        while (lines[i][j] == ' ')
                            j++;
                        s = "";
                        while ((lines[i][j] != ' ') && (j < lines[i].Length))
                        {
                            s += lines[i][j];
                            j++;
                        }
                        y1 = float.Parse(s);
                        while (lines[i][j] == ' ')
                            j++;
                        s = "";
                        while ((j < lines[i].Length) && (lines[i][j] != ' '))
                        {
                            s += lines[i][j];
                            j++;
                        }
                        z1 = float.Parse(s);
                        while (lines[i][j] == ' ')
                            j++;
                        s = "";
                        while ((lines[i][j] != ' ') && (j < lines[i].Length))
                        {
                            s += lines[i][j];
                            j++;
                        }
                        x2 = float.Parse(s);
                        while (lines[i][j] == ' ')
                            j++;
                        s = "";
                        while ((lines[i][j] != ' ') && (j < lines[i].Length))
                        {
                            s += lines[i][j];
                            j++;
                        }
                        y2 = float.Parse(s);
                        while (lines[i][j] == ' ')
                            j++;
                        s = "";
                        while ((j < lines[i].Length) && (lines[i][j] != ' '))
                        {
                            s += lines[i][j];
                            j++;
                        }
                        z2 = float.Parse(s);
                        while (lines[i][j] == ' ')
                            j++;
                        s = "";
                        while ((lines[i][j] != ' ') && (j < lines[i].Length))
                        {
                            s += lines[i][j];
                            j++;
                        }
                        x3 = float.Parse(s);
                        while (lines[i][j] == ' ')
                            j++;
                        s = "";
                        while ((lines[i][j] != ' ') && (j < lines[i].Length))
                        {
                            s += lines[i][j];
                            j++;
                        }
                        y3 = float.Parse(s);
                        while (lines[i][j] == ' ')
                            j++;
                        s = "";
                        while ((j < lines[i].Length) && (lines[i][j] != ' '))
                        {
                            s += lines[i][j];
                            j++;
                        }
                        z3 = float.Parse(s);
                        while (lines[i][j] == ' ')
                            j++;
                        s = "";
                        while ((lines[i][j] != ' ') && (j < lines[i].Length))
                        {
                            s += lines[i][j];
                            j++;
                        }
                        x4 = float.Parse(s);
                        while (lines[i][j] == ' ')
                            j++;
                        s = "";
                        while ((lines[i][j] != ' ') && (j < lines[i].Length))
                        {
                            s += lines[i][j];
                            j++;
                        }
                        y4 = float.Parse(s);
                        while (lines[i][j] == ' ')
                            j++;
                        s = "";
                        while ((j < lines[i].Length) && (lines[i][j] != ' '))
                        {
                            s += lines[i][j];
                            j++;
                        }
                        z4 = float.Parse(s);
                        MoveToPosition(2, x4, y4, z4, 0, 0, 0, _UI.Interval);
                        MoveToPosition(1, x1, y1, z1, 0, 0, 0, _UI.Interval);
                        MoveToPosition(3, x2, y2, z2, 0, 0, 0, _UI.Interval);
                        MoveToPosition(0, x3, y3, z3, 0, 0, 0, _UI.Interval);
                        yield return Arbiter.Receive(false, TimeoutPort((int)(_UI.Interval * 1000 + 25)), delegate(DateTime dt) { });
                        FormCollection fc = Application.OpenForms;
                        foreach (Form frm in fc)
                            if (frm.GetType() == typeof(Trajectory))
                            {
                                Legattribute._trajectory1.DrawChart(0, Conversions.LocationToPixel(_leg[0].Ankle.Position.X), Conversions.LocationToPixel(_leg[0].Ankle.Position.Y), Conversions.LocationToPixel(2 * _leg[0].UpperLeg.Position.X - _leg[0].HipJoint.Position.X),
                                   Conversions.LocationToPixel(2 * _leg[0].UpperLeg.Position.Y - _leg[0].HipJoint.Position.Y), Conversions.LocationToPixel(_leg[0].HipJoint.Position.X), Conversions.LocationToPixel(_leg[0].HipJoint.Position.Y));
                                Legattribute._trajectory1.DrawChart(1, Conversions.LocationToPixel(_leg[1].Ankle.Position.X), Conversions.LocationToPixel(_leg[1].Ankle.Position.Y), Conversions.LocationToPixel(2 * _leg[1].UpperLeg.Position.X - _leg[1].HipJoint.Position.X),
              Conversions.LocationToPixel(2 * _leg[1].UpperLeg.Position.Y - _leg[1].HipJoint.Position.Y), Conversions.LocationToPixel(_leg[1].HipJoint.Position.X), Conversions.LocationToPixel(_leg[1].HipJoint.Position.Y));
                                Legattribute._trajectory1.DrawChart(2, Conversions.LocationToPixel(_leg[2].Ankle.Position.X), Conversions.LocationToPixel(_leg[2].Ankle.Position.Y), Conversions.LocationToPixel(2 * _leg[2].UpperLeg.Position.X - _leg[2].HipJoint.Position.X),
            Conversions.LocationToPixel(2 * _leg[2].UpperLeg.Position.Y - _leg[2].HipJoint.Position.Y), Conversions.LocationToPixel(_leg[2].HipJoint.Position.X), Conversions.LocationToPixel(_leg[2].HipJoint.Position.Y));
                                Legattribute._trajectory1.DrawChart(3, Conversions.LocationToPixel(_leg[3].Ankle.Position.X), Conversions.LocationToPixel(_leg[3].Ankle.Position.Y), Conversions.LocationToPixel(2 * _leg[3].UpperLeg.Position.X - _leg[3].HipJoint.Position.X),
            Conversions.LocationToPixel(2 * _leg[3].UpperLeg.Position.Y - _leg[3].HipJoint.Position.Y), Conversions.LocationToPixel(_leg[3].HipJoint.Position.X), Conversions.LocationToPixel(_leg[3].HipJoint.Position.Y));
                                // Example #2: Write one string to a text file.
                            }
                        _ankleposition += _leg[1].Ankle.Position.X + " " + _leg[1].Ankle.Position.Y + " " + _leg[3].Ankle.Position.X + " " + _leg[3].Ankle.Position.Y + " " + _leg[0].Ankle.Position.X + " " + _leg[0].Ankle.Position.Y + " " + _leg[2].Ankle.Position.X + " " + _leg[2].Ankle.Position.Y + "\n";
                        _hipposition += _leg[1].HipJoint.Position.X + " " + _leg[1].HipJoint.Position.Y + " " + _leg[3].HipJoint.Position.X + " " + _leg[3].HipJoint.Position.Y + " " + _leg[0].HipJoint.Position.X + " " + _leg[0].HipJoint.Position.Y + " " + _leg[2].HipJoint.Position.X + " " + _leg[2].HipJoint.Position.Y + "\n";
                        _relativeposition += (_leg[1].Ankle.Position.X - _leg[1].HipJoint.Position.X) + " " + (_leg[1].Ankle.Position.Y - _leg[1].HipJoint.Position.Y) + " " + (_leg[3].Ankle.Position.X - _leg[3].HipJoint.Position.X) + " " + (_leg[3].Ankle.Position.Y - _leg[3].HipJoint.Position.Y) + " " +
                            (_leg[0].Ankle.Position.X - _leg[0].HipJoint.Position.X) + " " + (_leg[0].Ankle.Position.Y - _leg[0].HipJoint.Position.Y) + " " + (_leg[2].Ankle.Position.X - _leg[2].HipJoint.Position.X) + " " + (_leg[2].Ankle.Position.Y - _leg[2].HipJoint.Position.Y) + "\n";
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Trajectory\Actual\footposition.txt", true))
                        {
                            file.WriteLine(_ankleposition);
                        }
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Trajectory\Actual\hipposition.txt", true))
                        {
                            file.WriteLine(_hipposition);
                        }
                        using (System.IO.StreamWriter file = new System.IO.StreamWriter(@"C:\Trajectory\Actual\relativeposition.txt", true))
                        {
                            file.WriteLine(_relativeposition);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Please Check Your Trajectory File");
                MessageBox.Show("Please Check Your Trajectory File");
            }
            Console.WriteLine("finish");
            yield break;
        }
        // Perform the Inverse Kinematics calculations
        //
        bool InverseKinematics(
            float mx, // x position
            float my, // y position
            float mz, // z position
            ref float hipAngle,
            ref float upperAngle,
            ref float lowerAngle)
        {
            // Algorithm taken from Hoon Hong's ik2.xls IK method posted on the Lynx website
            float l1 = Legattribute.upperlegL1, l2 = Legattribute.lowerlegL2;
            float temp;
            temp = l1 + l2 - (float)(Math.Sqrt(mx * mx + my * my + mz * mz));
            if (temp < 0)
            {
                Console.WriteLine("No solution to Inverse Kinematics");
                return false;
            }
            lowerAngle = -Conversions.DegreesToRadians(180) + (float)(Math.Acos((l1 * l1 + l2 * l2 - mx * mx - my * my - mz * mz) / (2 * l1 * l2)));
            //  else lowerAngle = Conversions.DegreesToRadians(180) - (float)(Math.Acos((l1 * l1 + l2 * l2 - mx * mx - my * my - mz * mz) / (2 * l1 * l2)));
            if (mx >= 0) upperAngle = (float)(Math.Atan(-(float)(Math.Sqrt(mx * mx + mz * mz) / my))) - (float)(Math.Asin((float)(Math.Sin(lowerAngle)) * l2 / (Math.Sqrt(mx * mx + my * my + mz * mz))));
            else upperAngle = -(float)(Math.Atan(-(float)(Math.Sqrt(mx * mx + mz * mz) / my))) - (float)(Math.Asin((float)(Math.Sin(lowerAngle)) * l2 / (Math.Sqrt(mx * mx + my * my + mz * mz))));
            //if (mx >= 0) upperAngle = (float)(Math.Atan(-(float)(Math.Sqrt(mx * mx + mz * mz) / my))) - (float)(Math.Asin((float)(Math.Sin(lowerAngle)) * l2 / (Math.Sqrt(mx * mx + my * my + mz * mz))));
            // else upperAngle = -(float)(Math.Atan(-(float)(Math.Sqrt(mx * mx + mz * mz) / my))) +(float)(Math.Asin((float)(Math.Sin(-lowerAngle)) * l2 / (Math.Sqrt(mx * mx + my * my + mz * mz))));
            if (mx == 0) { if (mz < 0)hipAngle = Conversions.DegreesToRadians(90); else if (mz > 0) hipAngle = Conversions.DegreesToRadians(-90); else hipAngle = 0; }
            else hipAngle = (float)(Math.Atan(-mz / mx));
            //  else hipAngle = Conversions.DegreesToRadians(180) +(float)(Math.Atan(mz / mx));
            Console.WriteLine("Rotate around the hip joint for " + Conversions.RadiansToDegrees(hipAngle) + " degrees(anti-clockwise).");
            Console.WriteLine("Move the upper leg up for " + Conversions.RadiansToDegrees(upperAngle) + " degrees.");
            Console.WriteLine("Move the Lower leg up for " + Conversions.RadiansToDegrees(lowerAngle) + " degrees.");

            // Convert all values to degrees
            hipAngle = Conversions.RadiansToDegrees(hipAngle);
            upperAngle = Conversions.RadiansToDegrees(upperAngle);
            lowerAngle = Conversions.RadiansToDegrees(lowerAngle);
            // Check to make sure that the solution is valid
            if (Single.IsNaN(hipAngle) ||
                Single.IsNaN(upperAngle) ||
                Single.IsNaN(lowerAngle))
            {
                // Use for debugging only!
                //Console.WriteLine("No solution to Inverse Kinematics");
                return false;
            }
            else
                return true;
        }
        bool InverseKinematics2(
            float mx, // x position
            float my, // y position
            float mz, // z position
            ref float hipAngle,
            ref float upperAngle,
            ref float lowerAngle)
        {
            // Algorithm taken from Hoon Hong's ik2.xls IK method posted on the Lynx website
            float l1 = Legattribute.upperlegL1, l2 = Legattribute.lowerlegL2;
            float temp;
            temp = l1 + l2 - (float)(Math.Sqrt(mx * mx + my * my + mz * mz));
            if (temp < 0)
            {
                Console.WriteLine("No solution to Inverse Kinematics");
                return false;
            }
            lowerAngle = Conversions.DegreesToRadians(180) - (float)(Math.Acos((l1 * l1 + l2 * l2 - mx * mx - my * my - mz * mz) / (2 * l1 * l2)));
            if (mx >= 0) upperAngle = (float)(Math.Atan(-(float)(Math.Sqrt(mx * mx + mz * mz) / my))) + (float)(Math.Asin((float)(Math.Sin(-lowerAngle)) * l2 / (Math.Sqrt(mx * mx + my * my + mz * mz))));
            else upperAngle = -(float)(Math.Atan(-(float)(Math.Sqrt(mx * mx + mz * mz) / my))) + (float)(Math.Asin((float)(Math.Sin(-lowerAngle)) * l2 / (Math.Sqrt(mx * mx + my * my + mz * mz))));
            if (mx == 0) { if (mz < 0)hipAngle = Conversions.DegreesToRadians(90); else if (mz > 0) hipAngle = Conversions.DegreesToRadians(-90); else hipAngle = 0; }
            else hipAngle = (float)(Math.Atan(-mz / mx));
            //  else hipAngle = Conversions.DegreesToRadians(180) +(float)(Math.Atan(mz / mx));
            Console.WriteLine("Rotate around the hip joint for " + Conversions.RadiansToDegrees(hipAngle) + " degrees(anti-clockwise).");
            Console.WriteLine("Move the upper leg up for " + Conversions.RadiansToDegrees(upperAngle) + " degrees.");
            Console.WriteLine("Move the Lower leg up for " + Conversions.RadiansToDegrees(lowerAngle) + " degrees.");

            // Convert all values to degrees
            hipAngle = Conversions.RadiansToDegrees(hipAngle);
            upperAngle = Conversions.RadiansToDegrees(upperAngle);
            lowerAngle = Conversions.RadiansToDegrees(lowerAngle);
            // Check to make sure that the solution is valid
            if (Single.IsNaN(hipAngle) ||
                Single.IsNaN(upperAngle) ||
                Single.IsNaN(lowerAngle))
            {
                // Use for debugging only!
                //Console.WriteLine("No solution to Inverse Kinematics");
                return false;
            }
            else
                return true;
        }
        // This method calculates the joint angles necessary to place the arm into the 
        // specified position.  The arm position is specified by the X,Y,Z coordinates
        // of the ankle tip as well as the angle of the ankle, the rotation of the ankle, 
        // and the open distance of the grip.  The motion is completed in the 
        // specified time.
        public SuccessFailurePort MoveToPosition(
            int i,
            float mx, // x position
            float my, // y position
            float mz, // z position
            float hipx,
            float hipy,
            float hipz,
            float time) // time to complete the movement
        {
            float hipAngle = 0, upperAngle = 0, lowerAngle = 0;
            // Do the inverse kinematics to obtain the joint angles
            if ((i == 0) || (i == 1))
            {
                if (!InverseKinematics2(mx - hipx, my - hipy, mz - hipz, ref hipAngle, ref upperAngle, ref lowerAngle))
                {
                    SuccessFailurePort s = new SuccessFailurePort();
                    s.Post(new Exception("Inverse Kinematics failed"));
                    Console.WriteLine("Inverse Kinematics Failed, Cannot Move to the Indicated Position");
                    MessageBox.Show("Inverse Kinematics Failed, Cannot Move to the Indicated Position");
                    return s;
                }
                // Position the arm with the calculated joint angles.
                return _leg[i].MoveTo(
                    hipAngle,
                    upperAngle,
                    lowerAngle,
                    time);
            }
            else
            {
                if (!InverseKinematics(mx - hipx, my - hipy, mz - hipz, ref hipAngle, ref upperAngle, ref lowerAngle))
                {
                    SuccessFailurePort s = new SuccessFailurePort();
                    s.Post(new Exception("Inverse Kinematics failed"));
                    Console.WriteLine("Inverse Kinematics Failed, Cannot Move to the Indicated Position");
                    MessageBox.Show("Inverse Kinematics Failed, Cannot Move to the Indicated Position");
                    return s;
                }
                // Position the arm with the calculated joint angles.
                return _leg[i].MoveTo(
                    hipAngle,
                    upperAngle,
                    lowerAngle,
                    time);
            }
        }


        #region UI form methods
        // Create the UI form
        System.Windows.Forms.Form CreateForm1()
        {
            _UI = new ControlPanel(_fromWinformPort);
            return _UI;
        }
        System.Windows.Forms.Form CreateForm2()
        {
            if (Legattribute._trajectory1 == null) Legattribute._trajectory1 = new Trajectory();
            return Legattribute._trajectory1;
        }
        // process messages from the UI Form
        IEnumerator<ITask> OnWinformMessageHandler(FromWinformMsg msg)
        {
            switch (msg.Command)
            {
                case FromWinformMsg.MsgEnum.Loaded:
                    _UI = (ControlPanel)msg.Object;
                    break;
                case FromWinformMsg.MsgEnum.Build:
                    L = _UI.Length;
                    H = _UI.Height1;
                    W = _UI.Width1;
                    BW = _UI.Bodyweight;
                    L1 = _UI.Uleglength;
                    L1W = _UI.Ulegweight;
                    L2 = _UI.Lleglength;
                    L2W = _UI.Lleglength;
                    L3 = L1 + L2;
                    R1 = _UI.Hipradius;
                    R2 = _UI.Kneeradius;
                    R3 = _UI.Ankleradius;
                    AW = _UI.Ankleweight;
                    HW = _UI.Hipweight;
                    Basew = _UI.Baseweight;
                    Legattribute.lowerlegL2 = L2;
                    Legattribute.lowerlegR2 = R2;
                    Legattribute.lowerlegW2 = L2W;
                    Legattribute.upperlegW1 = L1W;
                    Legattribute.upperlegR1 = R1;
                    Legattribute.upperlegL1 = L1;
                    Legattribute.hipweight = HW;
                    Legattribute.ankleradius = R3;
                    Legattribute.ankleweight = AW;
                    Legattribute.baseweight = Basew;
                    SpawnIterator(BuildRobot);
                    break;
                case FromWinformMsg.MsgEnum.Trotting:
                    SpawnIterator(Trotting);
                    break;
                case FromWinformMsg.MsgEnum.Trotting2:
                    SpawnIterator(Trotting2);
                    break;
                case FromWinformMsg.MsgEnum.Move:
                    SpawnIterator(Move);
                    break;
                case FromWinformMsg.MsgEnum.Movetoposition:
                    Console.WriteLine("Inverse Kinematics Testing:");
                    SpawnIterator(Movetoposition);
                    break;
                case FromWinformMsg.MsgEnum.Bouncing:
                    SpawnIterator(Bouncing);
                    break;
                case FromWinformMsg.MsgEnum.Stop:
                    stillmoving = false;
                    break;
                case FromWinformMsg.MsgEnum.Pacing:
                    SpawnIterator(Pacing);
                    break;
                case FromWinformMsg.MsgEnum.Set:
                    SpawnIterator(Set);
                    break;
                case FromWinformMsg.MsgEnum.Reset:
                    SpawnIterator(Reset);
                    break;
                case FromWinformMsg.MsgEnum.Crawling:
                    SpawnIterator(Crawling);
                    break;
                case FromWinformMsg.MsgEnum.Background1:
                    AddGround1();
                    break;
                case FromWinformMsg.MsgEnum.Ankleposition:
                    Getankleposition();
                    break;
                case FromWinformMsg.MsgEnum.Hipposition:
                    Gethipposition();
                    break;
                case FromWinformMsg.MsgEnum.Background2:
                    AddGround2();
                    break;
                case FromWinformMsg.MsgEnum.Background3:
                    AddGround3();
                    break;
                case FromWinformMsg.MsgEnum.Test:
                    SpawnIterator(Test);
                    break;
            }
            yield break;
        }
        #endregion
    }

    /// <summary>
    /// Defines a new entity type that overrides the ParentJoint with 
    /// custom joint properties.  It also handles serialization and
    /// deserialization properly.  The simulated arm is built from
    /// these entities.
    /// </summary>
    [DataContract]
    public class SingleShapeSegmentEntity : SingleShapeEntity
    {
        private Joint _customJoint;

        [DataMember]
        public Joint CustomJoint
        {
            get { return _customJoint; }
            set { _customJoint = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SingleShapeSegmentEntity() { }

        /// <summary>
        /// Initialization constructor
        /// </summary>
        /// <param name="shape"></param>
        /// <param name="initialPos"></param>
        public SingleShapeSegmentEntity(Shape shape, Vector3 initialPos)
            : base(shape, initialPos)
        {
        }

        // Override the Initialize method to add code to replace the ParentJoint
        public override void Initialize(Microsoft.Xna.Framework.Graphics.GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            base.Initialize(device, physicsEngine);

            // update the parent joint to match our custom joint parameters
            if (_customJoint != null)
            {
                if (ParentJoint != null)
                    PhysicsEngine.DeleteJoint((PhysicsJoint)ParentJoint);

                // restore the entity pointers in _customJoint after deserialization if necessary
                if (_customJoint.State.Connectors[0].Entity == null)
                    _customJoint.State.Connectors[0].Entity = FindConnectedEntity(_customJoint.State.Connectors[0].EntityName, this);

                if (_customJoint.State.Connectors[1].Entity == null)
                    _customJoint.State.Connectors[1].Entity = FindConnectedEntity(_customJoint.State.Connectors[1].EntityName, this);

                ParentJoint = _customJoint;
                PhysicsEngine.InsertJoint((PhysicsJoint)ParentJoint);
            }
        }

        // Traverse to the top-level parent and search all of its descendents for the specified entity name
        VisualEntity FindConnectedEntity(string name, VisualEntity me)
        {
            // find the parent at the top of the hierarchy
            while (me.Parent != null)
                me = me.Parent;

            // now traverse the hierarchy looking for the name
            return FindConnectedEntityHelper(name, me);
        }

        VisualEntity FindConnectedEntityHelper(string name, VisualEntity me)
        {
            if (me.State.Name == name)
                return me;

            foreach (VisualEntity child in me.Children)
            {
                VisualEntity result = FindConnectedEntityHelper(name, child);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// Override the base PreSerialize method so that we can properly serialize joints
        /// </summary>
        public override void PreSerialize()
        {
            base.PreSerialize();
            PrepareJointsForSerialization();
        }
    }

    public static class Legattribute
    {
        public static Trajectory _trajectory1 = null;
        public static float upperlegL1, lowerlegL2, upperlegR1, lowerlegR2, upperlegW1, lowerlegW2, hipweight, ankleweight, ankleradius, baseweight;

    }
    //------------------------------ The Oneleg Entity -----------------------------
    // The Quadruped robot leg consists of a hip entity of type Oneleg with the following child entities
    // which are of type SingleShapeSegmentEntity in the following hierarchy:
    // Oneleg
    //   HipJoint - Represents the hip joint, joint is _joints[0] which controls base rotation
    //     UpperLeg - Represents the upper leg segment, _joints[1] controls upplerleg movement
    //       LowerLeg - Represents the lower leg segment, _joints[2] controls lowerleg movement
    [DataContract]
    public class Oneleg : SingleShapeSegmentEntity
    {
        // physical attributes of the leg
        static float L1 = Legattribute.upperlegL1;//upper leg length
        static float L2 = Legattribute.lowerlegL2;//lower leg length
        static float R1 = Legattribute.upperlegR1;//upper leg radius
        static float R2 = Legattribute.lowerlegR2;//lower leg radius
        static float W1 = Legattribute.upperlegW1;//upper leg weight
        static float W2 = Legattribute.lowerlegW2;//lower leg weight
        static float R3 = Legattribute.ankleradius;//upper leg radius
        static float HW = Legattribute.hipweight;//lower leg radius
        static float AW = Legattribute.ankleweight;//upper leg weight
        static float Basew = Legattribute.baseweight;//lower leg weight
        public SingleShapeSegmentEntity HipJoint, UpperLeg, LowerLeg, Ankle;
        // This class holds a description of each of the joints in the leg.
        class JointDesc
        {
            public string Name;
            public float Min;  // minimum allowable angle
            public float Max;  // maximum allowable angle
            public PhysicsJoint Joint; // Phyics Joint
            public float Target;  // Target joint position
            public float Current;  // Current joint position
            public float Speed;  // Rate of moving toward the target position
            public JointDesc(string name, float min, float max)
            {
                Name = name; Min = min; Max = max;
                Joint = null;
                Current = Target = 0;
                Speed = 30;
            }

            // Returns true if the specified target is within the valid bounds
            public bool ValidTarget(float target)
            {
                return ((target >= Min) && (target <= Max));
            }

            // Returns true if the joint is not yet at the target position
            public bool NeedToMove(float epsilon)
            {
                if (Joint == null) return false;
                return (Math.Abs(Target - Current) > epsilon);
            }

            // Takes one step toward the target position based on the specified time
            public void UpdateCurrent(double time)
            {
                float delta = (float)(time * Speed);
                if (Target > Current)
                    Current = Math.Min(Current + delta, Target);
                else
                    Current = Math.Max(Current - delta, Target);
            }
        }

        // Initialize an array of descriptions for each joint in the leg
        JointDesc[] _joints = new JointDesc[]
        {
            new JointDesc("Hip", -180, 180),
            new JointDesc("Upplerleg", -90,90),
            new JointDesc("Lowerleg", -180,180),
          //  new JointDesc("Ankle",-90,90)
        };

        // default constructor (used in the case of deserialization)
        public Oneleg() { }

        // initialize constructor
        public Oneleg(string name, Vector3 position)
        {
            // The physics shape for the base is slightly lower than the actual hip
            // so that the upper-leg segment does not intersect it as it moves around.
            State.Name = name;
            State.Pose.Position = position;
            State.Pose.Orientation = new Quaternion(0, 0, 0, 1);
            // State.Assets.Mesh = "hip.obj";
            State.Flags = EntitySimulationModifiers.Dynamic;
            // build the hip
            BoxShape = new BoxShape(new BoxShapeProperties(
                "hip" + name,
                Basew, // mass
                new Pose(new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 1)),
                new Vector3(2 * R1, 2 * R1, 2 * R1)));
            BoxShape.State.DiffuseColor = new Vector4(0.8f, 0.8f, 0.8f, 1);

            // build and position hip joint
            SphereShape hipSphere = new SphereShape(new SphereShapeProperties(
                "hipSphere" + name,
                HW,
                new Pose(new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 1)),
                R1));
            hipSphere.State.DiffuseColor = new Vector4(0.8f, 0.8f, 0.8f, 1);
            HipJoint = new SingleShapeSegmentEntity(hipSphere, position - new Vector3(0, 2 * R1, 0));
            HipJoint.State.Pose.Orientation = new Quaternion(0, 0, 0, 1);
            HipJoint.State.Name = name + "_hip";
            //   HipJoint.State.Assets.Mesh = "hip_joint.obj";
            JointAngularProperties hipAngular = new JointAngularProperties();
            hipAngular.Swing1Mode = JointDOFMode.Free;
            hipAngular.SwingDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(50000000, 1000, 0), 100000000);
            EntityJointConnector[] L0Connectors = new EntityJointConnector[2]
            {
                new EntityJointConnector(HipJoint, new Vector3(0,1,0), new Vector3(1,0,0), new Vector3(0,R1, 0)),
                new EntityJointConnector(this, new Vector3(0,1,0), new Vector3(1,0,0), new Vector3(0, -R1, 0))
            };
            HipJoint.CustomJoint = new Joint();
            HipJoint.CustomJoint.State = new JointProperties(hipAngular, L0Connectors);
            HipJoint.CustomJoint.State.Name = "Hip " + name + "|-90|90|";
            this.InsertEntityGlobal(HipJoint);
            // build and position L1 (upper leg)
            CapsuleShape L1Capsule = new CapsuleShape(new CapsuleShapeProperties(
                "L1Capsule" + name,
                W1,
                new Pose(new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 1)),
                R1,
                L1));
            L1Capsule.State.DiffuseColor = new Vector4(0.8f, 0.8f, 0.8f, 1);

            UpperLeg = new SingleShapeSegmentEntity(L1Capsule, position + new Vector3(0, -L1 / 2 - 2 * R1, 0));
            UpperLeg.State.Pose.Orientation = new Quaternion(0, 0, 0, 1);
            UpperLeg.State.Name = name + "_L1";
            JointAngularProperties L1Angular = new JointAngularProperties();
            L1Angular.TwistMode = JointDOFMode.Free;
            L1Angular.TwistDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(50000000, 1000, 0), 100000000);
            EntityJointConnector[] L1Connectors = new EntityJointConnector[2]
            {
                new EntityJointConnector(UpperLeg, new Vector3(0,1,0), new Vector3(0,0,1), new Vector3(0, L1/2, 0)),
                new EntityJointConnector(HipJoint, new Vector3(0,1,0), new Vector3(0,0,1), new Vector3(0, 0, 0))
            };
            UpperLeg.CustomJoint = new Joint();
            UpperLeg.CustomJoint.State = new JointProperties(L1Angular, L1Connectors);
            UpperLeg.CustomJoint.State.Name = "Upper Leg" + name + "|-90|90|";
            //  UpperLeg.State.Assets.Mesh = "upperleg.obj";
            HipJoint.InsertEntityGlobal(UpperLeg);
            // build and position L2 (lower leg)
            CapsuleShape L2Capsule = new CapsuleShape(new CapsuleShapeProperties(
                "L2Capsule",
                W2,
                new Pose(new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 1)),
                R2,
                L2));
            L2Capsule.State.DiffuseColor = new Vector4(0.8f, 0.8f, 0.8f, 1);
            LowerLeg = new SingleShapeSegmentEntity(L2Capsule, position + new Vector3(0, -R1 * 2 - L1 - L2 / 2, 0));
            LowerLeg.State.Name = name + "_L2";
            LowerLeg.State.Pose.Orientation = new Quaternion(0, 0, 0, 1);
            JointAngularProperties L2Angular = new JointAngularProperties();
            L2Angular.TwistMode = JointDOFMode.Free;
            L2Angular.TwistDrive = new JointDriveProperties(JointDriveMode.Position, new SpringProperties(50000000, 1000, 0), 100000000);
            EntityJointConnector[] L2Connectors = new EntityJointConnector[2]
            {
                new EntityJointConnector(LowerLeg, new Vector3(0,1,0), new Vector3(0,0,1), new Vector3(0, L2/2, 0)),
                new EntityJointConnector(UpperLeg, new Vector3(0,1,0), new Vector3(0,0,1), new Vector3(0, -L1/2, 0))
            };
            LowerLeg.CustomJoint = new Joint();
            LowerLeg.CustomJoint.State = new JointProperties(L2Angular, L2Connectors);
            LowerLeg.CustomJoint.State.Name = "Lower Leg" + name + "|-180|180|";
            // LowerLeg.State.Assets.Mesh = "lowerleg.obj";
            UpperLeg.InsertEntityGlobal(LowerLeg);
            BoxShape ankleShape = new BoxShape(new BoxShapeProperties(
                "L0Sphere" + name,
                AW,
                new Pose(new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 1)),
                new Vector3(2 * R3, 2 * R3, 2 * R3)));
            /* SphereShape ankleShape = new SphereShape(new SphereShapeProperties(
                  "L0Sphere" + name,
                  AW,
                  new Pose(new Vector3(0, 0, 0), new Quaternion(0, 0, 0, 1)),
                  R3));*/
            ankleShape.State.DiffuseColor = new Vector4(0.8f, 0.8f, 0.8f, 1);
            ankleShape.State.Material = new MaterialProperties("ankle",
                    0.9f, // restitution
                    0.9f, // dynamic friction
                    0.9f); // static friction
            Ankle = new SingleShapeSegmentEntity(ankleShape, position + new Vector3(0, -R1 * 2 - L1 - L2 / 2, 0));
            Ankle.State.Name = name + "_ankle";
            //   Ankle.State.Assets.Mesh = "foot.obj";
            Ankle.State.Pose.Orientation = new Quaternion(0, 0, 0, 1);
            JointAngularProperties ankleAngular = new JointAngularProperties();
            EntityJointConnector[] ankleConnectors = new EntityJointConnector[2]
            {
                new EntityJointConnector(Ankle, new Vector3(1,0,0), new Vector3(0,0,1), new Vector3(0, 0, 0)),
                new EntityJointConnector(LowerLeg, new Vector3(0,1,0), new Vector3(0,0,1), new Vector3(0, -L2/2, 0))
            };
            Ankle.CustomJoint = new Joint();
            Ankle.CustomJoint.State = new JointProperties(ankleAngular, ankleConnectors);
            Ankle.CustomJoint.State.Name = "Ankle" + name;
            //   Ankle.State.Assets.Mesh = "ankle.obj";
            LowerLeg.InsertEntityGlobal(Ankle);

        }
        // These variables are used to keep track of the state of the arm while it is moving
        bool _moveToActive = false;
        const float _epsilon = 0.01f;
        SuccessFailurePort _moveToResponsePort = null;
        double _prevTime = 0;
        // This method updates the hip entity and then updates the joints if a movement is active.
        public override void Update(FrameUpdate update)
        {
            // The joint member in each joint description needs to be set after all of the joints
            // have been created.  If this has not been done yet, do it now.
            if (_joints[0].Joint == null)
            {
                VisualEntity entity = this;
                if (entity.Children.Count > 0)
                {
                    entity = entity.Children[0];
                    _joints[0].Joint = (PhysicsJoint)entity.ParentJoint;
                    if (entity.Children.Count > 0)
                    {
                        entity = entity.Children[0];
                        _joints[1].Joint = (PhysicsJoint)entity.ParentJoint;
                        if (entity.Children.Count > 0)
                        {
                            entity = entity.Children[0];
                            _joints[2].Joint = (PhysicsJoint)entity.ParentJoint;
                        }
                    }
                }
            }
            base.Update(update);

            // update joints if necessary
            if (_moveToActive)
            {
                bool done = true;
                // Check each joint and update it if necessary.
                if (_joints[0].NeedToMove(_epsilon))
                {
                    done = false;

                    Vector3 normal = _joints[0].Joint.State.Connectors[0].JointNormal;
                    _joints[0].UpdateCurrent(_prevTime);
                    _joints[0].Joint.SetAngularDriveOrientation(
                        Quaternion.FromAxisAngle(normal.X, normal.Y, normal.Z, Conversions.DegreesToRadians(_joints[0].Current)));
                }
                if (_joints[1].NeedToMove(_epsilon))
                {
                    done = false;

                    Vector3 axis = _joints[1].Joint.State.Connectors[1].JointAxis;
                    _joints[1].UpdateCurrent(_prevTime);
                    _joints[1].Joint.SetAngularDriveOrientation(
                        Quaternion.FromAxisAngle(1, 0, 0, Conversions.DegreesToRadians(_joints[1].Current)));
                }

                if (_joints[2].NeedToMove(_epsilon))
                {
                    done = false;

                    Vector3 axis = _joints[2].Joint.State.Connectors[0].JointAxis;
                    _joints[2].UpdateCurrent(_prevTime);
                    _joints[2].Joint.SetAngularDriveOrientation(
                        Quaternion.FromAxisAngle(1, 0, 0, Conversions.DegreesToRadians(_joints[2].Current)));
                }

                if (done)
                {
                    // no joints needed to be updated, the movement is finished
                    _moveToActive = false;
                    _moveToResponsePort.Post(new SuccessResult());
                }
            }

            _prevTime = update.ElapsedTime;
        }
        public void Deactivate()
        {
            _moveToActive = false;
        }
        // This is the basic method used to move the leg.  The target position for each joint is specified along
        // with a time for the movement to be completed.  A port is returned which will receive a success message when the 
        // movement is completed or an exception message if an error is encountered.
        public SuccessFailurePort MoveTo(
            float hipVal,
            float upplerlegVal,
            float lowerlegVal,
            float time)
        {
            SuccessFailurePort responsePort = new SuccessFailurePort();

            if (_moveToActive)
            {
                responsePort.Post(new Exception("Previous MoveTo still active."));
                return responsePort;
            }

            // check bounds.  If the target is invalid, post an exception message to the response port with a helpful error.
            if (!_joints[0].ValidTarget(hipVal))
            {
                responsePort.Post(new Exception(_joints[0].Name + "Joint set to invalid value: " + hipVal.ToString()));
                return responsePort;
            }

            if (!_joints[1].ValidTarget(upplerlegVal))
            {
                responsePort.Post(new Exception(_joints[1].Name + "Joint set to invalid value: " + upplerlegVal.ToString()));
                return responsePort;
            }

            if (!_joints[2].ValidTarget(lowerlegVal))
            {
                responsePort.Post(new Exception(_joints[2].Name + "Joint set to invalid value: " + lowerlegVal.ToString()));
                return responsePort;
            }

            // set the target values on the joint descriptors
            _joints[0].Target = -hipVal;
            _joints[1].Target = -upplerlegVal;
            _joints[2].Target = -lowerlegVal;

            // calculate a speed value for each joint that will cause it to complete its motion in the specified time
            for (int i = 0; i < 3; i++)
                _joints[i].Speed = Math.Abs(_joints[i].Target - _joints[i].Current) / time;

            // set this flag so that the motion is evaluated in the update method
            _moveToActive = true;

            // keep a pointer to the response port so we can post a result message to it.
            _moveToResponsePort = responsePort;

            return responsePort;
        }
    }

    #region WinForms communication
    public class FromWinformEvents : Port<FromWinformMsg>
    {
    }

    public class FromWinformMsg
    {
        public enum MsgEnum
        {
            Loaded,//load the control panel form
            Background1,//choose the grassland background
            Background2,//choose the mountain background
            Background3,
            Build,//build the customized robot
            Reset,//reset the robot state to the ready state  
            Crawling,//kinematics streching pattern
            Pacing,//pacing gait
            Trotting,//first trotting gait
            Trotting2,//second trotting gait
            Bouncing,//bouncing gait
            Move,
            Movetoposition,
            Set,
            Stop,
            Lfconfirm,
            Lbconfirm,
            Rfconfirm,
            Rbconfirm,
            Ankleposition,
            Hipposition,
            Test
        }

        private string[] _parameters;
        public string[] Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        private MsgEnum _command;
        public MsgEnum Command
        {
            get { return _command; }
            set { _command = value; }
        }

        private object _object;
        public object Object
        {
            get { return _object; }
            set { _object = value; }
        }

        public FromWinformMsg(MsgEnum command, string[] parameters)
        {
            _command = command;
            _parameters = parameters;
        }
        public FromWinformMsg(MsgEnum command, string[] parameters, object objectParam)
        {
            _command = command;
            _parameters = parameters;
            _object = objectParam;
        }
    }
    #endregion


    // Utility methods combined into a single class
    public class Conversions
    {
        public static float DegreesToRadians(float degrees)
        {
            return (float)(degrees * Math.PI / 180);
        }

        public static float RadiansToDegrees(float radians)
        {
            return (float)(radians * 180 / Math.PI);
        }

        public static float InchesToMeters(float inches)
        {
            return (float)(inches * 0.0254);
        }
        public static int LocationToPixel(float location)
        {
            return (int)((location) * 16);
        }
    }
    public class AttachedCameraModel : Camera
    {
        public VisualEntity Parent = null;
        public override void Update(double elapsedRealTime, bool hasFocus)
        {
            // no need to update this camera, the keyboard and mouse don't affect it
        }
        public override void SetViewParameters(Microsoft.Xna.Framework.Vector3 eyePt, Microsoft.Xna.Framework.Vector3 lookAtPt)
        {
            // if there is a parent set, the camera matrix is the inverse of the parent world matrix.
            if (Parent != null)
                _viewMatrix = xna.Matrix.Invert(Parent.World);
        }
    }

    class AttachedCameraEntity : CameraEntity
    {
        public AttachedCameraEntity()
            : base()
        {
        }

        public override void Initialize(Microsoft.Xna.Framework.Graphics.GraphicsDevice device, PhysicsEngine physicsEngine)
        {
            base.Initialize(device, physicsEngine);

            // replace the camera model used by this camera entity using reflection
            System.Reflection.FieldInfo fInfo = GetType().BaseType.GetField("_frameworkCamera",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            if (fInfo != null)
            {
                AttachedCameraModel newCamera = new AttachedCameraModel();
                newCamera.Parent = this;
                fInfo.SetValue(this, newCamera);
            }

            SetProjectionParameters(
                ViewAngle,
                ((float)ViewSizeX / (float)ViewSizeY),
                Near,
                Far,
                ViewSizeX,
                ViewSizeY);
        }
    }

    public static class Contract
    {
        public const string Identifier = "http://schemas.tempuri.org/2011/07/quadrupedrobot.html";
    }

    [ServicePort]
    public class QuadrupedRobotOperations : PortSet<DsspDefaultLookup, DsspDefaultDrop>
    {
    }
}
