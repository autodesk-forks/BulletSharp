﻿using System;
using BulletSharp;
using DemoFramework;

namespace BasicDemo
{
    class BasicDemo : Demo
    {
        Vector3 eye = new Vector3(30, 20, 10);
        Vector3 target = new Vector3(0, 5, -4);

        // create 125 (5x5x5) dynamic objects
        const int ArraySizeX = 5, ArraySizeY = 5, ArraySizeZ = 5;

        // scaling of the objects (0.1 = 20 centimeter boxes )
        const float StartPosX = -5;
        const float StartPosY = -5;
        const float StartPosZ = -3;

        protected override void OnInitialize()
        {
            Freelook.SetEyeTarget(eye, target);

            Graphics.SetFormText("BulletSharp - Basic Demo");
            Graphics.SetInfoText("Move using mouse and WASD+shift\n" +
                "F3 - Toggle debug\n" +
                //"F11 - Toggle fullscreen\n" +
                "Space - Shoot box");
        }

        protected override void OnInitializePhysics()
        {
            // collision configuration contains default setup for memory, collision setup
            CollisionConf = new DefaultCollisionConfiguration();
            Dispatcher = new CollisionDispatcher(CollisionConf);

            Broadphase = new DbvtBroadphase();

            World = new DiscreteDynamicsWorld(Dispatcher, Broadphase, null, CollisionConf);
            World.Gravity = new Vector3(0, -10, 0);

            // create the ground
            BoxShape groundShape = new BoxShape(50, 1, 50);
            //groundShape.InitializePolyhedralFeatures();
            //CollisionShape groundShape = new StaticPlaneShape(new Vector3(0,1,0), 50);

            CollisionShapes.Add(groundShape);
            CollisionObject ground = LocalCreateRigidBody(0, Matrix.Identity, groundShape);
            ground.UserObject = "Ground";

            // create a few dynamic rigidbodies
            const float mass = 1.0f;

            BoxShape colShape = new BoxShape(1);
            CollisionShapes.Add(colShape);
            Vector3 localInertia = colShape.CalculateLocalInertia(mass);

            const float start_x = StartPosX - ArraySizeX / 2;
            const float start_y = StartPosY;
            const float start_z = StartPosZ - ArraySizeZ / 2;

            RigidBodyConstructionInfo rbInfo =
                new RigidBodyConstructionInfo(mass, null, colShape, localInertia);

            int k, i, j;
            for (k = 0; k < ArraySizeY; k++)
            {
                for (i = 0; i < ArraySizeX; i++)
                {
                    for (j = 0; j < ArraySizeZ; j++)
                    {
                        Matrix startTransform = Matrix.Translation(
                            2 * i + start_x,
                            2 * k + start_y,
                            2 * j + start_z
                        );

                        // using motionstate is recommended, it provides interpolation capabilities
                        // and only synchronizes 'active' objects
                        rbInfo.MotionState = new DefaultMotionState(startTransform);
                        RigidBody body = new RigidBody(rbInfo);

                        // make it drop from a height
                        body.Translate(new Vector3(0, 20, 0));

                        World.AddRigidBody(body);
                    }
                }
            }

            rbInfo.Dispose();
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            using (Demo demo = new BasicDemo())
            {
                GraphicsLibraryManager.Run(demo);
            }
        }
    }
}
