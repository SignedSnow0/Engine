using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    /// <summary>
    /// Dati dei vertici di un modello
    /// </summary>
    public class RawModel
    {
        public int VaoHandle { get; private set; }
        public int VertexCount { get; private set; }

        public RawModel(int vaoHandle, int vertexCount)
        {
            VaoHandle = vaoHandle;
            VertexCount = vertexCount;
        }

    }

    /// <summary>
    /// Dati texture di un modello
    /// </summary>
    public class ModelTexture
    {
        public int handle;
        public float shineDamper = 1;
        public float reflectivity = 0;
        public bool hasTransparency = false;
        public bool useFakeLighting = false;
        public int NormalMap;

        public int NumberOfRows { get; set; } = 1;

        public ModelTexture(int handle)
        {
            this.handle = handle;
        }
    }

    /// <summary>
    /// Dati texture e vertici di un modello
    /// </summary>
    public class TexturedModel
    {
        public RawModel model;
        public ModelTexture Texture;

        public TexturedModel(RawModel model, ModelTexture texture)
        {
            this.model = model;
            this.Texture = texture;
        }
    }
}
