using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class GuiTexture
    {
        public int Texture { get; private set; }
        public Vector2 Position { get; private set; }
        public Vector2 Scale { get; private set; }

        public GuiTexture(int texture, Vector2 position, Vector2 scale)
        {
            Texture = texture;
            Position = position;
            Scale = scale;
        }
    }

    public class GuiRenderer
    {
        private RawModel quad;
        private GuiShader shader;

        public GuiRenderer(Loader loader)
        {
            float[] positions = new float[] { -1.0f,  1.0f,
                                              -1.0f, -1.0f,
                                               1.0f,  1.0f, 
                                               1.0f, -1.0f};
            quad = loader.LoadToVao(positions, 2);
            shader = new GuiShader();
        }

        public void Render(List<GuiTexture> guis)
        {
            shader.Start();

            GL.BindVertexArray(quad.VaoHandle);
            GL.EnableVertexAttribArray(0);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.Disable(EnableCap.DepthTest);

            foreach (GuiTexture gui in guis)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, gui.Texture);

                Matrix4 matrix = Util.CreateTransformationMatrix(gui.Position, gui.Scale);
                shader.LoadTransformationMatrix(matrix);

                GL.DrawArrays(PrimitiveType.TriangleStrip, 0, quad.VertexCount);
            }

            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);

            GL.DisableVertexAttribArray(0);
            GL.BindVertexArray(0);

            shader.Stop();
        }

        public void Delete()
        {
            shader.Delete();
        }
    }

    public class GuiShader : Shader
    {
        private const string vertexFileName = "GuiVertexShader.vert";
	    private const string fragmentFileName = "GuiFragmentShader.frag";
	
	    private int handleTransformationMatrix;

        public GuiShader() : base(vertexFileName, fragmentFileName)
        {
           
        }
        protected override void GetAllUniformLocations()
        {
            handleTransformationMatrix = GetUniformLocation("transformationMatrix");
        }
        protected override void BindAttributes()
        {
            BindAttribute(0, "position");
        }

        public void LoadTransformationMatrix(Matrix4 matrix)
        {
            LoadToUniform(handleTransformationMatrix, matrix);
        }

    }
}
