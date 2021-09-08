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
    internal unsafe struct BufferChunk : IDisposable
    {
        public uint startX, startY, endX, endY;
        public uint width, height;

        public uint* buffer;



        public BufferChunk(uint x, uint y, uint color) : this()
        {
            startX = endX = x;
            startY = endY = y;
            width = height = 1;
            buffer = (uint*)Heap.Alloc(12);
            buffer[0] = color;
        }

        public BufferChunk(uint startX, uint startY, uint endX, uint endY)
        {
            this.startX = startX;
            this.startY = startY;
            this.endX = endX;
            this.endY = endY;
            this.width = (endX - startX);
            this.height = (endY - startY);
            buffer = (uint*)Heap.Alloc(width * height * 12);
        }

        public void Dispose()
        {
            //Heap.Free(buffer);
        }
    }


    public class QueueGraphics : IGraphics
    {
        private Queue<BufferChunk> _bufferChunkQueue;

        private uint _width, _height;
        private uint[] _buffer;
        private Debugger _debugger;

        public void Initialize(Debugger debugger, ref uint[] buffer, int width, int height)
        {
            _debugger = debugger;
            _buffer = buffer;
            this._width = (uint)width;
            this._height = (uint)height;
            _bufferChunkQueue = new Queue<BufferChunk>();
            //_debugger.Send(_buffer.Length.ToString());
        }


        public void Update()
        {
            unsafe
            {
                while (_bufferChunkQueue.TryDequeue(out BufferChunk chunk))
                {
                    fixed (uint* bufferPtr = &_buffer[0])
                    {
                        for (uint y = 0; y < chunk.height; y++)
                        {
                            MemoryOperations.Copy(bufferPtr + chunk.startX + (chunk.startY + y) * _width,
                                chunk.buffer + y * chunk.width, (int)chunk.width);
                        }
                    }
                    chunk.Dispose();
                }
            }
        }
        

        #region EditBufferMethods

        public unsafe void DrawRectangle(uint startX, uint startY, uint endX, uint endY, uint color, bool border,
            uint borderColor, uint borderThickness)
        {
            int width = (int)(endX - startX);
            int height = (int)(endY - startY);

            BufferChunk bufferChunk = new BufferChunk(startX, startY, endX, endY);
            uint* tempBuffer = bufferChunk.buffer;


            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    tempBuffer[x + (y * _width)] = color;
                }
            }

            if (border)
            {
                for (int x = 0; x < borderThickness; x++)
                {
                    int invX = width - x;
                    for (int y = 0; y < height; y++)
                    {
                        tempBuffer[x + (y * width)] = borderColor;
                        tempBuffer[invX + (y * width)] = borderColor;
                    }
                }

                for (int y = 0; y < borderThickness; y++)
                {
                    int invY = height - y;
                    for (int x = 0; x < width; x++)
                    {
                        tempBuffer[x + (y * width)] = borderColor;
                        tempBuffer[x + (invY * width)] = borderColor;
                    }
                }

                _bufferChunkQueue.Enqueue(bufferChunk);
            }
        }
        
        public unsafe void DrawImage(Image image, uint x, uint y)
        {
            if (image.Width > _width || image.Height > _height) return;
            if (x <= 0 && x > _width && y <= 0 && y > _height) return;


            uint width = image.Width;
            uint height = image.Height;

            BufferChunk bufferChunk = new BufferChunk(x, y, x + width, y + height);
            uint* tempBuffer = bufferChunk.buffer;

            for (uint w = 0; w < width; w++)
            {
                for (uint h = 0; h < height; h++)
                {
                    uint index = w + (h * width);
                    tempBuffer[index] = (uint)image.rawData[index];
                }
            }
            _bufferChunkQueue.Enqueue(bufferChunk);
        }

        public void DrawCircle(uint x, uint y, uint radius, uint color, bool border = false, uint borderColor = 0, uint borderThickness = 0)
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
        #endregion
    }
}
