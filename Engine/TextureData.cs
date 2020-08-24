using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class TextureData
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public BitmapData Data { get; private set; }

        public TextureData(int width, int height, BitmapData bitmapData)
        {
            Width = width;
            Height = height;
            Data = bitmapData;
        }
    }
}
