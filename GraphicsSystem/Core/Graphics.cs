﻿using Cosmos.Debug.Kernel;
using Cosmos.HAL;
using Cosmos.HAL.Drivers.PCI.Video;
using Cosmos.System.Graphics;
using GraphicsSystem.Hardware;
using GraphicsSystem.Types;
using System.Collections.Generic;
using Point = GraphicsSystem.Types.Point;
using Sys = Cosmos.System;

namespace GraphicsSystem.Core
{

    public struct Chunk
    {
        public uint startX, startY, endX, endY;
        public bool bufferChanged;

        public int width, height;
    }

    public static class Graphics
    {
        public static VMWareSVGAII driver;

        public const int width = 720, height = 480;
        public static int FONT_SPACING = 1;
        public static uint[] buffer;
        private static uint[] oldBuffer;
        private static bool bufferChanged = false;
        private static Debugger _debugger;

        public static Chunk[] chunks = new Chunk[6];

        private static int frames;
        public static int fps { get; private set; }
        public static float delta { get; private set; }
        private static int tick;

        public static void Initialize(Debugger debugger)
        {
            _debugger = debugger;
            driver = new VMWareSVGAII();
            driver.SetMode(width, height);
            buffer = new uint[width * height];
            oldBuffer = new uint[width * height];
            _debugger.Send(buffer.Length.ToString());
            Sys.MouseManager.ScreenWidth = width;
            Sys.MouseManager.ScreenHeight = height;

            //int chunksX = 8 / 2;
            //int chunksY = 8 / 4;

            //uint chunksWidth = width / 3;
            //uint chunksHeight = height / 2;
            //int index = 0;
            //for (uint i = 0; i < 3; i++)
            //{
            //    for (uint j = 0; j < 2; j++)
            //    {
            //        chunks[index].endX = (chunks[index].startX = chunksWidth * i) + chunksWidth;
            //        chunks[index].endY = (chunks[index].startY = chunksHeight * j) + chunksHeight;

            //        chunks[index].bufferChanged = false;
            //        chunks[index].width = (int)chunksWidth;
            //        chunks[index].height = (int)chunksHeight;

            //        index++;
            //    }
            //}

            chunks = GetChunkGrid(9, 16, width, height);

        }

        // TODO use a chunk system, split screen into 6 chunks
        public static void Update()
        {
            //if (bufferChanged)
            //{
            //    for (int i = 0; i < width * height; i++)
            //    {
            //        if (buffer[i] != oldBuffer[i])
            //        {

            //            int x = i % width;
            //            int y = i / width;
            //            driver.SetPixel((uint)x, (uint)y, buffer[i]);

            //        }
            //        oldBuffer[i] = buffer[i];
            //    }
            //    driver.Update(0, 0, width, height);
            //    ClearBuffer(Color.gray160);
            //    bufferChanged = false;
            //}

            if (bufferChanged)
            {
                for (int i = 0; i < chunks.Length; i++)
                {
                    if (chunks[i].bufferChanged)
                    {
                        int _width = chunks[i].width;
                        int _height = chunks[i].height;
                        for (uint x = chunks[i].startX; x <= chunks[i].endX; x++)
                        {
                            for (uint y = chunks[i].startY; y <= chunks[i].endY; y++)
                            {
                                int index = (int)(y * width + x);
                                if (buffer[index] != oldBuffer[index])
                                {
                                    driver.SetPixel(x, y, buffer[index]);
                                }
                            }
                        }
                        driver.Update(chunks[i].startX, chunks[i].startY, (uint)_width, (uint)_height);
                        chunks[i].bufferChanged = false;
                    }
                }
                ClearBuffer(Color.gray160);
                bufferChanged = false;
            }

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

        public static void ClearBuffer(uint color = 0)
        {
            for (int i = 0; i < width * height; i++)
            {
                if (buffer[i] != color) { buffer[i] = color; }
            }
        }

        public static void UpdateCursor()
        {
            Point position = Mouse.position;

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
        }

        #region EditBufferMethods

        public static void SetPixel(uint x, uint y, uint color)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                ChangedInChunk(x, y);

                bufferChanged = true;

                buffer[x + y * width] = color;
            }
        }

