﻿using System;
using System.Windows.Forms;
using BulletSharp;
using DemoFramework;

namespace VehicleDemo
{
    class VehicleDemo : Demo
    {
        Vector3 eye = new Vector3(35, 45, -55);
        Vector3 target = Vector3.Zero;

        //bool UseTrimeshGround = false;
        //string heightfieldFile = "data/heightfield128x128.raw";

        const int rightIndex = 0;
        const int upIndex = 1;
        const int forwardIndex = 2;
        Vector3 wheelDirectionCS0 = new Vector3(0, -1, 0);
        Vector3 wheelAxleCS = new Vector3(-1, 0, 0);

        const int maxProxies = 32766;
        const int maxOverlap = 65535;

        // btRaycastVehicle is the interface for the constraint that implements the raycast vehicle
        // notice that for higher-quality slow-moving vehicles, another approach might be better
        // implementing explicit hinged-wheel constraints with cylinder collision, rather then raycasts
        float gEngineForce = 0.0f;
        float gBreakingForce = 0.0f;

        const float maxEngineForce = 1500.0f;//this should be engine/velocity dependent
        const float maxBreakingForce = 100.0f;

        float gVehicleSteering = 0.0f;
        const float steeringIncrement = 1.0f;
        const float steeringClamp = 0.3f;
        public const float wheelRadius = 0.7f;
        public const float wheelWidth = 0.4f;
        const float wheelFriction = 1000;//BT_LARGE_FLOAT;
        const float suspensionStiffness = 20.0f;
        const float suspensionDamping = 2.3f;
        const float suspensionCompression = 4.4f;
        const float rollInfluence = 0.1f;//1.0f;

        const float suspensionRestLength = 0.6f;
        const float CUBE_HALF_EXTENTS = 1;

        public RaycastVehicle vehicle;
        //public CustomVehicle vehicle;

        protected override void OnInitialize()
        {
            Freelook.SetEyeTarget(eye, target);

            Graphics.SetFormText("BulletSharp - Vehicle Demo");
            Graphics.SetInfoText("Move using mouse and WASD+shift\n" +
                "F3 - Toggle debug\n" +
                //"F11 - Toggle fullscreen\n" +
                "Space - Shoot box, break\n" +
                "Drive with arrow keys");

            Graphics.FarPlane = 600.0f;
            //DebugDrawMode = DebugDrawModes.DrawAabb;
            IsDebugDrawEnabled = true;
        }

