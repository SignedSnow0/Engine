using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
	public class SkyboxShader : Shader
	{
		private const string VERTEX_FILE = "SkyboxVertexShader.vert";
		private const string FRAGMENT_FILE = "SkyboxFragmentShader.frag";
	
		private int location_projectionMatrix;
		private int location_viewMatrix;

		public SkyboxShader() : base(VERTEX_FILE, FRAGMENT_FILE)
		{
			
		}

		protected override void GetAllUniformLocations()
		{
			location_projectionMatrix = GetUniformLocation("projectionMatrix");
			location_viewMatrix = GetUniformLocation("viewMatrix");
		}
		protected override void BindAttributes()
		{
			BindAttribute(0, "position");
		}

		public void LoadProjectionMatrix(Matrix4 matrix)
		{
			LoadToUniform(location_projectionMatrix, matrix);
		}

		public void LoadViewMatrix(Camera camera)
		{
			Matrix4 matrix = Util.CreateViewMatrix(camera);
			//questi 3 valori indicano la traslazione, impostandoli a 0 la skybox rimmarrà sempre al centro della camera, ma continuerà a ruotare
			matrix.M41 = 0;
			matrix.M42 = 0;
			matrix.M43 = 0;
			LoadToUniform(location_viewMatrix, matrix);
		}		
	}

	public class SkyboxRenderer
	{
		private const float SIZE = 500f;

		private float[] VERTICES =  {
			-SIZE,  SIZE, -SIZE,
			-SIZE, -SIZE, -SIZE,
			 SIZE, -SIZE, -SIZE,
			 SIZE, -SIZE, -SIZE,
			 SIZE,  SIZE, -SIZE,
			-SIZE,  SIZE, -SIZE,

			-SIZE, -SIZE,  SIZE,
			-SIZE, -SIZE, -SIZE,
			-SIZE,  SIZE, -SIZE,
			-SIZE,  SIZE, -SIZE,
			-SIZE,  SIZE,  SIZE,
			-SIZE, -SIZE,  SIZE,

			 SIZE, -SIZE, -SIZE,
			 SIZE, -SIZE,  SIZE,
			 SIZE,  SIZE,  SIZE,
			 SIZE,  SIZE,  SIZE,
			 SIZE,  SIZE, -SIZE,
			 SIZE, -SIZE, -SIZE,

			-SIZE, -SIZE,  SIZE,
			-SIZE,  SIZE,  SIZE,
			 SIZE,  SIZE,  SIZE,
			 SIZE,  SIZE,  SIZE,
			 SIZE, -SIZE,  SIZE,
			-SIZE, -SIZE,  SIZE,

			-SIZE,  SIZE, -SIZE,
			 SIZE,  SIZE, -SIZE,
			 SIZE,  SIZE,  SIZE,
			 SIZE,  SIZE,  SIZE,
			-SIZE,  SIZE,  SIZE,
			-SIZE,  SIZE, -SIZE,

			-SIZE, -SIZE, -SIZE,
			-SIZE, -SIZE,  SIZE,
			 SIZE, -SIZE, -SIZE,
			 SIZE, -SIZE, -SIZE,
			-SIZE, -SIZE,  SIZE,
			 SIZE, -SIZE,  SIZE};

		private string[] textureFileNames = { "right.png", "left.png", "top.png", "bottom.png", "back.png", "front.png" };

		private RawModel cube;
		private int texture;
		private SkyboxShader shader;

		public SkyboxRenderer(Loader loader, Matrix4 projectionMatrix)
        {
			cube = loader.LoadToVao(VERTICES, 3);
			texture = loader.LoadCubeMap(textureFileNames);
			shader = new SkyboxShader();
			shader.Start();
			shader.LoadProjectionMatrix(projectionMatrix);
			shader.Stop();
        }

		public void Render(Camera camera)
        {
			shader.Start();
			shader.LoadViewMatrix(camera);
			GL.BindVertexArray(cube.VaoHandle);
			GL.EnableVertexAttribArray(0);
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.TextureCubeMap, texture);
			GL.DrawArrays(PrimitiveType.Triangles, 0, cube.VertexCount);
			GL.DisableVertexAttribArray(0);
			GL.BindVertexArray(0);
			shader.Stop();
        }
	}
}
