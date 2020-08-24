using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace Engine
{
	public class WaterRenderer
	{
		private const string FILENAME = "WaterDUDV.png"; 
		private const string NORMAL_FILENAME = "WaterNormalMap.png";
		private const float WAVE_SPEED = 0.03f;
		private int handleDudv;
		private int handleNormalMap;
		private float moveFactor = 0.0f;
		private RawModel quad;
		private WaterShader shader;
		private WaterFrameBuffer fbos;

		public WaterRenderer(Loader loader, WaterShader shader, Matrix4 projectionMatrix, WaterFrameBuffer fbos)
		{
			this.shader = shader;
			this.fbos = fbos;
			handleDudv = loader.LoadTexture(FILENAME);
			handleNormalMap = loader.LoadTexture(NORMAL_FILENAME);
			shader.Start();
			shader.ConnecttextureUnits();
			shader.LoadProjectionMatrix(projectionMatrix);
			shader.Stop();
			SetUpVAO(loader);
		}

		public void Render(List<WaterTile> water, Camera camera, Light light)
		{
			PrepareRender(camera, light);
			foreach (WaterTile tile in water)
			{
				Matrix4 modelMatrix = Util.CreateTransformationMatrix(
						new Vector3(tile.X, tile.Height, tile.Z), 0, 0, 0,
						WaterTile.TILE_SIZE);
				shader.loadModelMatrix(modelMatrix);
				GL.DrawArrays(PrimitiveType.Triangles, 0, quad.VertexCount);
			}
			Unbind();
		}

		private void PrepareRender(Camera camera, Light light)
		{
			shader.Start();
			shader.LoadViewMatrix(camera);
			moveFactor += WAVE_SPEED * (CoreEngine.Delta / 1000);
			moveFactor %= 1;
			shader.LoadMoveFactor(moveFactor);
			shader.LoadLight(light);
			GL.BindVertexArray(quad.VaoHandle);
			GL.EnableVertexAttribArray(0);
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, fbos.ReflectionTexture);
			GL.ActiveTexture(TextureUnit.Texture1);
			GL.BindTexture(TextureTarget.Texture2D, fbos.RefractionTexture);
			GL.ActiveTexture(TextureUnit.Texture2);
			GL.BindTexture(TextureTarget.Texture2D, handleDudv);
			GL.ActiveTexture(TextureUnit.Texture3);
			GL.BindTexture(TextureTarget.Texture2D, handleNormalMap);
			GL.ActiveTexture(TextureUnit.Texture4);
			GL.BindTexture(TextureTarget.Texture2D, fbos.RefractionDepthTexture);

			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
		}

		private void Unbind()
		{
			GL.Disable(EnableCap.Blend);
			GL.DisableVertexAttribArray(0);
			GL.BindVertexArray(0);
			shader.Stop();
		}

		private void SetUpVAO(Loader loader)
		{
			// Just x and z vectex positions here, y is set to 0 in v.shader
			float[] vertices = { -1, -1, -1, 1, 1, -1, 1, -1, -1, 1, 1, 1 };
			quad = loader.LoadToVao(vertices, 2);
		}

	}

	public class WaterShader : Shader
	{

		private const string VERTEX_FILE = "WaterVertex.vert";
		private const string FRAGMENT_FILE = "WaterFragment.frag";

		private int handleModelMatrix;
		private int handleViewMatrix;
		private int handleProjectionMatrix;
		private int handleReflectionTexture;
		private int handleRefractionTexture;
		private int handleDudv;
		private int handleMoveFactor;
		private int handleCameraPosition;
		private int handleNormalMap;
		private int handleLightColor;
		private int handleLightPosition;
		private int handleDepthMap;


		public WaterShader() : base(VERTEX_FILE, FRAGMENT_FILE)
		{
			
		}

		
		protected override void BindAttributes()
		{
			BindAttribute(0, "position");
		}


		protected override void GetAllUniformLocations()
		{
			handleProjectionMatrix = GetUniformLocation("projectionMatrix");
			handleViewMatrix = GetUniformLocation("viewMatrix");
			handleModelMatrix = GetUniformLocation("modelMatrix");
			handleReflectionTexture = GetUniformLocation("reflectionTexture");
			handleRefractionTexture = GetUniformLocation("refractionTexture");
			handleDudv = GetUniformLocation("dudv");
			handleMoveFactor = GetUniformLocation("moveFactor");
			handleCameraPosition = GetUniformLocation("cameraPosition");
			handleNormalMap = GetUniformLocation("normalMap");
			handleLightColor = GetUniformLocation("lightColor");
			handleLightPosition = GetUniformLocation("lightPosition");
			handleDepthMap = GetUniformLocation("depthMap");
		}

		public void ConnecttextureUnits()
        {
			LoadToUniform(handleReflectionTexture, 0);
			LoadToUniform(handleRefractionTexture, 1);
			LoadToUniform(handleDudv, 2);
			LoadToUniform(handleNormalMap, 3);
			LoadToUniform(handleDepthMap, 4);
		}

		public void LoadLight(Light light)
		{
			LoadToUniform(handleLightPosition, light.Position);
			LoadToUniform(handleLightColor, light.Color);
		}

		public void LoadMoveFactor(float factor)
        {
			LoadToUniform(handleMoveFactor, factor);
        }

		public void LoadProjectionMatrix(Matrix4 projection)
		{
			LoadToUniform(handleProjectionMatrix, projection);
		}

		public void LoadViewMatrix(Camera camera)
		{
			Matrix4 viewMatrix = Util.CreateViewMatrix(camera);
			LoadToUniform(handleViewMatrix, viewMatrix);
			LoadToUniform(handleCameraPosition, camera.Position);
		}

		public void loadModelMatrix(Matrix4 modelMatrix)
		{
			LoadToUniform(handleModelMatrix, modelMatrix);
		}

	}

	public class WaterTile
	{
	
		public const float TILE_SIZE = 150;
	
		public float Height { get; private set; }
		public float X { get; private set; }
		public float Z { get; private set; }

	public WaterTile(float centerX, float centerZ, float height)
		{
			X = centerX;
			Z = centerZ;
			Height = height;
		}
	
	}
    public class WaterFrameBuffer
    {

		public int ReflectionTexture { get; private set; }
		public int RefractionTexture { get; private set; }
		public int RefractionDepthTexture { get; private set; }

		private int ReflectionDepthBuffer;
		private int RefractionFrameBuffer;
		private int ReflectionFrameBuffer;

		protected const int REFLECTION_WIDTH = 1280;
        private const int REFLECTION_HEIGHT = 720;

        protected const int REFRACTION_WIDTH = 1280;
        private const int REFRACTION_HEIGHT = 720;


		/// <summary>
		/// Crea un oggetto contenente l`fbo necessario per calcoli dell`acqua
		/// </summary>
		/// <param name="width">larghezza della finestra</param>
		/// <param name="height">altezza della finestra</param>
		public WaterFrameBuffer(int width, int height)
        {
            InitialiseReflectionFrameBuffer(width, height);
            InitialiseRefractionFrameBuffer(width, height);
        }
        public void BindReflectionFrameBuffer()
        {
            BindFrameBuffer(ReflectionFrameBuffer, REFLECTION_WIDTH, REFLECTION_HEIGHT);
        }
        public void BindRefractionFrameBuffer()
        {
            BindFrameBuffer(RefractionFrameBuffer, REFRACTION_WIDTH, REFRACTION_HEIGHT);
        }
        public void UnbindCurrentFrameBuffer(int width, int height)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, width, height);
        }
        public void Delete()
        {
            GL.DeleteFramebuffer(ReflectionFrameBuffer);
            GL.DeleteTexture(ReflectionTexture);
            GL.DeleteRenderbuffer(ReflectionDepthBuffer);
            GL.DeleteFramebuffer(RefractionFrameBuffer);
            GL.DeleteTexture(RefractionTexture);
            GL.DeleteTexture(RefractionDepthTexture);
        }

        private void InitialiseReflectionFrameBuffer(int width, int height)
        {
            ReflectionFrameBuffer = CreateFrameBuffer();
            ReflectionTexture = CreateTextureAttachment(REFLECTION_WIDTH, REFLECTION_HEIGHT);
            ReflectionDepthBuffer = CreateDepthBufferAttachment(REFLECTION_WIDTH, REFLECTION_HEIGHT);
            UnbindCurrentFrameBuffer(width, height);
        }
        private void InitialiseRefractionFrameBuffer(int width, int height)
        {
            RefractionFrameBuffer = CreateFrameBuffer();
            RefractionTexture = CreateTextureAttachment(REFRACTION_WIDTH, REFRACTION_HEIGHT);
            RefractionDepthTexture = CreateDepthTextureAttachment(REFRACTION_WIDTH, REFRACTION_HEIGHT);
            UnbindCurrentFrameBuffer(width, height);
        }
		/// <summary>
		/// Specifica l`fbo con cui renderizzare la scena
		/// </summary>
		/// <param name="frameBuffer">L`fbo scelto</param>
		/// <param name="width">Larghezza della finestra</param>
		/// <param name="height">Altezza della finestra</param>
        private void BindFrameBuffer(int frameBuffer, int width, int height)
        {
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);
            GL.Viewport(0, 0, width, height);
        }
		/// <summary>
		/// Crea un Fbo
		/// </summary>
		/// <returns>Puntatore al fbo creato</returns>
        private int CreateFrameBuffer()
        {
            int frameBuffer = GL.GenFramebuffer();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);
			//Indica quale colorattachment da usare per il renderind
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);

            return frameBuffer;
        }
		/// <summary>
		/// Aggiunge al fbo un tipo di buffer con una texture
		/// </summary>
		/// <param name="width">Larghezza della texture in pixel</param>
		/// <param name="height">Altezza della texture in pixel</param>
		/// <returns>Id della texture</returns>
		private int CreateTextureAttachment(int width, int height)
        {
            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, width, height, 0, PixelFormat.Rgb, PixelType.UnsignedInt, (IntPtr)null);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			//aggiunge la texture al fbo attivo
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, texture, 0);
            return texture;
        }
		/// <summary>
		/// Aggiunge al fbo un tipo di buffer con la profondità di una texture
		/// </summary>
		/// <param name="width">Larghezza della texture in pixel</param>
		/// <param name="height">Altezza della texture in pixel</param>
		/// <returns>Id della profondità della texture</returns>
		private int CreateDepthTextureAttachment(int width, int height)
        {
            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent, width, height, 0, PixelFormat.DepthComponent, PixelType.Float, (IntPtr)null);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			//aggiunge la texture al fbo attivo
			GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, texture, 0);
            return texture;
        }
		/// <summary>
		/// Aggiunge un depth buffer che non si ottiene da una texture
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <returns></returns>
        private int CreateDepthBufferAttachment(int width, int height)
        {
            int depthBuffer = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.DepthComponent, width, height);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, depthBuffer);
            return depthBuffer;
        }
    }
}