        protected override void OnInitializePhysics()
        {
            CollisionShape groundShape = new BoxShape(50, 3, 50);
            CollisionShapes.Add(groundShape);

            CollisionConf = new DefaultCollisionConfiguration();
            Dispatcher = new CollisionDispatcher(CollisionConf);
            Solver = new SequentialImpulseConstraintSolver();

            Vector3 worldMin = new Vector3(-10000, -10000, -10000);
            Vector3 worldMax = new Vector3(10000, 10000, 10000);
            Broadphase = new AxisSweep3(worldMin, worldMax);
            //Broadphase = new DbvtBroadphase();

            World = new DiscreteDynamicsWorld(Dispatcher, Broadphase, Solver, CollisionConf);

            int i;
            Matrix tr;
            Matrix vehicleTr;
            //if (UseTrimeshGround)
            //{
                const float scale = 20.0f;

                //create a triangle-mesh ground
                int vertStride = Vector3.SizeInBytes;
                int indexStride = 3 * sizeof(int);

                const int NUM_VERTS_X = 20;
                const int NUM_VERTS_Y = 20;
                const int totalVerts = NUM_VERTS_X * NUM_VERTS_Y;

                const int totalTriangles = 2 * (NUM_VERTS_X - 1) * (NUM_VERTS_Y - 1);

                TriangleIndexVertexArray vertexArray = new TriangleIndexVertexArray();
                IndexedMesh mesh = new IndexedMesh();
                mesh.Allocate(totalTriangles, totalVerts, indexStride, vertStride, PhyScalarType.Int32, PhyScalarType.Single);

                using (var data = mesh.LockVerts())
                {
                    for (i = 0; i < NUM_VERTS_X; i++)
                    {
                        for (int j = 0; j < NUM_VERTS_Y; j++)
                        {
                            float wl = .2f;
                            float height = 20.0f * (float)(Math.Sin(i * wl) * Math.Cos(j * wl));

                            data.Write((i - NUM_VERTS_X * 0.5f) * scale);
                            data.Write(height);
                            data.Write((j - NUM_VERTS_Y * 0.5f) * scale);
                        }
                    }
                }

                int index = 0;
                IntArray idata = mesh.TriangleIndices;
                for (i = 0; i < NUM_VERTS_X - 1; i++)
                {
                    for (int j = 0; j < NUM_VERTS_Y - 1; j++)
                    {
                        idata[index++] = j * NUM_VERTS_X + i;
                        idata[index++] = j * NUM_VERTS_X + i + 1;
                        idata[index++] = (j + 1) * NUM_VERTS_X + i + 1;

                        idata[index++] = j * NUM_VERTS_X + i;
                        idata[index++] = (j + 1) * NUM_VERTS_X + i + 1;
                        idata[index++] = (j + 1) * NUM_VERTS_X + i;
                    }
                }

                vertexArray.AddIndexedMesh(mesh);
                groundShape = new BvhTriangleMeshShape(vertexArray, true);

                tr = Matrix.Identity;
                vehicleTr = Matrix.Translation(0, -2, 0);
            /*}
            else
            {
                // Use HeightfieldTerrainShape

                int width = 40, length = 40;
                //int width = 128, length = 128; // Debugging is too slow for this
                float maxHeight = 10.0f;
                float heightScale = maxHeight / 256.0f;
                Vector3 scale = new Vector3(20.0f, maxHeight, 20.0f);

                //PhyScalarType scalarType = PhyScalarType.PhyUChar;
                //FileStream file = new FileStream(heightfieldFile, FileMode.Open, FileAccess.Read);

                // Use float data
                PhyScalarType scalarType = PhyScalarType.PhyFloat;
                byte[] terr = new byte[width * length * 4];
                MemoryStream file = new MemoryStream(terr);
                BinaryWriter writer = new BinaryWriter(file);
                for (i = 0; i < width; i++)
                    for (int j = 0; j < length; j++)
                        writer.Write((float)((maxHeight / 2) + 4 * Math.Sin(j * 0.5f) * Math.Cos(i)));
                writer.Flush();
                file.Position = 0;

                HeightfieldTerrainShape heightterrainShape = new HeightfieldTerrainShape(width, length,
                    file, heightScale, 0, maxHeight, upIndex, scalarType, false);
                heightterrainShape.SetUseDiamondSubdivision(true);

                groundShape = heightterrainShape;
                groundShape.LocalScaling = new Vector3(scale.X, 1, scale.Z);

                tr = Matrix.Translation(new Vector3(-scale.X / 2, scale.Y / 2, -scale.Z / 2));
                vehicleTr = Matrix.Translation(new Vector3(20, 3, -3));


                // Create graphics object

                file.Position = 0;
                BinaryReader reader = new BinaryReader(file);

                int totalTriangles = (width - 1) * (length - 1) * 2;
                int totalVerts = width * length;

                game.groundMesh = new Mesh(game.Device, totalTriangles, totalVerts,
                    MeshFlags.SystemMemory | MeshFlags.Use32Bit, VertexFormat.Position | VertexFormat.Normal);
                SlimDX.DataStream data = game.groundMesh.LockVertexBuffer(LockFlags.None);
                for (i = 0; i < width; i++)
                {
                    for (int j = 0; j < length; j++)
                    {
                        float height;
                        if (scalarType == PhyScalarType.PhyFloat)
                        {
                            // heightScale isn't applied internally for float data
                            height = reader.ReadSingle();
                        }
                        else if (scalarType == PhyScalarType.PhyUChar)
                        {
                            height = file.ReadByte() * heightScale;
                        }
                        else
                        {
                            height = 0.0f;
                        }

                        data.Write((j - length * 0.5f) * scale.X);
                        data.Write(height);
                        data.Write((i - width * 0.5f) * scale.Z);

                        // Normals will be calculated later
                        data.Position += 12;
                    }
                }
                game.groundMesh.UnlockVertexBuffer();
                file.Close();

                data = game.groundMesh.LockIndexBuffer(LockFlags.None);
                for (i = 0; i < width - 1; i++)
                {
                    for (int j = 0; j < length - 1; j++)
                    {
                        // Using diamond subdivision
                        if ((j + i) % 2 == 0)
                        {
                            data.Write(j * width + i);
                            data.Write((j + 1) * width + i + 1);
                            data.Write(j * width + i + 1);

                            data.Write(j * width + i);
                            data.Write((j + 1) * width + i);
                            data.Write((j + 1) * width + i + 1);
                        }
                        else
                        {
                            data.Write(j * width + i);
                            data.Write((j + 1) * width + i);
                            data.Write(j * width + i + 1);

                            data.Write(j * width + i + 1);
                            data.Write((j + 1) * width + i);
                            data.Write((j + 1) * width + i + 1);
                        }

                        / *
                        // Not using diamond subdivision
                        data.Write(j * width + i);
                        data.Write((j + 1) * width + i);
                        data.Write(j * width + i + 1);

                        data.Write(j * width + i + 1);
                        data.Write((j + 1) * width + i);
                        data.Write((j + 1) * width + i + 1);
                        * /
                    }
                }
                game.groundMesh.UnlockIndexBuffer();

                game.groundMesh.ComputeNormals();
            }*/

            CollisionShapes.Add(groundShape);


            //create ground object
            RigidBody ground = LocalCreateRigidBody(0, tr, groundShape);
            ground.UserObject = "Ground";


            CollisionShape chassisShape = new BoxShape(1.0f, 0.5f, 2.0f);
            CollisionShapes.Add(chassisShape);

            CompoundShape compound = new CompoundShape();
            CollisionShapes.Add(compound);

            //localTrans effectively shifts the center of mass with respect to the chassis
            Matrix localTrans = Matrix.Translation(Vector3.UnitY);
            compound.AddChildShape(localTrans, chassisShape);
            RigidBody carChassis = LocalCreateRigidBody(800, Matrix.Identity, compound);
            carChassis.UserObject = "Chassis";
            //carChassis.SetDamping(0.2f, 0.2f);

            //CylinderShapeX wheelShape = new CylinderShapeX(wheelWidth, wheelRadius, wheelRadius);


            // clientResetScene();

            // create vehicle
            RaycastVehicle.VehicleTuning tuning = new RaycastVehicle.VehicleTuning();
            DefaultVehicleRaycaster vehicleRayCaster = new DefaultVehicleRaycaster(World);
            vehicle = new RaycastVehicle(tuning, carChassis, vehicleRayCaster);
            //vehicle = new CustomVehicle(tuning, carChassis, vehicleRayCaster);

            carChassis.ActivationState = ActivationState.DisableDeactivation;
            World.AddAction(vehicle);


            const float connectionHeight = 1.2f;
            bool isFrontWheel = true;

            // choose coordinate system
            vehicle.SetCoordinateSystem(rightIndex, upIndex, forwardIndex);

            Vector3 connectionPointCS0 = new Vector3(CUBE_HALF_EXTENTS - (0.3f * wheelWidth), connectionHeight, 2 * CUBE_HALF_EXTENTS - wheelRadius);
            vehicle.AddWheel(connectionPointCS0, wheelDirectionCS0, wheelAxleCS, suspensionRestLength, wheelRadius, tuning, isFrontWheel);

            connectionPointCS0 = new Vector3(-CUBE_HALF_EXTENTS + (0.3f * wheelWidth), connectionHeight, 2 * CUBE_HALF_EXTENTS - wheelRadius);
            vehicle.AddWheel(connectionPointCS0, wheelDirectionCS0, wheelAxleCS, suspensionRestLength, wheelRadius, tuning, isFrontWheel);

            isFrontWheel = false;
            connectionPointCS0 = new Vector3(-CUBE_HALF_EXTENTS + (0.3f * wheelWidth), connectionHeight, -2 * CUBE_HALF_EXTENTS + wheelRadius);
            vehicle.AddWheel(connectionPointCS0, wheelDirectionCS0, wheelAxleCS, suspensionRestLength, wheelRadius, tuning, isFrontWheel);

            connectionPointCS0 = new Vector3(CUBE_HALF_EXTENTS - (0.3f * wheelWidth), connectionHeight, -2 * CUBE_HALF_EXTENTS + wheelRadius);
            vehicle.AddWheel(connectionPointCS0, wheelDirectionCS0, wheelAxleCS, suspensionRestLength, wheelRadius, tuning, isFrontWheel);


            for (i = 0; i < vehicle.NumWheels; i++)
            {
                WheelInfo wheel = vehicle.GetWheelInfo(i);
                wheel.SuspensionStiffness = suspensionStiffness;
                wheel.WheelsDampingRelaxation = suspensionDamping;
                wheel.WheelsDampingCompression = suspensionCompression;
                wheel.FrictionSlip = wheelFriction;
                wheel.RollInfluence = rollInfluence;
            }

            vehicle.RigidBody.WorldTransform = vehicleTr;
        }

