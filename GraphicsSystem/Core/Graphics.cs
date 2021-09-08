using Cosmos.Core;
using Cosmos.Debug.Kernel;
using Cosmos.HAL;
using Cosmos.HAL.Drivers.PCI.Video_plug;
using Cosmos.System.Graphics;
using GraphicsSystem.Hardware;
using GraphicsSystem.Types;
using System;
using System.Collections.Generic;
using Cosmos.Core.Memory;
using Point = GraphicsSystem.Types.Point;
using Sys = Cosmos.System;

namespace GraphicsSystem.Core
{


    public static class Graphics
    {
        private static IGraphics graphics;
        public static VMWareSVGAII driver;

        public const int width = 1920, height = 1080;
        public static int FONT_SPACING = 1;
        public static uint[] buffer;
        

        private static Debugger _debugger;
        

        private static int frames = 0;
        public static int fps { get; private set; } = 0;
        public static float delta { get; private set; } = 0;
        private static int tick = 0;

        public static void Initialize(Debugger debugger)
        {
            _debugger = debugger;
            graphics = new BasicGraphics();
            buffer = new uint[width * height];
            graphics.Initialize(debugger, ref buffer, width, height);
            driver = new VMWareSVGAII();
            driver.SetMode(width, height);
            //_debugger.Send(buffer.Length.ToString());
            Sys.MouseManager.ScreenWidth = width;
            Sys.MouseManager.ScreenHeight = height;
            ClearBuffer(Color.gray160);
        }

        // TODO use a chunk system, split screen into 6 chunks
        public static void Update()
        {
            graphics.Update();

            driver.Video_Memory.Copy(buffer);
            driver.Update(0, 0, width, height);
            ClearBuffer(Color.gray160);

            if (frames > 0) { delta = (float)1000 / (float)frames; }
            int sec = RTC.Second;
            if (tick != sec)
            {
                fps = frames;
                frames = 0;
                tick = sec;
            }
            frames++;
        }

        public static void UpdateCursor()
        {
            Point position = Mouse.position;
            if (position != Mouse.positionOld)
            {
                for (int x = 0; x < 12; x++)
                {
                    for (int y = 0; y < 20; y++)
                    {
                        if (Cursor.arrow[x + y * 12] != Color.gray160)
                        {
                            SetPixel((uint)(position.x + x), (uint)(position.y + y), Cursor.arrow[x + y * 12]);
                        }
                    }
                }
                Mouse.positionOld = position;
            }
        }

       


        #region IGraphics
        public static void Rectangle(uint startX, uint startY, uint endX, uint endY, uint color, bool border = false,
            uint borderColor = 0, uint borderThickness = 0)
        {
            if (startX <= 0 
                && endX > width 
                && startY <= 0 
                && endY > height
                && startX > endX
                && startY > endY) return;

            graphics.DrawRectangle(startX, startY, endX, endY, color, border, borderColor, borderThickness);
        }
        public static void DrawImage(Image image, uint x, uint y)
        {
            graphics.DrawImage(image, x, y);
        }
        public static void DrawCircle(uint x, uint y, uint radius, uint color, bool border = false, uint borderColor = 0, uint borderThickness = 0)
        {
            graphics.DrawCircle(x, y, radius, color, border, borderColor, borderThickness);
        }

        public static void DrawBitmapFromData(int aX, int aY, int aWidth, int aHeight, Bitmap data)
        {
            graphics.DrawBitmapFromData(aX,aY,aWidth,aHeight,data);
        }

        #endregion

        #region EditBufferMethods

