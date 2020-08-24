using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;

namespace Engine
{
    /// <summary>
    /// Insieme di un modello, posizione, rotazione e dimensione nel mondo
    /// </summary>
    public class Entity
    {
        public Vector3 Position;

        public TexturedModel Model { get; private set; }
        public float rX { get; private set; }
        public float rY{ get; private set; }
        public float rZ { get; private set; }
        public float Scale { get; private set; }
        public int TextureIndex { get; private set; } = 0;

        /// <summary>
        /// Crea un oggetto della classe Entity
        /// </summary>
        /// <param name="model">Il modello dell`entità</param>
        /// <param name="position">La posizione nel mondo</param>
        /// <param name="rx">La rotazione rispetto l`asse X</param>
        /// <param name="ry">La rotazione rispetto l`asse Y</param>
        /// <param name="rz">La rotazione rispetto l`asse Z</param>
        /// <param name="scale">La dimensione dell`oggetto</param>
        public Entity(TexturedModel model, Vector3 position, float rx, float ry, float rz, float scale)
        {
            Model = model;
            Position = position;
            rX = rx;
            rY = ry;
            rZ = rz;
            Scale = scale;
        }
        /// <summary>
        /// Crea un oggetto della classe Entity
        /// </summary>
        /// <param name="model">Il modello dell`entità</param>
        /// <param name="position">La posizione nel mondo</param>
        /// <param name="rx">La rotazione rispetto l`asse X</param>
        /// <param name="ry">La rotazione rispetto l`asse Y</param>
        /// <param name="rz">La rotazione rispetto l`asse Z</param>
        /// <param name="scale">La dimensione dell`oggetto</param>
        /// <param name="textureIndex">L`indice della texture in un texture atlas</param>
        public Entity(TexturedModel model, Vector3 position, float rx, float ry, float rz, float scale, int textureIndex)
        {
            this.Model = model;
            Position = position;
            rX = rx;
            rY = ry;
            rZ = rz;
            Scale = scale;
            TextureIndex = textureIndex;
        }
        public float GetTextureOffsetX()
        {
            int column = TextureIndex % Model.Texture.NumberOfRows;
            return column / (float)Model.Texture.NumberOfRows;
        }
        public float GetTextureOffsetY()
        {
            int row = TextureIndex / Model.Texture.NumberOfRows;
            return row / (float)Model.Texture.NumberOfRows;
        }
        /// <summary>
        /// Cambia la posizione dell`entità
        /// </summary>
        /// <param name="x">Spostamento nell`asse X</param>
        /// <param name="y">Spostamento nell`asse Y</param>
        /// <param name="z">Spostamento nell`asse Z</param>
        public void Move(float x, float y, float z)
        {
            Position += new Vector3(x, y, z);
        }
        /// <summary>
        /// Cambia la posizione dell`entità
        /// </summary>
        /// <param name="movement">La quantità di spostamento in ogni asse</param>
        public void Move(Vector3 movement)
        {
            Position += movement;
        }
        /// <summary>
        /// Ruota l`oggetto
        /// </summary>
        /// <param name="rx">Angolo di rotazione nell`asse X</param>
        /// <param name="ry">Angolo di rotazione nell`asse Y</param>
        /// <param name="rz">Angolo di rotazione nell`asse Z</param>
        public void Rotate(float rx, float ry, float rz)
        {
            this.rX += rx;
            this.rY += ry;
            this.rZ += rz;
        }
        /// <summary>
        /// Ruota l`oggetto
        /// </summary>
        /// <param name="rotation">Anglo di rotazione di ogni asse</param>
        public void Rotate(Vector3 rotation)
        {
            this.rX += rotation.X;
            this.rY += rotation.Y;
            this.rZ += rotation.Z;
        }
    }

    /// <summary>
    /// Renderer con metodi specifici per le entità
    /// </summary>
    public class EntityRenderer
    {
        private StaticShader shader;

        /// <summary>
        /// Istanzia un oggetto di classe EntityRenderer
        /// </summary>
        /// <param name="shader">Lo shader da utilizzare per il rendering</param>
        /// <param name="projectionMatrix">La matrice utilizzata per la visione finale della telecamera</param>
        public EntityRenderer(StaticShader shader, Matrix4 projectionMatrix)
        {
            this.shader = shader;
            shader.Start();
            shader.LoadProjectionMatrix(projectionMatrix);
            shader.Stop();
        }

        /// <summary>
        /// Renderizza un insieme di modelli
        /// </summary>
        /// <param name="entities">Dizionario di modelli dove la chiave è il loro tipo e il valore associato è una lista di modelli di quel tipo</param>
        public void Render(Dictionary<TexturedModel, List<Entity>> entities)
        {
            foreach (TexturedModel texturedModel in entities.Keys)
            {
                PrepareTexturedModel(texturedModel);
                List<Entity> batch;
                entities.TryGetValue(texturedModel, out batch);
                foreach (Entity entity in batch)
                {
                    PrepareInstance(entity);
                    //primo modello è con texture, secondo solo vertici
                    GL.DrawElements(BeginMode.Triangles, texturedModel.model.VertexCount, DrawElementsType.UnsignedInt, 0);
                }
                UnbindTexturedModel();
            }
        }

        private void PrepareTexturedModel(TexturedModel texturedModel)
        {
            RawModel model = texturedModel.model;

            //rendo il vao attivo
            GL.BindVertexArray(model.VaoHandle);

            //Indico che voglio usare le informazioni nella riga 0
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);

            ModelTexture texture = texturedModel.Texture;
            shader.LoadNumberOfRows(texture.NumberOfRows);
            if (texture.hasTransparency)
            {
                MasterRenderer.DisableCulling();
            }
            shader.LoadFakeLighting(texture.useFakeLighting);
            shader.LoadShineVariables(texture.shineDamper, texture.reflectivity);

            //Rendo attiva l`area della texture 0
            GL.ActiveTexture(TextureUnit.Texture0);
            //Riempio l`area texture 0
            GL.BindTexture(TextureTarget.Texture2D, texturedModel.Texture.handle);
        }
        private void UnbindTexturedModel()
        {
            MasterRenderer.EnableCulling();
            GL.DisableVertexAttribArray(0);
            GL.DisableVertexAttribArray(1);
            GL.DisableVertexAttribArray(2);
            //scollego il vao
            GL.BindVertexArray(0);
        }
        private void PrepareInstance(Entity entity)
        {
            Matrix4 transformationMatrix = Util.CreateTransformationMatrix(entity.Position, entity.rX, entity.rY, entity.rZ, entity.Scale);
            shader.LoadTransformationMatrix(transformationMatrix);
            shader.LoadOffset(new Vector2(entity.GetTextureOffsetX(), entity.GetTextureOffsetY()));
        }
    }
}