        public override void OnUpdate()
        {
            gEngineForce *= (1.0f - FrameDelta);

            vehicle.ApplyEngineForce(gEngineForce, 2);
            vehicle.SetBrake(gBreakingForce, 2);
            vehicle.ApplyEngineForce(gEngineForce, 3);
            vehicle.SetBrake(gBreakingForce, 3);

            vehicle.SetSteeringValue(gVehicleSteering, 0);
            vehicle.SetSteeringValue(gVehicleSteering, 1);

            base.OnUpdate();
        }

        public override void OnHandleInput()
        {
            if (Input.KeysDown.Contains(Keys.Left))
            {
                gVehicleSteering -= FrameDelta * steeringIncrement;
                if (gVehicleSteering < -steeringClamp)
                    gVehicleSteering = -steeringClamp;
            }
            else if ((gVehicleSteering + float.Epsilon) < 0)
            {
                gVehicleSteering += FrameDelta * steeringIncrement;
            }

            if (Input.KeysDown.Contains(Keys.Right))
            {
                gVehicleSteering += FrameDelta * steeringIncrement;
                if (gVehicleSteering > steeringClamp)
                    gVehicleSteering = steeringClamp;
            }
            else if ((gVehicleSteering - float.Epsilon) > 0)
            {
                gVehicleSteering -= FrameDelta * steeringIncrement;
            }

            if (Input.KeysDown.Contains(Keys.Up))
            {
                gEngineForce = maxEngineForce;
            }

            if (Input.KeysDown.Contains(Keys.Down))
            {
                gEngineForce = -maxEngineForce;
            }

            if (Input.KeysDown.Contains(Keys.Space))
            {
                gBreakingForce = maxBreakingForce;
            }

            if (Input.KeysReleased.Contains(Keys.Space))
            {
                gBreakingForce = 0;
            }

            base.OnHandleInput();
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            using (Demo demo = new VehicleDemo())
            {
                GraphicsLibraryManager.Run(demo);
            }
        }
    }
}
