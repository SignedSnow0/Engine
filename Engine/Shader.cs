using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.IO;

namespace Engine
{
    public abstract class Shader
    {
        private int handle;
        private int vertexHandle;
        private int fragmentHandle;

        /// <summary>
        /// Crea uno shader opengl composto da un vertex shader e un fragment shader
        /// </summary>
        /// <param name="vertexFileName">Nome del file .vert</param>
        /// <param name="fragmentFileName">Nome del file .frag</param>
        public Shader(string vertexFileName, string fragmentFileName)
        {
            vertexHandle = LoadShader(vertexFileName, ShaderType.VertexShader);
            fragmentHandle = LoadShader(fragmentFileName, ShaderType.FragmentShader);
            handle = GL.CreateProgram();

            GL.AttachShader(handle, vertexHandle);
            GL.AttachShader(handle, fragmentHandle);

            BindAttributes();

            GL.LinkProgram(handle);
            GL.ValidateProgram(handle);

            GetAllUniformLocations();
        }

        protected void LoadToUniform(int location, int value)
        {
            GL.Uniform1(location, value);
        }

        protected void LoadToUniform(int location, float value)
        {
            GL.Uniform1(location, value);
        }
        protected void LoadToUniform(int location, Vector2 value)
        {
            GL.Uniform2(location, value);
        }

        protected void LoadToUniform(int location, Vector3 value)
        {
            GL.Uniform3(location, value);
        }
        protected void LoadToUniform(int location, Vector4 value)
        {
            GL.Uniform4(location, value);
        }

        protected void LoadToUniform(int location, bool value)
        {
            if(value)
            {
                GL.Uniform1(location, 1.0f);
            }
            else
            {
                GL.Uniform1(location, 0.0f);
            }
        }

        protected void LoadToUniform(int location, Matrix4 value)
        {
            GL.UniformMatrix4(location, false, ref value);
        }

        protected int GetUniformLocation(string uniformName)
        {
            return GL.GetUniformLocation(handle, uniformName);
        }

        protected abstract void GetAllUniformLocations();

        protected abstract void BindAttributes();

        /// <summary>
        /// Dice al programma di renderizzare con lo shader compilato
        /// </summary>
        public void Start()
        {
            GL.UseProgram(handle);
        }

        /// <summary>
        /// Smette di renderizzare con questo shader
        /// </summary>
        public void Stop()
        {
            GL.UseProgram(0);
        }

        /// <summary>
        /// Elimina dalla memoria lo shader
        /// </summary>
        public void Delete()
        {
            Stop();
            GL.DetachShader(handle, vertexHandle);
            GL.DetachShader(handle, fragmentHandle);

            GL.DeleteShader(vertexHandle);
            GL.DeleteShader(fragmentHandle);
            GL.DeleteProgram(handle);
        }

        /// <summary>
        /// Associa al programma un attributo dello shader glsl con un nome
        /// </summary>
        /// <param name="attribute">Indice dell`attributo nel file glsl</param>
        /// <param name="name">Nome dell`attributo nel file glsl</param>
        protected void BindAttribute(int attribute, string name)
        {
            GL.BindAttribLocation(handle, attribute, name);
        }

        /// <summary>
        /// Carica uno shader dal file in cui è salvato
        /// </summary>
        /// <param name="fileName">Nome del file dello shader</param>
        /// <param name="type">Tipo di shader</param>
        /// <returns></returns>
        private int LoadShader(string fileName, ShaderType type)
        {
            string shaderCode;
            using(StreamReader sr = new StreamReader($"Shaders/{fileName}"))
            {
                shaderCode = sr.ReadToEnd();
            }

            int shaderHandle = GL.CreateShader(type);
            GL.ShaderSource(shaderHandle, shaderCode);
            GL.CompileShader(shaderHandle);

            string infoLogShader = GL.GetShaderInfoLog(shaderHandle);
            if (infoLogShader != string.Empty)
            {
                throw new Exception(infoLogShader);
            }

            return shaderHandle;
        }
    }

    /// <summary>
    /// Classe shader con due shader statici
    /// </summary>
    public class StaticShader : Shader
    {
        private const int MAX_LIGHTS = 4;

        private static string vertexFileName = "VertexShader.vert";
        private static string fragmentFileName = "FragmentShader.frag";

        private int handleTransformationMatrix;
        private int handleProjectionMatrix;
        private int handleViewMatrix;
        private int[] handleLightPositions;
        private int[] handleAttenuations;
        private int[] handleLightColors;
        private int handleShineDamper;
        private int handleReflectivity;
        private int handleUseFakeLighting;
        private int handleSkyColor;
        private int handleNumberOfRows;
        private int handleOffset;
        private int handlePlane;

        public StaticShader() : base(vertexFileName, fragmentFileName)
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
            handleUseFakeLighting = GetUniformLocation("useFakeLighting");
            handleSkyColor = GetUniformLocation("skyColor");
            handleNumberOfRows = GetUniformLocation("numberOfRows");
            handleOffset = GetUniformLocation("offset");
            handlePlane = GetUniformLocation("plane");

            handleLightPositions = new int[MAX_LIGHTS];
            handleLightColors = new int[MAX_LIGHTS];
            handleAttenuations = new int[MAX_LIGHTS];
            for (int i = 0; i < MAX_LIGHTS; i++)
            {
                handleLightPositions[i] = GetUniformLocation($"lightPosition[{i}]");
                handleLightColors[i] = GetUniformLocation($"lightColor[{i}]");
                handleAttenuations[i] = GetUniformLocation($"attenuation[{i}]");
            }
        }

        public void LoadClipPlane(Vector4 plane)
        {
            LoadToUniform(handlePlane, plane);
        }

        public void LoadOffset(Vector2 offset)
        {
            LoadToUniform(handleOffset, offset);
        }

        public void LoadNumberOfRows(int numberOfRows)
        {
            LoadToUniform(handleNumberOfRows, (float)numberOfRows);
        }

        public void LoadSkyColor(float r, float g, float b)
        {
            LoadToUniform(handleSkyColor, new Vector3(r, g, b));
        }

        public void LoadFakeLighting(bool useFakeLighting)
        {
            LoadToUniform(handleUseFakeLighting, useFakeLighting);
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
                if(i < lights.Count)
                {
                    LoadToUniform(handleLightPositions[i], lights[i].Position);
                    LoadToUniform(handleLightColors[i], lights[i].Color);
                    LoadToUniform(handleAttenuations[i], lights[i].Attenuation);
                }
                else
                {
                    LoadToUniform(handleLightPositions[i], new Vector3(0.0f, 0.0f, 0.0f));
                    LoadToUniform(handleLightColors[i], new Vector3(0.0f, 0.0f, 0.0f));
                    LoadToUniform(handleAttenuations[i], new Vector3(1.0f, 0.0f, 0.0f));
                }
            }

        }
    }
}
