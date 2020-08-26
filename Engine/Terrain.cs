using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Engine
{
    public class Terrain
    {
        private const float SIZE = 400;
        private const float MAX_PIXEL_COLOR = 255;
        private const float MAX_HEIGHT = 40;
        private float[,] heights;
        private int seed = new Random().Next(1000000000);
        private HeightsGenerator generator;
        private int vertexCount = 128;

        public float X { get; private set; }
        public float Z { get; private set; }
        public RawModel Model { get; private set; }
        public TerrainTexturePack TexturePack { get; private set; }
        public TerrainTexture BlendMap { get; private set; }

        public Terrain(int gridX, int gridZ,Loader loader, TerrainTexturePack texturePack, TerrainTexture blendMap)
        {
            TexturePack = texturePack;
            BlendMap = blendMap;
            X = gridX * SIZE;
            Z = gridZ * SIZE;
            generator = new HeightsGenerator(gridX, gridZ, vertexCount, seed);
            Model = GenerateTerrain(loader);
        }
        public Terrain(float gridX, float gridZ, Loader loader, TerrainTexturePack texturePack, TerrainTexture blendMap, string heightMap)
        {
            X = gridX * SIZE;
            Z = gridZ * SIZE;
            TexturePack = texturePack;
            BlendMap = blendMap;
            Model = GenerateTerrain(loader, heightMap);
        }
        private RawModel GenerateTerrain(Loader loader)
        {
            HeightsGenerator generator = new HeightsGenerator();
            int count = vertexCount * vertexCount;
            heights = new float[vertexCount, vertexCount];
            float[] vertices = new float[count * 3];
            float[] normals = new float[count * 3];
            float[] textureCoords = new float[count * 2];
            uint[] indices = new uint[6 * (vertexCount - 1) * (vertexCount * 1)];
            int vertexPointer = 0;
            for (int i = 0; i < vertexCount; i++)
            {
                for (int j = 0; j < vertexCount; j++)
                {
                    vertices[vertexPointer * 3] = j / ((float)vertexCount - 1) * SIZE;
                    float height = GetHeight(j, i);
                    vertices[vertexPointer * 3 + 1] = height;
                    heights[j, i] = height;
                    vertices[vertexPointer * 3 + 2] = i / ((float)vertexCount - 1) * SIZE;
                    Vector3 normal = CalculateNormal(j, i);
                    normals[vertexPointer * 3] = normal.X;
                    normals[vertexPointer * 3 + 1] = normal.Y;
                    normals[vertexPointer * 3 + 2] = normal.Z;
                    textureCoords[vertexPointer * 2] = j / ((float)vertexCount - 1);
                    textureCoords[vertexPointer * 2 + 1] = i / ((float)vertexCount - 1);
                    vertexPointer++;
                }
            }
            int pointer = 0;
            for (int gz = 0; gz < vertexCount - 1; gz++)
            {
                for (int gx = 0; gx < vertexCount - 1; gx++)
                {
                    uint topLeft = (uint)Math.Abs((gz * vertexCount) + gx);
                    uint topRight = topLeft + 1;
                    uint bottomLeft = (uint)Math.Abs(((gz + 1) * vertexCount) + gx);
                    uint bottomRight = bottomLeft + 1;
                    indices[pointer++] = topLeft;
                    indices[pointer++] = bottomLeft;
                    indices[pointer++] = topRight;
                    indices[pointer++] = topRight;
                    indices[pointer++] = bottomLeft;
                    indices[pointer++] = bottomRight;
                }
            }
            return loader.LoadToVao(vertices, textureCoords, normals, indices);
        }

        private RawModel GenerateTerrain(Loader loader, string heightMap)
        {
            Bitmap bitmap = new Bitmap($"Textures/{heightMap}");
            int vertexCount = bitmap.Height;
            heights = new float[vertexCount, vertexCount];
            int count = vertexCount * vertexCount;
            float[] vertices = new float[count * 3];
            float[] normals = new float[count * 3];
            float[] textureCoords = new float[count * 2];
            uint[] indices = new uint[6 * (vertexCount - 1) * (vertexCount - 1)];
            int vertexPointer = 0;
            for (int i = 0; i < vertexCount; i++)
            {
                for (int j = 0; j < vertexCount; j++)
                {
                    vertices[vertexPointer * 3] = j / ((float)vertexCount - 1) * SIZE;
                    float height = GetHeight(j, i, bitmap);
                    heights[j, i] = height;
                    vertices[vertexPointer * 3 + 1] = height;
                    vertices[vertexPointer * 3 + 2] = i / ((float)vertexCount - 1) * SIZE;
                    Vector3 normal = CalculateNormal(j, i, bitmap);
                    normals[vertexPointer * 3] = normal.X;
                    normals[vertexPointer * 3 + 1] = normal.Y;
                    normals[vertexPointer * 3 + 2] = normal.Z;
                    textureCoords[vertexPointer * 2] = j / ((float)vertexCount - 1);
                    textureCoords[vertexPointer * 2 + 1] = i / ((float)vertexCount - 1);
                    vertexPointer++;
                }
            }
            int pointer = 0;
            for (int gz = 0; gz < vertexCount - 1; gz++)
            {
                for (int gx = 0; gx < vertexCount - 1; gx++)
                {
                    uint topLeft = (uint)Math.Abs((gz * vertexCount) + gx);
                    uint topRight = topLeft + 1;
                    uint bottomLeft = (uint)Math.Abs(((gz + 1) * vertexCount) + gx);
                    uint bottomRight = bottomLeft + 1;
                    indices[pointer++] = topLeft;
                    indices[pointer++] = bottomLeft;
                    indices[pointer++] = topRight;
                    indices[pointer++] = topRight;
                    indices[pointer++] = bottomLeft;
                    indices[pointer++] = bottomRight;
                }
            }
            return loader.LoadToVao(vertices, textureCoords, normals, indices);
        }

        public float GetHeightOfTerrain(float worldX, float worldZ)
        {
            float terrainX = worldX - X;
            float terrainZ = worldZ - Z;
            //Trova il quadrato dove si trova l'entità
            float gridSquareSize = SIZE / ((float)heights.GetLength(0) - 1);
            int gridX = (int)Math.Floor(terrainX / gridSquareSize);
            int gridZ = (int)Math.Floor(terrainZ / gridSquareSize);
            //trova il triangolo che fa parte del quadrato
            if (gridX >= (heights.GetLength(0) - 1) || gridZ >= (heights.GetLength(1) - 1) || gridX < 0 || gridZ < 0)
            {
                return 0;
            }
            float xCoord = (terrainX % gridSquareSize) / gridSquareSize;
            float zCoord = (terrainZ % gridSquareSize) / gridSquareSize;
            float answer;
            //calcola se collide o no
            if (xCoord <= (1 - zCoord))
            {
                answer = Util.BarryCentric(new Vector3(0, heights[gridX, gridZ], 0),
                                           new Vector3(1, heights[gridX + 1, gridZ], 0),
                                           new Vector3(0, heights[gridX, gridZ + 1], 1),
                                           new Vector2(xCoord, zCoord));
            }
            else
            {
                answer = Util.BarryCentric(new Vector3(1, heights[gridX + 1, gridZ], 0),
                                           new Vector3(1, heights[gridX + 1, gridZ + 1], 1),
                                           new Vector3(0, heights[gridX, gridZ + 1], 1),
                                           new Vector2(xCoord, zCoord));
            }
            return answer;
        }
        private Vector3 CalculateNormal(int x, int z, Bitmap image)
        {
            float heightL = GetHeight(x - 1, z, image);
            float heightR = GetHeight(x + 1, z, image);
            float heightD = GetHeight(x, z - 1, image);
            float heightU = GetHeight(x, z + 1, image);
            Vector3 normal = new Vector3(heightL - heightR, 2.0f, heightD - heightU);
            normal.Normalize();
            return normal;
        }
        private Vector3 CalculateNormal(int x, int z)
        {
            float heightL = GetHeight(x - 1, z);
            float heightR = GetHeight(x + 1, z);
            float heightD = GetHeight(x, z - 1);
            float heightU = GetHeight(x, z + 1);
            Vector3 normal = new Vector3(heightL - heightR, 2.0f, heightD - heightU);
            normal.Normalize();
            return normal;
        }
        private float GetHeight(int x, int z, Bitmap heightMap)
        {
            if (x < 0 || x >= heightMap.Height || z < 0 || z >= heightMap.Height)
            {
                return 0;
            }
            float height = heightMap.GetPixel(x, z).R;
            height += MAX_PIXEL_COLOR / 2f;
            height /= MAX_PIXEL_COLOR / 2f;
            height *= MAX_HEIGHT;
            return height;
        }
        private float GetHeight(int x, int z)
        {
            return generator.GenerateHeight(x, z);
        }
    }

    public class TerrainRenderer
    {
        private TerrainShader shader;

        public TerrainRenderer(TerrainShader shader, Matrix4 projectionMatrix)
        {
            this.shader = shader;
            shader.Start();
            shader.LoadProjectionMatrix(projectionMatrix);
            shader.ConnectTextureUnits();
            shader.Stop();
        }

        public void Render(List<Terrain> terrains)
        {
            foreach (Terrain terrain in terrains)
            {
                PrepareTerrain(terrain);
                LoadModelMatrix(terrain);

                GL.DrawElements(BeginMode.Triangles, terrain.Model.VertexCount, DrawElementsType.UnsignedInt, 0);

                UnbindTerrain();
            }
        }

        private void BindTextures(Terrain terrain)
        {
            TerrainTexturePack texturePack = terrain.TexturePack;
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texturePack.backgroundTexture.handleTexture); 
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, texturePack.rTexture.handleTexture);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, texturePack.gTexture.handleTexture);
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, texturePack.bTexture.handleTexture);
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, terrain.BlendMap.handleTexture);
        }

        private void PrepareTerrain(Terrain terrain)
        {
            RawModel model = terrain.Model;

            //rendo il vao attivo
            GL.BindVertexArray(model.VaoHandle);

            //Indico che voglio usare le informazioni nella riga 0
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
            BindTextures(terrain);
            shader.LoadShineVariables(1.0f, 0.0f);

        }

        private void UnbindTerrain()
        {
            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
            GL.DisableVertexAttribArray(2);
            //scollego il vao
            GL.BindVertexArray(0);
        }

        private void LoadModelMatrix(Terrain terrain)
        {
            Matrix4 transformationMatrix = Util.CreateTransformationMatrix(new Vector3(terrain.X, 0.0f, terrain.Z), 0.0f, 0.0f, 0.0f, 1.0f);
            shader.LoadTransformationMatrix(transformationMatrix);
        }
    }

    public class TerrainShader : Shader
    {
        private const int MAX_LIGHTS = 4;

        private static string vertexFileName = "TerrainVertexShader.vert";
        private static string fragmentFileName = "TerrainFragmentShader.frag";

        private int handleTransformationMatrix;
        private int handleProjectionMatrix;
        private int handleViewMatrix;
        private int[] handleLightPosition;
        private int[] handleLightColor;
        private int[] handleAttenuation;
        private int handleShineDamper;
        private int handleReflectivity;
        private int handleSkyColor;
        private int handleBackgroundTexture;
        private int handleRTexture;
        private int handleGTexture;
        private int handleBTexture;
        private int handleBlendMap;
        private int handlePlane;

        public TerrainShader() : base(vertexFileName, fragmentFileName)
        {

        }

        /// <summary>
        /// Associa gli attributi del programma con quelli del file glsl
        /// </summary>
        protected override void BindAttributes()
        {
            BindAttribute(0, "position");
            BindAttribute(1, "textureCoords");
            BindAttribute(2, "normal");
        }

        /// <summary>
        /// Trova il luogo nello shader della variabile con quel nome
        /// </summary>
        protected override void GetAllUniformLocations()
        {
            handleTransformationMatrix = GetUniformLocation("transformationMatrix");
            handleProjectionMatrix = GetUniformLocation("projectionMatrix");
            handleViewMatrix = GetUniformLocation("viewMatrix");
            handleShineDamper = GetUniformLocation("shineDamper");
            handleReflectivity = GetUniformLocation("reflectivity");
            handleSkyColor = GetUniformLocation("skyColor");
            handleBackgroundTexture = GetUniformLocation("backgroundTexture");
            handleRTexture = GetUniformLocation("rTexture");
            handleGTexture = GetUniformLocation("gTexture");
            handleBTexture = GetUniformLocation("bTexture");
            handleBlendMap = GetUniformLocation("blendMap");
            handlePlane = GetUniformLocation("plane");

            handleLightPosition = new int[MAX_LIGHTS];
            handleLightColor = new int[MAX_LIGHTS];
            handleAttenuation = new int[MAX_LIGHTS];
            for (int i = 0; i < MAX_LIGHTS; i++)
            {
                handleLightPosition[i] = GetUniformLocation($"lightPosition[{i}]");
                handleLightColor[i] = GetUniformLocation($"lightColor[{i}]");
                handleAttenuation[i] = GetUniformLocation($"attenuation[{i}]");
            }
        }

        public void ConnectTextureUnits()
        {
            LoadToUniform(handleBackgroundTexture, 0);
            LoadToUniform(handleRTexture, 1);
            LoadToUniform(handleGTexture, 2);
            LoadToUniform(handleBTexture, 3);
            LoadToUniform(handleBlendMap, 4);
        }
        public void LoadClipPlane(Vector4 plane)
        {
            LoadToUniform(handlePlane, plane);
        }

        public void LoadSkyColor(float r, float g, float b)
        {
            LoadToUniform(handleSkyColor, new Vector3(r, g, b));
        }

        public void LoadShineVariables(float damper, float reflectivity)
        {
            LoadToUniform(handleShineDamper, damper);
            LoadToUniform(handleReflectivity, reflectivity);
        }

        public void LoadTransformationMatrix(Matrix4 value)
        {
            LoadToUniform(handleTransformationMatrix, value);
        }

        public void LoadProjectionMatrix(Matrix4 value)
        {
            LoadToUniform(handleProjectionMatrix, value);
        }

        public void LoadViewMatrix(Camera camera)
        {
            Matrix4 viewMatrix = Util.CreateViewMatrix(camera);
            LoadToUniform(handleViewMatrix, viewMatrix);
        }

        public void LoadLights(List<Light> lights)
        {
            for (int i = 0; i < MAX_LIGHTS; i++)
            {
                if (i < lights.Count)
                {
                    LoadToUniform(handleLightPosition[i], lights[i].Position);
                    LoadToUniform(handleLightColor[i], lights[i].Color);
                    LoadToUniform(handleAttenuation[i], lights[i].Attenuation);
                }
                else
                {
                    LoadToUniform(handleLightPosition[i], new Vector3(0.0f, 0.0f, 0.0f));
                    LoadToUniform(handleLightColor[i], new Vector3(0.0f, 0.0f, 0.0f));
                    LoadToUniform(handleAttenuation[i], new Vector3(1.0f, 0.0f, 0.0f));
                }
            }

        }
    }

    public class TerrainTexture
    {
        public int handleTexture {get; private set; }

        public TerrainTexture(int handleTexture)
        {
            this.handleTexture = handleTexture;
        }
    }

    public class TerrainTexturePack
    {
        public TerrainTexture backgroundTexture { get; private set; }
        public TerrainTexture rTexture { get; private set; }
        public TerrainTexture gTexture { get; private set; }
        public TerrainTexture bTexture { get; private set; }

        public TerrainTexturePack(TerrainTexture backgroundTexture, TerrainTexture rTexture, TerrainTexture gTexture, TerrainTexture bTexture)
        {
            this.backgroundTexture = backgroundTexture;
            this.rTexture = rTexture;
            this.gTexture = gTexture;
            this.bTexture = bTexture;
        }
    }

    public class HeightsGenerator
    {

        private const float AMPLITUDE = 30f;
        private const int OCTAVES = 3;
        private const float ROUGHNESS = 0.3f;

        private Random random;
        private int seed;
        private int xOffset = 0;
        private int zOffset = 0;

        public HeightsGenerator()
        {
            seed = new Random().Next(1000000000);
        }

        //only works with POSITIVE gridX and gridZ values!
        public HeightsGenerator(int gridX, int gridZ, int vertexCount, int seed)
        {
            this.seed = seed;
            xOffset = gridX * (vertexCount - 1);
            zOffset = gridZ * (vertexCount - 1);
        }

        public float GenerateHeight(int x, int z)
        {
            float total = 0;
            float d = (float)Math.Pow(2, OCTAVES - 1);
            for (int i = 0; i < OCTAVES; i++)
            {
                float freq = (float)(Math.Pow(2, i) / d);
                float amp = (float)Math.Pow(ROUGHNESS, i) * AMPLITUDE;
                total += GetInterpolatedNoise((x + xOffset) * freq, (z + zOffset) * freq) * amp;
            }
            return total;
        }

        private float GetInterpolatedNoise(float x, float z)
        {
            int intX = (int)x;
            int intZ = (int)z;
            float fracX = x - intX;
            float fracZ = z - intZ;

            float v1 = GetSmoothNoise(intX, intZ);
            float v2 = GetSmoothNoise(intX + 1, intZ);
            float v3 = GetSmoothNoise(intX, intZ + 1);
            float v4 = GetSmoothNoise(intX + 1, intZ + 1);
            float i1 = Interpolate(v1, v2, fracX);
            float i2 = Interpolate(v3, v4, fracX);
            return Interpolate(i1, i2, fracZ);
        }

        private float Interpolate(float a, float b, float blend)
        {
            double theta = blend * Math.PI;
            float f = (float)(1.0f - Math.Cos(theta)) * 0.5f;
            return a * (1.0f - f) + b * f;
        }

        private float GetSmoothNoise(int x, int z)
        {
            float corners = (GetNoise(x - 1, z - 1) + GetNoise(x + 1, z - 1) + GetNoise(x - 1, z + 1) + GetNoise(x + 1, z + 1)) / 16.0f;
            float sides = (GetNoise(x - 1, z) + GetNoise(x + 1, z) + GetNoise(x, z - 1) + GetNoise(x, z + 1)) / 8.0f;
            float center = GetNoise(x, z) / 4.0f;
            return corners + sides + center;
        }

        private float GetNoise(int x, int z)
        {
            random = new Random(x * 49364632 + z * 325265616 + seed);
            return (float)random.NextDouble() * 2f - 1f;
        }

    }
}
