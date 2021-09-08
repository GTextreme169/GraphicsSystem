using System;
using System.Collections.Generic;
using System.Text;
using Cosmos.Debug.Kernel;
using Cosmos.System.Graphics;

namespace GraphicsSystem.Core
{
    interface IGraphics
    {
        void Initialize(Debugger debugger, ref uint[] buffer, int width, int height);
        void Update();

        void DrawRectangle(uint startX, uint startY, uint endX, uint endY, uint color, bool border,
            uint borderColor, uint borderThickness);

        void DrawImage(Image image, uint u, uint u1);
        void DrawCircle(uint u, uint u1, uint radius, uint color, bool border, uint borderColor, uint borderThickness);

        void DrawBitmapFromData(int aX, int aY, int aWidth, int aHeight, Bitmap data);
    }
}
