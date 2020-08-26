using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace Engine
{
	public class ShadowBox
	{

		private const float OFFSET = 10;
		private Vector4 UP = new Vector4(0, 1, 0, 0);
		private Vector4 FORWARD = new Vector4(0, 0, -1, 0);
		private const float SHADOW_DISTANCE = 100;

		private float minX, maxX;
		private float minY, maxY;
		private float minZ, maxZ;
		private Matrix4 lightViewMatrix;
		private Camera cam;

		private float farHeight, farWidth, nearHeight, nearWidth;

		private int width;
		private int height;

		/// <summary>
		/// Crea una shadowBox e ne inizializza i valori riguardo la camera
		/// </summary>
		/// <param name="lightViewMatrix">La vievMatrix della luce</param>
		/// <param name="camera">La camera del gioco</param>		
		public ShadowBox(Matrix4 lightViewMatrix, Camera camera, int width, int height)
		{
			this.lightViewMatrix = lightViewMatrix;
			cam = camera;
			this.width = width;
			this.height = height;
			CalculateWidthsAndHeights();
		}

		/// <summary>
		/// Aggiorna i margini della shadow box in base alla direzione della luce e il view frustum della camera
		/// </summary>
		public void Update()
		{
			Matrix4 rotation = CalculateCameraRotationMatrix();
			Vector3 forwardVector = new Vector3(Vector4.Transform(FORWARD ,rotation));

			Vector3 toFar = new Vector3(forwardVector);
			toFar*= SHADOW_DISTANCE;
			Vector3 toNear = forwardVector;
			toNear *= MasterRenderer.NEAR_PLANE;
			Vector3 centerNear = toNear + cam.Position;
			Vector3 centerFar = toFar + cam.Position;

			Vector4[] points = CalculateFrustumVertices(rotation, forwardVector, centerNear, centerFar);

			bool first = true;
			foreach(Vector4 point in points)
			{
				if (first)
				{
					minX = point.X;
					maxX = point.X;
					minY = point.Y;
					maxY = point.Y;
					minZ = point.Z;
					maxZ = point.Z;
					first = false;
					continue;
				}
				if (point.X > maxX)
				{
					maxX = point.X;
				}
				else if (point.X < minX)
				{
					minX = point.X;
				}
				if (point.Y > maxY)
				{
					maxY = point.Y;
				}
				else if (point.Y < minY)
				{
					minY = point.Y;
				}
				if (point.Z > maxZ)
				{
					maxZ = point.Z;
				}
				else if (point.Z < minZ)
				{
					minZ = point.Z;
				}
			}
			maxZ += OFFSET;
		}

		/// <summary>
		/// Calcola il centro del "view cuboid" in light space e po lo converte in world space
		/// </summary>
		/// <returns>il cento dek view cuboid in world space</returns>
		public Vector3 GetCenter()
		{
			float x = (minX + maxX) / 2f;
			float y = (minY + maxY) / 2f;
			float z = (minZ + maxZ) / 2f;
			Vector4 cen = new Vector4(x, y, z, 1);
			Matrix4 invertedLight = new Matrix4();
			if (lightViewMatrix.Determinant != 0)
			{
				Matrix4.Invert(ref lightViewMatrix, out invertedLight);
			}
			return new Vector3(Vector4.Transform(cen, invertedLight));
		}

		///<summary>
		///Returns the width of the "view cuboid" (orthographic projection area).
		///</summary>
		public float GetWidth()
		{
			return maxX - minX;
		}

		///<summary>
		///Returns the height of the "view cuboid" (orthographic projection area).
		///</summary>
		public float GetHeight()
		{
			return maxY - minY;
		}

		///<summary>
		/// Returns the length of the "view cuboid" (orthographic projection area).
		///</summary>
		public float GetLength()
		{
			return maxZ - minZ;
		}

		/// <summary>
		/// Calcola la posizione del vertice di ogni anglolo del view frustum in light space
		/// </summary>
		/// <param name="rotation">Rotazione della camera</param>
		/// <param name="forwardVector">La direzione verso la quale la camera sta puntando</param>
		/// <param name="centerNear">Il punto centrale del near plane</param>
		/// <param name="centerFar">Il punto centrale del far plane</param>
		/// <returns>La posizione dei vertici del frustum in light space</returns>
		private Vector4[] CalculateFrustumVertices(Matrix4 rotation, Vector3 forwardVector,
				Vector3 centerNear, Vector3 centerFar)
		{
			Vector3 upVector = new Vector3(Vector4.Transform(UP, rotation));
			Vector3 rightVector = Vector3.Cross(forwardVector, upVector);
			Vector3 downVector = new Vector3(-upVector.X,    -upVector.Y,    -upVector.Z);
			Vector3 leftVector = new Vector3(-rightVector.X, -rightVector.Y, -rightVector.Z);
			Vector3 farTop     = centerFar  + new Vector3(upVector.X   * farHeight,  upVector.Y   * farHeight,  upVector.Z   * farHeight);
			Vector3 farBottom  = centerFar  + new Vector3(downVector.X * farHeight,  downVector.Y * farHeight,  downVector.Z * farHeight);
			Vector3 nearTop    = centerNear + new Vector3(upVector.X   * nearHeight, upVector.Y   * nearHeight, upVector.Z   * nearHeight);
			Vector3 nearBottom = centerNear + new Vector3(downVector.X * nearHeight, downVector.Y * nearHeight, downVector.Z * nearHeight);
			Vector4[] points = new Vector4[8];
			points[0] = CalculateLightSpaceFrustumCorner(farTop, rightVector, farWidth);
			points[1] = CalculateLightSpaceFrustumCorner(farTop, leftVector, farWidth);
			points[2] = CalculateLightSpaceFrustumCorner(farBottom, rightVector, farWidth);
			points[3] = CalculateLightSpaceFrustumCorner(farBottom, leftVector, farWidth);
			points[4] = CalculateLightSpaceFrustumCorner(nearTop, rightVector, nearWidth);
			points[5] = CalculateLightSpaceFrustumCorner(nearTop, leftVector, nearWidth);
			points[6] = CalculateLightSpaceFrustumCorner(nearBottom, rightVector, nearWidth);
			points[7] = CalculateLightSpaceFrustumCorner(nearBottom, leftVector, nearWidth);
			return points;
		}

		/// <summary>
		/// Calcola uno degli angoli del view frustum in world space e lo converte in light space
		/// </summary>
		/// <param name="startPoint">Il punto inziale del view frustum</param>
		/// <param name="direction">La direzione dell`angolo dal punto iniziale</param>
		/// <param name="width">La distanza tra il punto iniziale e l`angolo</param>
		/// <returns>L`angolo di partenza in light space</returns>
		private Vector4 CalculateLightSpaceFrustumCorner(Vector3 startPoint, Vector3 direction, float width)
		{
			Vector3 point = startPoint + new Vector3(direction.X * width, direction.Y * width, direction.Z * width);
			Vector4 point4f = new Vector4(point, 1.0f);
			point4f = Vector4.Transform(point4f, lightViewMatrix);
			return point4f;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>La rotazione della camera sotto forma di matrice</returns>
		private Matrix4 CalculateCameraRotationMatrix()
		{
			Matrix4 rotation = Matrix4.Identity;
			rotation *= Matrix4.CreateRotationY(MathHelper.DegreesToRadians(-cam.Yaw));
			rotation *= Matrix4.CreateRotationX(MathHelper.DegreesToRadians(-cam.Pitch));
			return rotation;
		}

		/// <summary>
		/// Calcola la larghezza a l`altezza del near e far plane del view frustum della camera
		/// </summary>
		private void CalculateWidthsAndHeights()
		{
			farWidth = (float)(SHADOW_DISTANCE * Math.Tan(MathHelper.DegreesToRadians(MasterRenderer.FOV)));
			nearWidth = (float)(MasterRenderer.NEAR_PLANE * Math.Tan(MathHelper.DegreesToRadians(MasterRenderer.FOV)));
			farHeight = farWidth / GetAspectRatio();
			nearHeight = nearWidth / GetAspectRatio();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>AspectRatio del display</returns>
		private float GetAspectRatio()
		{
			return width / (float)height;
		}
	}

	public class ShadowFrameBuffer
	{

		private int WIDTH;
		private int HEIGHT;
		private int fbo;
		public int ShadowMap { get; private set; }
	
		/// <summary>
		/// Inizializza in frame buffer e la shadow map data una specifica dimensione
		/// </summary>
		/// <param name="width">Larghezza della shadow map in pixels</param>
		/// <param name="height">Altezza della shadow map in pixels</param>
		public ShadowFrameBuffer(int width, int height)
		{
			WIDTH = width;
			HEIGHT = height;
			InitializeFrameBuffer();
		}

		/// <summary>
		/// Elimina il fbo e la shadow map
		/// </summary>
		public void Delete()
		{
			GL.DeleteFramebuffer(fbo);
			GL.DeleteTexture(ShadowMap);
		}
		
		/// <summary>
		/// Attiva il fbo 
		/// </summary>
		public void BindFrameBuffer()
		{
			BindFrameBuffer(fbo, WIDTH, HEIGHT);
		}

		/// <summary>
		/// Disattiva il fbo 
		/// </summary>
		public void UnbindFrameBuffer()
		{
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GL.Viewport(0, 0, WIDTH, HEIGHT);
		}

		/// <summary>
		/// Crea un fbo e ci aggiunge la texure della profondità
		/// </summary>
		private void InitializeFrameBuffer()
		{
			fbo = CreateFrameBuffer();
			ShadowMap = CreateDepthBufferAttachment(WIDTH, HEIGHT);
			UnbindFrameBuffer();
		}

		/// <summary>
		/// Attiva il fbo corrente come render target
		/// </summary>
		/// <param name="frameBuffer">Il fbo da attivare</param>
		/// <param name="width">Larghezza del fbo</param>
		/// <param name="height">Altezza del fbo</param>
		private static void BindFrameBuffer(int frameBuffer, int width, int height)
		{
			GL.BindTexture(TextureTarget.Texture2D, 0);
			GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, frameBuffer);
			GL.Viewport(0, 0, width, height);
		}

		/// <summary>
		/// Crea un frame buffer e lo attiva in modo che si possano aggiungere attachments
		/// </summary>
		/// <returns>L`handle del fbo</returns>
		private static int CreateFrameBuffer()
		{
			int frameBuffer = GL.GenFramebuffer();
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);
			GL.DrawBuffer(DrawBufferMode.None);
			GL.ReadBuffer(ReadBufferMode.None);
			return frameBuffer;
		}

		/// <summary>
		/// Crea una texture di profondità
		/// </summary>
		/// <param name="width">Larghezza della texture in pixel</param>
		/// <param name="height">Altezza della texture in pixel</param>
		/// <returns>Handle della texture</returns>
		private static int CreateDepthBufferAttachment(int width, int height)
		{
			int texture = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, texture);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent16, width, height, 0, PixelFormat.DepthComponent, PixelType.Float, (IntPtr)null);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
			GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, texture, 0);
			return texture;
		}
	}

	public class ShadowMapEntityRenderer
	{

		private Matrix4 projectionViewMatrix;
		private ShadowShader shader;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="shader">Shader usato per il rendering</param>
		/// <param name="projectionViewMatrix">La projection matrix moltiplicata per la view matrix della luce</param>
		public ShadowMapEntityRenderer(ShadowShader shader, Matrix4 projectionViewMatrix)
		{
			this.shader = shader;
			this.projectionViewMatrix = projectionViewMatrix;
		}
		
		/// <summary>
		/// Renderizza entità sulla shadowMap
		/// </summary>
		/// <param name="entities">Le entità da renderizzare e il loro modello associato</param>
		public void Render(Dictionary<TexturedModel, List<Entity>> entities)
		{
			foreach(TexturedModel model in entities.Keys)
			{
				RawModel rawModel = model.model;
				BindModel(rawModel);
				foreach(Entity entity in entities[model])
				{
					PrepareInstance(entity);
					GL.DrawElements(PrimitiveType.Triangles, rawModel.VertexCount, DrawElementsType.UnsignedInt, 0);
				}
			}
			GL.DisableVertexAttribArray(0);
			GL.BindVertexArray(0);
		}

		/// <summary>
		/// Attiva un rawModel prima del rendering
		/// </summary>
		/// <param name="rawModel">Il rawModel da attivare</param>
		private void BindModel(RawModel rawModel)
		{
			GL.BindVertexArray(rawModel.VaoHandle);
			GL.EnableVertexAttribArray(0);
		}

		/// <summary>
		/// Prepara le entità per essere renderizzate
		/// </summary>
		/// <param name="entity">Le entità da renderizzare</param>
		private void PrepareInstance(Entity entity)
		{
			Matrix4 modelMatrix = Util.CreateTransformationMatrix(entity.Position,entity.rX, entity.rY, entity.rZ, entity.Scale);
			////////////////////////////////////
			Matrix4 mvpMatrix = projectionViewMatrix * modelMatrix;
			shader.LoadMvpMatrix(mvpMatrix);
		}
	}

	public class ShadowMapMasterRenderer
	{
		public Matrix4 lightViewMatrix { get; private set; } = Matrix4.Identity;

		private const int SHADOW_MAP_SIZE = 2048;
		private ShadowFrameBuffer shadowFbo;
		private ShadowShader shader;
		private ShadowBox shadowBox;
		private Matrix4 projectionMatrix = Matrix4.Identity;
		private Matrix4 projectionViewMatrix = Matrix4.Identity;
		private Matrix4 offset = CreateOffset();
		private ShadowMapEntityRenderer entityRenderer;

		/// <summary>
		/// Crea un`istanza del MasterRenderer
		/// </summary>
		/// <param name="camera">La camper usata nella scena</param>
		/// <param name="width">Larghezza in pixel della finestra</param>
		/// <param name="height">Altezza in pixel della finestra</param>
		public ShadowMapMasterRenderer(Camera camera, int width, int height)
		{
			shader = new ShadowShader();
			shadowBox = new ShadowBox(lightViewMatrix, camera, width, height);
			shadowFbo = new ShadowFrameBuffer(SHADOW_MAP_SIZE, SHADOW_MAP_SIZE);
			entityRenderer = new ShadowMapEntityRenderer(shader, projectionViewMatrix);
		}

		/// <summary>
		/// Renderizza al scena nella shadowMap
		/// </summary>
		/// <param name="entities">La lista di entità da renderizzare associate al loro modello</param>
		/// <param name="sun">La luce della scena</param>
		public void Render(Dictionary<TexturedModel, List<Entity>> entities, Light sun)
		{
			shadowBox.Update();
			Vector3 sunPosition = sun.Position;
			Vector3 lightDirection = new Vector3(-sunPosition.X, -sunPosition.Y, -sunPosition.Z);
			Prepare(lightDirection, shadowBox);
			entityRenderer.Render(entities);
			Finish();
		}

		/// <summary>
		/// Questa matrice è usata per convertire in shadow map space
		/// </summary>
		/// <returns>Una matrice che trasforma una posizione in world space in coordinate 2D sulla shadowMap</returns>
		public Matrix4 GetToShadowMapSpaceMatrix()
		{
			////////////////////////////////
			return offset * projectionViewMatrix;
		}

		/// <summary>
		/// Distrugge lo shader e fbo
		/// </summary>
		public void Delete()
		{
			shader.Delete();
			shadowFbo.Delete();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>Handle della texture</returns>
		public int GetShadowMap()
		{
			return shadowFbo.ShadowMap;
		}

		/// <summary>
		/// Preparazione prima del rendering
		/// </summary>
		/// <param name="lightDirection">La direzione dei raggi del sole</param>
		/// <param name="box">Informazioni riguardo il "view cuboid"</param>
		private void Prepare(Vector3 lightDirection, ShadowBox box)
		{
			UpdateOrthoProjectionMatrix(box.GetWidth(), box.GetHeight(), box.GetLength());
			UpdateLightViewMatrix(lightDirection, box.GetCenter());
			/////////////////
			projectionMatrix = lightViewMatrix * projectionViewMatrix;
			shadowFbo.BindFrameBuffer();
			GL.Enable(EnableCap.DepthTest);
			GL.Clear(ClearBufferMask.DepthBufferBit);
			shader.Start();
		}

		/**
		 * Finish the shadow render pass. Stops the shader and unbinds the shadow
		 * FBO, so everything rendered after this point is rendered to the screen,
		 * rather than to the shadow FBO.
		 */
		private void Finish()
		{
			shader.Stop();
			shadowFbo.UnbindFrameBuffer();
		}

		/// <summary>
		/// Aggiorna la view matrix della luce allienandola alla direzione specificata
		/// </summary>
		/// <param name="direction">Direzione verso la quale la luce sta puntando</param>
		/// <param name="center">Il centro del "view cuboid" in wolrd space</param>
		private void UpdateLightViewMatrix(Vector3 direction, Vector3 center)
		{
			direction.Normalize();
			center = new Vector3(-center.X, -center.Y, -center.Z);
			lightViewMatrix = Matrix4.Identity;
			float pitch = (float)Math.Acos(new Vector2(direction.X, direction.Z).Length);
			//////////////
			lightViewMatrix *= Matrix4.CreateRotationX(pitch);
			float yaw = (float)MathHelper.RadiansToDegrees(((float)Math.Atan(direction.X / direction.Z)));
			yaw = direction.Z > 0 ? yaw - 180 : yaw;
			////////////////
			lightViewMatrix *= Matrix4.CreateRotationY((float)-MathHelper.DegreesToRadians(yaw));
			////////////////////
			lightViewMatrix *= Matrix4.CreateTranslation(center);
		}

		/// <summary>
		/// Crea una matrice che imposta la larghezza, altezza e altezza della vista
		/// </summary>
		/// <param name="width">Larghezza della shadow box</param>
		/// <param name="height">Altezza della shadow box</param>
		/// <param name="length">Lunghezza della shadow box</param>
		private void UpdateOrthoProjectionMatrix(float width, float height, float length)
		{
			projectionMatrix = Matrix4.Identity;
			projectionMatrix.M11 = 2f / width;
			projectionMatrix.M22 = 2f / height;
			projectionMatrix.M33 = -2f / length;
			projectionMatrix.M44 = 1;
		}

		/// <summary>
		/// Crea l`offset per la conversione a shadow map space
		/// </summary>
		/// <returns>L`offset in formato matrice</returns>
		private static Matrix4 CreateOffset()
		{
			Matrix4 offset;
			offset = Matrix4.CreateScale(new Vector3(0.5f, 0.5f, 0.5f));
			//////////////////////////
			offset *= Matrix4.CreateTranslation(new Vector3(0.5f, 0.5f, 0.5f));
			return offset;
		}
	}

	public class ShadowShader : Shader
	{
		private const string VERTEX_FILE = "ShadowVertexShader.vert";
		private const string FRAGMENT_FILE = "ShadowFragmentShader.frag";
		/// <summary>
		/// Model view e projection matrix
		/// </summary>
		private int handleMvpMatrix;

		public ShadowShader() : base(VERTEX_FILE, FRAGMENT_FILE)
		{

		}

		protected override void GetAllUniformLocations()
		{
			handleMvpMatrix = GetUniformLocation("mvpMatrix");

		}

		protected override void BindAttributes()
		{
			BindAttribute(0, "in_position");
		}
		public void LoadMvpMatrix(Matrix4 mvpMatrix)
		{
			LoadToUniform(handleMvpMatrix, mvpMatrix);
		}
	}
}