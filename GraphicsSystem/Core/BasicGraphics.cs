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
    public class BasicGraphics : IGraphics
    {
        private uint _width, _height;
        private uint[] _buffer;
        private Debugger _debugger;

        public void Initialize(Debugger debugger, ref uint[] buffer, int width, int height)
        {
            _debugger = debugger;
            _buffer = buffer;
            this._width = (uint) width;
            this._height = (uint) height;
            //_debugger.Send(_buffer.Length.ToString());
        }

        public void Update()
        {

        }

        public void SetPixel(uint x, uint y, uint color)
        {
            if (x >= 0 && x < _width && y >= 0 && y < _height)
            {
                if (x + y * _width > _width * _height)
                {
                    throw new System.Exception("Tried setting a pixel outside of the screen _width and _height");
                }
                else
                {
                    _buffer[x + y * _width] = color;
                }
            }
        }

        public void DrawRectangle(uint startX, uint startY, uint endX, uint endY, uint color, bool border,
            uint borderColor, uint borderThickness)
        {
            int width = (int) (endX - startX);
            int height = (int) (endY - startY);

            startX -= 1;
            startY -= 1;
            endX -= 1;
            endY -= 1;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _buffer[(startX + x) + ((startY + y) * width)] = color;
                }
            }

            for (int b = 0; b < borderThickness; b++)
            {
                for (int y = 0; y < height; y++)
                {
                    _buffer[(startX + b) + ((startY + y) * width)] = borderColor;
                    _buffer[(endX - b) + ((startY + y) * width)] = borderColor;
                }

                for (int x = 0; x < width; x++)
                {
                    _buffer[(startX + x) + ((startY + b) * width)] = borderColor;
                    _buffer[(startX + x) + ((endY - b) * width)] = borderColor;
                }
            }
        }

        public void DrawImage(Image image, uint x, uint y)
        {
            if (image.Width > _width || image.Height > _height) return;
            if (x <= 0 && x > _width && y <= 0 && y > _height) return;


            uint width = image.Width;
            uint height = image.Height;

            for (uint w = 0; w < width; w++)
            {
                for (uint h = 0; h < height; h++)
                {
                    uint index = w + (h * width);
                    uint bIndex = (x + w) + ((h + y) * _width);
                    _buffer[bIndex] = (uint)image.rawData[index];
                }
            }
        }

        public void DrawCircle(uint x, uint y, uint radius, uint color, bool border, uint borderColor, uint borderThickness)
        {
            if (x <= 0 && x > _width && y <= 0 && y > _height) return;
            
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

        public unsafe void DrawBitmapFromData(int aX, int aY, int aWidth, int aHeight, Bitmap data)
        {
            fixed (uint* bufferPtr = &_buffer[0])
            {
                fixed (int* falseImgPtr = &data.rawData[0])
                {
                    uint* imgPtr = (uint*) falseImgPtr;
                    for (int y = 0; y < aHeight; y++)
                    {
                        MemoryOperations.Copy(bufferPtr + aX + (aY + y) * _width, imgPtr + y * aWidth, aWidth);
                    }
                }
            }
        }

    }
}