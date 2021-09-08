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
    public struct Chunk
    {
        public uint startX, startY, endX, endY;
        public bool bufferChanged;

        public int width, height;
    }


    public class ChunkGraphics : IGraphics
    {
        public Chunk[] chunks = new Chunk[6];

        private uint _width, _height;
        private uint[] _buffer;
        private Debugger _debugger;

        public void Initialize(Debugger debugger, ref uint[] buffer, int width, int height)
        {
            _debugger = debugger;
            _buffer = buffer;
            this._width = (uint)width;
            this._height = (uint)height;
            chunks = GetChunkGrid(9, 16, width, height);
            //_debugger.Send(_buffer.Length.ToString());
        }

        // TODO use a chunk system, split screen into 6 chunks
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
            int width = (int)(endX - startX);
            int height = (int)(endY - startY);


            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _buffer[x + (y * _width)] = color;
                }
            }

            if (border)
            {
                for (int x = 0; x < borderThickness; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        _buffer[(startX + x) + ((startY + y) * width)] = borderColor;
                        _buffer[(endX - x) + ((startY + y) * width)] = borderColor;
                    }
                }

                for (int y = 0; y < borderThickness; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        _buffer[(startX + x) + ((startY + y) * width)] = borderColor;
                        _buffer[(startX + x) + ((endY-y) * width)] = borderColor;
                    }
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

            throw new NotImplementedException();
        }

        public unsafe void DrawBitmapFromData(int aX, int aY, int aWidth, int aHeight, Bitmap data)
        {
            fixed (uint* bufferPtr = &_buffer[0])
            {
                fixed (int* falseImgPtr = &data.rawData[0])
                {
                    uint* imgPtr = (uint*)falseImgPtr;
                    for (int y = 0; y < aHeight; y++)
                    {
                        MemoryOperations.Copy(bufferPtr + aX + (aY + y) * _width, imgPtr + y * aWidth, aWidth);
                    }
                }
            }
        }
        

        public Chunk GetChunk(uint x, uint y)
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

        public void ChangedInChunk(uint x, uint y)
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