        #region Get/Set Pixel
        public static void SetPixel(uint x, uint y, uint color)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                if (x + y * width > width * height)
                {
                    throw new System.Exception("Tried setting a pixel outside of the screen width and height");
                }
                else
                {
                    buffer[x + y * width] = color;
                }
            }
        }

        public static uint GetPixel(uint x, uint y)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                return buffer[x + y * width];
            }
            return Color.black;

        }

        #endregion

        public static unsafe void ClearBuffer(uint color = 0)
        {
            fixed (uint* bufferPtr = &buffer[0])
            {
                MemoryOperations.Fill(bufferPtr, color, width * height);
            }
        }

        public static void DrawChar(int x, int y, char c, uint color, Font font)
        {
            int width = font.characterWidth;
            int height = font.characterHeight;

            if(c == '!') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.exclamation]); }
            else if(c == '"') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.quotation]); }
            else if(c == '#') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.numberSign]); }
            else if(c == '$') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.dollarSign]); }
            else if(c == '%') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.percent]); }
            else if(c == '&') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.ampersand]); }
            else if(c == '\'') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.apostrophe]); }
            else if(c == '(') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.bracketLeft]); }
            else if(c == ')') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.bracketRight]); }
            else if(c == '*') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.multiply]); }
            else if(c == '+') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.add]); }
            else if(c == ',') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.comma]); }
            else if(c == '-') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.minus]); }
            else if(c == '.') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.period]); }
            else if(c == '/') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.slash]); }
            else if(c == '1') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.n1]); }
            else if(c == '2') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.n2]); }
            else if(c == '3') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.n3]); }
            else if(c == '4') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.n4]); }
            else if(c == '5') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.n5]); }
            else if(c == '6') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.n6]); }
            else if(c == '7') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.n7]); }
            else if(c == '8') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.n8]); }
            else if(c == '9') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.n9]); }
            else if(c == '0') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.n0]); }
            else if(c == ':') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.colon]); }
            else if(c == ';') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.semiColon]); }
            else if(c == '<') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.arrowLeft]); }
            else if(c == '=') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.equals]); }
            else if(c == '>') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.arrowRight]); }
            else if(c == '?') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.question]); }
            else if(c == '@') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.at]); }
            else if(c == 'A') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capA]); }
            else if(c == 'B') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capB]); }
            else if(c == 'C') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capC]); }
            else if(c == 'D') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capD]); }
            else if(c == 'E') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capE]); }
            else if(c == 'F') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capF]); }
            else if(c == 'G') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capG]); }
            else if(c == 'H') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capH]); }
            else if(c == 'I') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capI]); }
            else if(c == 'J') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capJ]); }
            else if(c == 'K') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capK]); }
            else if(c == 'L') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capL]); }
            else if(c == 'M') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capM]); }
            else if(c == 'N') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capN]); }
            else if(c == 'O') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capO]); }
            else if(c == 'P') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capP]); }
            else if(c == 'Q') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capQ]); }
            else if(c == 'R') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capR]); }
            else if(c == 'S') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capS]); }
            else if(c == 'T') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capT]); }
            else if(c == 'U') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capU]); }
            else if(c == 'V') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capV]); }
            else if(c == 'W') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capW]); }
            else if(c == 'X') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capX]); }
            else if(c == 'Y') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capY]); }
            else if(c == 'Z') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.capZ]); }
            else if(c == '[') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.sqBracketL]); }
            else if(c == '\\') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.backSlash]); }
            else if(c == ']') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.sqBracketR]); }
            else if(c == '^') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.upArrow]); }
            else if(c == '_') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.underscore]); }
            else if(c == '`') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.tilde]); }
            else if(c == 'a') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.a]); }
            else if(c == 'b') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.b]); }
            else if(c == 'c') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.c]); }
            else if(c == 'd') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.d]); }
            else if(c == 'e') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.e]); }
            else if(c == 'f') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.f]); }
            else if(c == 'g') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.g]); }
            else if(c == 'h') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.h]); }
            else if(c == 'i') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.i]); }
            else if(c == 'j') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.j]); }
            else if(c == 'k') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.k]); }
            else if(c == 'l') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.l]); }
            else if(c == 'm') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.m]); }
            else if(c == 'n') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.n]); }
            else if(c == 'o') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.o]); }
            else if(c == 'p') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.p]); }
            else if(c == 'q') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.q]); }
            else if(c == 'r') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.r]); }
            else if(c == 's') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.s]); }
            else if(c == 't') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.t]); }
            else if(c == 'u') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.u]); }
            else if(c == 'v') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.v]); }
            else if(c == 'w') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.w]); }
            else if(c == 'x') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.x]); }
            else if(c == 'y') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.y]); }
            else if(c == 'z') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.z]); }
            else if(c == '{') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.crBracketL]); }
            else if(c == '|') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.div]); }
            else if(c == '}') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.crBracketR]); }
            else if(c == '~') { DrawBitmap(x, y, width, height, color, font.characters[FontCharIndex.squiggle]); }
        }

        public static void DrawBitmap(int x, int y, int width, int height, uint color, uint[] data)
        {
            for (int i = 0; i < width * height; i++)
            {
                int xx = x + (i % width);
                int yy = y + (i / width);

                if (data[i] != 0)
                {
                    SetPixel((uint)xx, (uint)yy, color);
                }
            }
        }

        public static void DrawString(uint x, uint y, Font font, char[] text, uint color = 0)
        {
            if (text.Length > 0 && font != null)
            {
                int xx = (int)x;
                int yy = (int)y;

                foreach (char item in text)
                {
                    if (item == '\0')
                    {
                        break;
                    }
                    DrawChar(xx, yy, item, color, font);

                    if (item == '\n') { yy += font.characterHeight + 1; xx = (int)x; }
                    else { xx += font.characterWidth + FONT_SPACING; }
                }
            }
        }
        #endregion
    }
}