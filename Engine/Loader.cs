using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;

namespace Engine
{
    /// <summary>
    /// Classe contenente metodi utili al caricamento di diversi dati
    /// </summary>
    public class Loader
    {
        //Tiene traccia di ogni vao e vbo per poi rimuoverli dalla memoria
        private List<int> vaos = new List<int>();
        private List<int> vbos = new List<int>();
        private List<int> textures = new List<int>();

        /// <summary>
        /// Aggiunge dei set di dati in un vertex array object
        /// </summary>
        /// <param name="vertices">I vertici dell`oggetto</param>
        /// <param name="textureCoords">Le coordinate della texture</param>
        /// <param name="normals">I vettori normali delle facce</param>
        /// <param name="indices">Gli indici collegati ai vertici</param>
        /// <returns>Un oggetto RawModel contente un puntatore al vao con i dati</returns>
        public RawModel LoadToVao(float[] vertices, float[] textureCoords, float[] normals, uint[] indices)
        {
            int VAOHandle = CreateVAO();
            BindToIBO(indices);
            StoreData(0, 3, vertices);
            StoreData(1, 2, textureCoords);
            StoreData(2, 3, normals);
            UnbindVAO();
            return new RawModel(VAOHandle, indices.Length);
        }
        public int LoadToVao(float[] vertices, float[] textureCoords)
        {
            int VAOHandle = CreateVAO();
            StoreData(0, 2, vertices);
            StoreData(1, 2, textureCoords);
            UnbindVAO();
            return VAOHandle;
        }
        public RawModel LoadToVao(float[] vertices, float[] textureCoords, float[] normals, float[] tangents, uint[] indices)
        {
            int VAOHandle = CreateVAO();
            BindToIBO(indices);
            StoreData(0, 3, vertices);
            StoreData(1, 2, textureCoords);
            StoreData(2, 3, normals);
            StoreData(3, 3, tangents);
            UnbindVAO();
            return new RawModel(VAOHandle, indices.Length);
        }
        public RawModel LoadToVao(ModelData data)
        {
            int VAOHandle = CreateVAO();
            BindToIBO(data.indices);
            StoreData(0, 3, data.vertices);
            StoreData(1, 2, data.textureCoords);
            StoreData(2, 3, data.normals);
            UnbindVAO();
            return new RawModel(VAOHandle, data.indices.Length);
        }
        public RawModel LoadToVao(float[] positions, int dimensions)
        {
            int VAOHandle = CreateVAO();
            StoreData(0, dimensions, positions);
            UnbindVAO();
            return new RawModel(VAOHandle, positions.Length / 2);
        }
        /// <summary>
        /// Aggiunge dei valori in un vertex buffer object e poi inserisce il vbo nel vao
        /// </summary>
        /// <param name="attributePosition">Il luogo del vao in cui inserire i dati</param>
        /// <param name="size">Quantità di valori per ogni dato</param>
        /// <param name="data">I dati da inserire</param>
        public void StoreData(int attributePosition, int size, float[] data)
        {
            //crea un vbo
            int vboHandle = GL.GenBuffer();
            vbos.Add(vboHandle);
            //rende il buffer corrente quello attivo
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboHandle);
            //inserisce i dati nel vbo
            GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StaticDraw);
            //specifica dove e come interpretare i dati inseriti nel vao
            GL.VertexAttribPointer(attributePosition, size, VertexAttribPointerType.Float, false, 0, 0);
            //scollega il vbo
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }
        /// <summary>
        /// Crea una texture e i suoi mipmaps da un file nella cartella Textures
        /// </summary>
        /// <param name="textureName">Nome del file contenente la texture</param>
        /// <returns>Handle della texture creata</returns>
        public int LoadTexture(string fileName)
        {
            Bitmap bitmap = new Bitmap($"Textures/{fileName}");

            int handle;
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

            GL.GenTextures(1, out handle);

            GL.BindTexture(TextureTarget.Texture2D, handle);

            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            bitmap.UnlockBits(data);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureLodBias, -0.4f);

            return handle;
        }
        public int LoadFontTexture(string fileName)
        {
            Bitmap bitmap = new Bitmap($"Textures/{fileName}");

            int handle;
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

            GL.GenTextures(1, out handle);

            GL.BindTexture(TextureTarget.Texture2D, handle);

            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            bitmap.UnlockBits(data);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureLodBias, 0.0f);

            return handle;
        }
        public int LoadCubeMap(string[] fileNames)
        {
            //////////////
            int handle = GL.GenTexture();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.TextureCubeMap, handle);
            for (int i = 0; i < fileNames.Length; i++)
            {
                TextureData data = DecodeTextureFile(fileNames[i]);
                GL.TexImage2D(TextureTarget.TextureCubeMapPositiveX + i, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0, OpenTK.Graphics.OpenGL4.PixelFormat.Bgra, PixelType.UnsignedByte, data.Data.Scan0);
            }
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.TextureCubeMap, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            textures.Add(handle);
            return handle;
        }

        public int CreateEmptyVbo(int floatCount)
        {
            int vbo = GL.GenBuffer();
            vbos.Add(vbo);
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, floatCount * sizeof(float), (IntPtr)0, BufferUsageHint.StreamDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            return vbo;
        }
        /// <summary>
        /// Aggiunge dei valori a un vbo già instanziato
        /// </summary>
        /// <param name="vao">Vao in cui inserire il vbo</param>
        /// <param name="vbo">Vbo in  cui inserire i dati</param>
        /// <param name="attribute">Posizione dell`attributo da inserire</param>
        /// <param name="dataSize">Numero di valori per ogni cella</param>
        /// <param name="instancedDataLength">Numero di valori per ogni gruppo</param>
        /// <param name="offset">Posizione del primo valore nella lista</param>
        public void AddInstancedAttribute(int vao, int vbo, int attribute, int dataSize, int instancedDataLength, int offset)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BindVertexArray(vao);
            GL.VertexAttribPointer(attribute, dataSize, VertexAttribPointerType.Float, false, instancedDataLength * 4, offset * 4);
            GL.VertexAttribDivisor(attribute, 1);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
        }
        public void UpdateVbo(int vbo, float[] data)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StreamDraw);
            GL.BufferSubData<float>(BufferTarget.ArrayBuffer, (IntPtr)0, data.Length * sizeof(float), data);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        /// <summary>
        /// Elimina tutte le istanze create attraverso l`istanza della classe Loader
        /// </summary>
        public void Delete()
        {
            foreach (int vao in vaos)
            {
                GL.DeleteVertexArray(vao);
            }
            foreach (int vbo in vbos)
            {
                GL.DeleteBuffer(vbo);
            }
            foreach (int texture in textures)
            {
                GL.DeleteTexture(texture);
            }
        }

        private TextureData DecodeTextureFile(string fileName)
        {
            Bitmap bmp = new Bitmap($"Textures/{fileName}");
            BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            TextureData data = new TextureData(bmpData.Width, bmpData.Height, bmpData);
            return data;
        }
        /// <summary>
        /// Crea un vertex array object
        /// </summary>
        /// <returns>Il puntatore al vao creato</returns>
        private int CreateVAO()
        {
            int vaoHandle = GL.GenVertexArray();
            vaos.Add(vaoHandle);
            GL.BindVertexArray(vaoHandle);
            return vaoHandle;
        }
        /// <summary>
        /// Scollega il vao in uso
        /// </summary>
        private void UnbindVAO()
        {
            GL.BindVertexArray(0);
        }
        /// <summary>
        /// Inserisce gli indici in un buffer 
        /// </summary>
        /// <param name="indices">Gli indici da inserire</param>
        private void BindToIBO(uint[] indices)
        {
            int vboHandle = GL.GenBuffer();
            vbos.Add(vboHandle);
            //specifico che questo buffer è per gli indici
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, vboHandle);

            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
        }
    }

    /// <summary>
    /// Classe utilizzata per caricare un modello da un file .obj
    /// </summary>
    public static class OBJLoader
    {
        private static string path = "Models/";
        
        /// <summary>
        /// Crea un mesh da un file .obj nella cartella Models
        /// </summary>
        /// <param name="fileName">Il nome del file</param>
        /// <returns>Un oggetto di tipo ModelData</returns>
        public static ModelData LoadOBJ(string fileName)
        {
            string line;
            List<Vertex> vertices = new List<Vertex>();
            List<Vector2> textures = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<uint> indices = new List<uint>();

            using (StreamReader sr = new StreamReader(path + fileName))
            {
                while (true)
                {
                    line = sr.ReadLine();

                    if (line.StartsWith("v "))
                    {
                        string[] currentLine = line.Split(' ');
                        Vector3 vertex = new Vector3(float.Parse(currentLine[1], CultureInfo.InvariantCulture.NumberFormat),
                                                     float.Parse(currentLine[2], CultureInfo.InvariantCulture.NumberFormat),
                                                     float.Parse(currentLine[3], CultureInfo.InvariantCulture.NumberFormat));
                        Vertex newVertex = new Vertex((uint)Math.Abs(vertices.Count), vertex);
                        vertices.Add(newVertex);

                    }
                    else if (line.StartsWith("vt "))
                    {
                        string[] currentLine = line.Split(' ');
                        Vector2 texture = new Vector2(float.Parse(currentLine[1], CultureInfo.InvariantCulture.NumberFormat),
                                                      float.Parse(currentLine[2], CultureInfo.InvariantCulture.NumberFormat));
                        textures.Add(texture);
                    }
                    else if (line.StartsWith("vn "))
                    {
                        string[] currentLine = line.Split(' ');
                        Vector3 normal = new Vector3(float.Parse(currentLine[1], CultureInfo.InvariantCulture.NumberFormat),
                                                     float.Parse(currentLine[2], CultureInfo.InvariantCulture.NumberFormat),
                                                     float.Parse(currentLine[3], CultureInfo.InvariantCulture.NumberFormat));
                        normals.Add(normal);
                    }
                    else if (line.StartsWith("f "))
                    {
                        break;
                    }
                }

                while (line != null && line.StartsWith("f "))
                {
                    string[] currentLine = line.Split(' ');
                    string[] vertex1 = currentLine[1].Split('/');
                    string[] vertex2 = currentLine[2].Split('/');
                    string[] vertex3 = currentLine[3].Split('/');
                    ProcessVertex(vertex1, vertices, indices);
                    ProcessVertex(vertex2, vertices, indices);
                    ProcessVertex(vertex3, vertices, indices);
                    line = sr.ReadLine();
                }
            }

            RemoveUnusedVertices(vertices);
            float[] verticesArray = new float[vertices.Count * 3];
            float[] texturesArray = new float[vertices.Count * 2];
            float[] normalsArray = new float[vertices.Count * 3];
            float furthest = ConvertDataToArrays(vertices, textures, normals, verticesArray, texturesArray, normalsArray);
            uint[] indicesArray = ConvertIndicesListToArray(indices);
            ModelData data = new ModelData(verticesArray, texturesArray, normalsArray, indicesArray, furthest);
            return data;
        }

        private static void ProcessVertex(string[] vertex, List<Vertex> vertices, List<uint> indices)
        {
            int index = int.Parse(vertex[0]) - 1;
            Vertex currentVertex = vertices[index];
            int textureIndex = int.Parse(vertex[1]) - 1;
            int normalIndex = int.Parse(vertex[2]) - 1;
            if (!(currentVertex == null))
            {
                currentVertex.textureIndex = textureIndex;
                currentVertex.normalIndex = normalIndex;
                indices.Add((uint)index);
            }
            else
            {
                DealWithAlreadyProcessedVertex(currentVertex, textureIndex, normalIndex, indices, vertices);
            }
        }
        private static uint[] ConvertIndicesListToArray(List<uint> indices)
        {
            uint[] indicesArray = new uint[indices.Count];
            for (int i = 0; i < indicesArray.Length; i++)
            {
                indicesArray[i] = indices[i];
            }
            return indicesArray;
        }
        private static float ConvertDataToArrays(List<Vertex> vertices, List<Vector2> textures, List<Vector3> normals, float[] verticesArray, float[] texturesArray, float[] normalsArray)
        {
            float furthestPoint = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                Vertex currentVertex = vertices[i];
                if (currentVertex.length > furthestPoint)
                {
                    furthestPoint = currentVertex.length;
                }
                Vector3 position = currentVertex.position;
                Vector2 textureCoord = textures[currentVertex.textureIndex];
                Vector3 normalVector = normals[currentVertex.normalIndex];
                verticesArray[i * 3] = position.X;
                verticesArray[i * 3 + 1] = position.Y;
                verticesArray[i * 3 + 2] = position.Z;
                texturesArray[i * 2] = textureCoord.X;
                texturesArray[i * 2 + 1] = 1 - textureCoord.Y;
                normalsArray[i * 3] = normalVector.X;
                normalsArray[i * 3 + 1] = normalVector.Y;
                normalsArray[i * 3 + 2] = normalVector.Z;
            }
            return furthestPoint;
        }
        private static void DealWithAlreadyProcessedVertex(Vertex previousVertex, int newTextureIndex, int newNormalIndex, List<uint> indices, List<Vertex> vertices)
        {
            if (previousVertex.HasSameTextureAndNormal(newTextureIndex, newNormalIndex))
            {
                indices.Add(previousVertex.index);
            }
            else
            {
                Vertex anotherVertex = previousVertex.duplicateVertex;
                if (anotherVertex != null)
                {
                    DealWithAlreadyProcessedVertex(anotherVertex, newTextureIndex, newNormalIndex,
                            indices, vertices);
                }
                else
                {
                    Vertex duplicateVertex = new Vertex((uint)Math.Abs(vertices.Count), previousVertex.position);
                    duplicateVertex.textureIndex = newTextureIndex;
                    duplicateVertex.normalIndex = newNormalIndex;
                    previousVertex.duplicateVertex = duplicateVertex;
                    vertices.Add(duplicateVertex);
                    indices.Add(duplicateVertex.index);
                }

            }
        }
        private static void RemoveUnusedVertices(List<Vertex> vertices)
        {
            foreach (Vertex vertex in vertices)
            {
                if (vertex == null)
                {
                    vertex.textureIndex = 0;
                    vertex.normalIndex = 0;
                }
            }
        }
    }

    public class ModelData
    {

        public float[] vertices { get; private set; }
        public float[] textureCoords { get; private set; }
        public float[] normals { get; private set; }
        public uint[] indices { get; private set; }
        public float furthestPoint { get; private set; }

        public ModelData(float[] vertices, float[] textureCoords, float[] normals, uint[] indices,
                float furthestPoint)
        {
            this.vertices = vertices;
            this.textureCoords = textureCoords;
            this.normals = normals;
            this.indices = indices;
            this.furthestPoint = furthestPoint;
        }
    }

    public class Vertex
    {
        private const int NO_INDEX = -1;

        public Vector3 position;
        public int textureIndex = NO_INDEX;
        public int normalIndex = NO_INDEX;
        public Vertex duplicateVertex = null;
        public uint index;
        public float length;

        public Vertex(uint index, Vector3 position)
        {
            this.index = index;
            this.position = position;
            this.length = position.Length;
        }

        public bool HasSameTextureAndNormal(int textureIndexOther, int normalIndexOther)
        {
            return textureIndexOther == textureIndex && normalIndexOther == normalIndex;
        }
    }
}