        public static void Rectangle(uint x, uint y, uint endX, uint endY, uint color, bool border = false, uint borderColor = 0, uint borderThickness = 0)
        {
            if (x <= 0 && x > width && y <= 0 && y > height) return;

            bufferChanged = true;
            if (border)
            {
                uint _width = endX - x;
                uint _height = endY - y;

                for (int i = 0; i < _width; i++)
                {
                    for (int h = 0; h < _height; h++)
                    {
                        if (h < borderThickness || _height - h <= borderThickness)
                        {
                            buffer[(x + i) + (y + h) * width] = borderColor;
                        }
                        else if (_width - i <= borderThickness || i < borderThickness)
                        {
                            buffer[(x + i) + (y + h) * width] = borderColor;
                        }
                        else
                        {
                            buffer[(x + i) + (y + h) * width] = color;
                        }

                    }
                }
            }
            else
            {
                uint _width = endX - x;
                uint _height = endY - y;
                for (int i = 0; i < _width; i++)
                {
                    for (int h = 0; h < _height; h++)
                    {
                        buffer[(x + i) + (y + h) * width] = color;
                    }
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

        public static void DrawImage(Image image, int x, int y)
        {
            if (image.Width > width || image.Height > height) return;
            if (x <= 0 && x > width && y <= 0 && y > height) return;


            bufferChanged = true;
            for (int w = 0; w < image.Width; w++)
            {
                for (int h = 0; h < image.Height; h++)
                {
                    buffer[(x + w) * (y + h) * width] = (uint)image.rawData[w + h * image.Width];
                }
            }
        }

        public static void DrawCircle(uint x, uint y, uint radius, uint color, bool border = false, uint borderColor = 0, uint borderThickness = 0)
        {
            if (x <= 0 && x > width && y <= 0 && y > height) return;
            bufferChanged = true;

            if (border)
            {


                int _x = (int)radius;
                int _y = 0;
                int xChange = (int)(1 - (radius << 1));
                int yChange = 0;
                int radiusError = 0;

                while (_x >= _y)
                {
                    for (int i = (int)(x - _x); i <= x + _x; i++)
                    {
                        SetPixel((uint)i, (uint)(y + _y), borderColor);
                        SetPixel((uint)i, (uint)(y - _y), borderColor);
                    }
                    for (int i = (int)(x - _y); i <= x + _y; i++)
                    {
                        SetPixel((uint)i, (uint)(y + _x), borderColor);
                        SetPixel((uint)i, (uint)(y - _x), borderColor);
                    }

                    _y++;
                    radiusError += yChange;
                    yChange += 2;
                    if (((radiusError << 1) + xChange) > 0)
                    {
                        _x--;
                        radiusError += xChange;
                        xChange += 2;
                    }
                }

                _x = (int)(radius - borderThickness / 2);
                _y = 0;
                xChange = (int)(1 - (radius << 1));
                yChange = 0;
                radiusError = 0;

                while (_x >= _y)
                {
                    for (int i = (int)(x - _x); i <= x + _x; i++)
                    {

                        SetPixel((uint)i, (uint)(y + _y), color);
                        SetPixel((uint)i, (uint)(y - _y), color);
                    }
                    for (int i = (int)(x - _y); i <= x + _y; i++)
                    {
                        SetPixel((uint)i, (uint)(y + _x), color);
                        SetPixel((uint)i, (uint)(y - _x), color);
                    }

                    _y++;
                    radiusError += yChange;
                    yChange += 2;
                    if (((radiusError << 1) + xChange) > 0)
                    {
                        _x--;
                        radiusError += xChange;
                        xChange += 2;
                    }
                }
            }
            else
            {
                int _x = (int)radius;
                int _y = 0;
                int xChange = (int)(1 - (radius << 1));
                int yChange = 0;
                int radiusError = 0;

                while (_x >= _y)
                {
                    for (int i = (int)(x - _x); i <= x + _x; i++)
                    {

                        SetPixel((uint)i, (uint)(y + _y), color);
                        SetPixel((uint)i, (uint)(y - _y), color);
                    }
                    for (int i = (int)(x - _y); i <= x + _y; i++)
                    {
                        SetPixel((uint)i, (uint)(y + _x), color);
                        SetPixel((uint)i, (uint)(y - _x), color);
                    }

                    _y++;
                    radiusError += yChange;
                    yChange += 2;
                    if (((radiusError << 1) + xChange) > 0)
                    {
                        _x--;
                        radiusError += xChange;
                        xChange += 2;
                    }
                }
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


        public static void DrawBitmap(int x, int y, int width, int height, uint color ,uint[] data)
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

        public static void DrawString(uint x, uint y, Font font, string text, uint color = 0)
        {
            if (text.Length > 0 && font != null)
            {
                int xx = (int)x;
                int yy = (int)y;

                foreach (char item in text)
                {
                    DrawChar(xx, yy, item, color, font);

                    if (item == '\n') { yy += font.characterHeight + 1; xx = (int)x; }
                    else { xx += font.characterWidth + FONT_SPACING; }
                }
            }
        }
        #endregion

        public static Chunk GetChunk(uint x, uint y)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                if (chunks[i].startX <= x && chunks[i].endX >= x)
                {
                    if (chunks[i].startY <= y && chunks[i].endY >= y)
                    {
                        return chunks[i];
                    }
                }
            }
            return new Chunk();
        }

        public static void ChangedInChunk(uint x, uint y)
        {
            for (int i = 0; i < chunks.Length; i++)
            {
                if (chunks[i].startX <= x && chunks[i].endX >= x)
                {
                    if (chunks[i].startY <= y && chunks[i].endY >= y)
                    {
                        chunks[i].bufferChanged = true;
                    }
                }
            }
        }
        public static Chunk[] GetChunkGrid(int row, int col, int width, int height)
        {
            Chunk[] tempChunks = new Chunk[row * col];
            uint chunksWidth = (uint)(width / col);
            uint chunksHeight = (uint)(height / row);
            int index = 0;
            for (uint i = 0; i < col; i++)
            {
                for (uint j = 0; j < row; j++)
                {
                    tempChunks[index].endX = (tempChunks[index].startX = chunksWidth * i) + chunksWidth;
                    tempChunks[index].endY = (tempChunks[index].startY = chunksHeight * j) + chunksHeight;
                    tempChunks[index].bufferChanged = true;
                    tempChunks[index].width = (int)chunksWidth;
                    tempChunks[index].height = (int)chunksHeight;
                    index++;
                }
            }
            return tempChunks;
        }
    }
}