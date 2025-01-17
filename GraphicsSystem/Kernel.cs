﻿using System;
using System.Collections.Generic;
using System.Text;
using Sys = Cosmos.System;
using GraphicsSystem.Core;
using GraphicsSystem.Types;
using GraphicsSystem.Math;

namespace GraphicsSystem
{
    public class Kernel : Sys.Kernel
    {
        protected override void BeforeRun()
        {
            Graphics.Initialize(mDebugger);
            Graphic.Taskbar.Initialize();
            //mDebugger.Send("Draw Rectangles");
            //Graphics.Rectangle(10, 10, 40, 40, Color.blue);
            //mDebugger.Send("First Rectangle drawn into memory");
            //Graphics.Rectangle(60, 60, 90, 90, Color.blue, true, Color.white, 2);
            //mDebugger.Send("Done Drawing");
            //Graphics.DrawCircle(60, 20, 6, Color.blue, true, Color.white, 2); 
            //Graphics.DrawCircle(90, 20, 6, Color.blue);
        }

        public char[] fps = new char[] { 'F', 'P', 'S', ':' , ' ', '\0' };
        public char[] final = new char[32];
        public char[] number = new char[32];
        Font font = new FontMono9x11();
        protected override void Run()
        {
            try
            {
                while (true)
                {

                    // Base System Draws
                    Graphic.Taskbar.Draw();



                    //for (int i = 0; i < Graphics.chunks.Length; i++)
                    //{
                    //    Graphics.Rectangle(Graphics.chunks[i].startX, Graphics.chunks[i].startY, Graphics.chunks[i].endX, Graphics.chunks[i].endY, Color.gray160, true, Color.red, 1);
                    //}


                    //ProcessManager.UpdateProcesses();

                    //mDebugger.Send("Cursor Update");

                    Graphics.UpdateCursor(); 

                    //mDebugger.Send("After Cursor Update");

                    //Graphics.Rectangle(10, 10, 40, 40, Color.blue);
                    //Graphics.Rectangle(60, 60, 90, 90, Color.blue, true, Color.white, 2);
                    //Graphics.DrawCircle(60, 20, 6, Color.blue, true, Color.white, 2);
                    //Graphics.DrawCircle(90, 20, 6, Color.blue);
                    InternalString.IntToString(Graphics.fps, ref number);
                    InternalString.combineString(ref fps, ref number, ref final);
                    Graphics.DrawString(0, 0, font, final, Color.white);
                    //Graphics.DrawString(0, 13, new FontMono9x11(), "Time Between Frames: " + Time.TimeBetweenFrames(Graphics.fps));
                    Graphics.Update();
                    //mDebugger.Send("After Update");
                }
            }
            catch (Exception e)
            {

                Panic(e);
            }
        }

        private void Panic(Exception e)
        {
            mDebugger.Send(e.Message);
            Graphics.ClearBuffer(Color.lightBlue);
            //Graphics.DrawString(10, 10, new FontMono9x11(), "FATAL ERROR: " + e.Message);
        }
    }

    public static class Console
    {
        public static void BeforeRun()
        {

        }

        public static void Run()
        {

        }
    }
}
