using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;

namespace Engine
{
    /// <summary>
    /// Render utilizzato per scopi generici
    /// </summary>
    public class MasterRenderer
    {
        public const float FOV = 70.0f;
        public const float NEAR_PLANE = 0.1f;
        public const float FAR_PLANE = 1000;
        public const float RED = 0.5f;
        public const float GREEN = 0.5f;
        public const float BLUE = 0.5f;
        private Dictionary<TexturedModel, List<Entity>> entities = new Dictionary<TexturedModel, List<Entity>>();
        private Dictionary<TexturedModel, List<Entity>> normalMapEntities = new Dictionary<TexturedModel, List<Entity>>();
        private List<Terrain> terrains = new List<Terrain>();
        private TerrainRenderer terrainRenderer;
        private TerrainShader terrainShader = new TerrainShader();
        private SkyboxRenderer skyboxRenderer;
        private ShadowMapMasterRenderer shadowMapRenderer;

        private NormalMappingRenderer normalMappingRenderer;

        public Matrix4 ProjectionMatrix { get; private set; }
        public StaticShader shader { get; private set; } = new StaticShader();
        public EntityRenderer renderer { get; private set; }

        public int Width { get; set; }
        public int Height { get; set; }
        /// <summary>
        /// Instanzia un oggetto di classe MasterRenderer
        /// </summary>
        public MasterRenderer(Loader loader,Camera camera, int width, int height)
        {
            EnableCulling();
            Width = width;
            Height = height;
            CreateProjectionMatrix();
            renderer = new EntityRenderer(shader, ProjectionMatrix);
            terrainRenderer = new TerrainRenderer(terrainShader, ProjectionMatrix);
            skyboxRenderer = new SkyboxRenderer(loader, ProjectionMatrix);
            normalMappingRenderer = new NormalMappingRenderer(ProjectionMatrix);
            shadowMapRenderer = new ShadowMapMasterRenderer(camera, width, height);
        }

        /// <summary>
        /// Aggiunge l`entità a un gruppo di renderering dello stesso tipo se già esistente, altrimenti ne crea uno nuovo
        /// </summary>
        /// <param name="entity">L`entità da aggiungere</param>
        public void ProcessEntity(Entity entity)
        {
            TexturedModel entityModel = entity.Model;
            List<Entity> batch;
            if(entities.TryGetValue(entityModel, out batch))
            {
                batch.Add(entity);
            }
            else
            {
                List<Entity> newBatch = new List<Entity>();
                newBatch.Add(entity);
                entities.Add(entityModel, newBatch);
            }
        }
        public void ProcessNormalMapEntity(Entity entity)
        {
            TexturedModel entityModel = entity.Model;
            List<Entity> batch;
            if (normalMapEntities.TryGetValue(entityModel, out batch))
            {
                batch.Add(entity);
            }
            else
            {
                List<Entity> newBatch = new List<Entity>();
                newBatch.Add(entity);
                normalMapEntities.Add(entityModel, newBatch);
            }
        }
        /// <summary>
        /// Aggiunge il alla lista di terreni da renderizzare
        /// </summary>
        /// <param name="terrain">Il terreno da aggiungere</param>
        public void ProcessTerrain(Terrain terrain)
        {
            terrains.Add(terrain);
        }
        /// <summary>
        /// Renderizza tutte le entità e i terreni aggiunti
        /// </summary>
        /// <param name="light">La luce da utilizzare</param>
        /// <param name="camera">La camera dalla quale si vede la scena</param>
        public void Render(List<Light> lights, Camera camera, Vector4 clipPlane)
        {
            Prepare();
            shader.Start();
            shader.LoadClipPlane(clipPlane);
            shader.LoadSkyColor(RED, GREEN, BLUE);
            shader.LoadLights(lights);
            shader.LoadViewMatrix(camera);

            renderer.Render(entities);

            shader.Stop();

            normalMappingRenderer.Render(normalMapEntities, clipPlane, lights, camera);

            terrainShader.Start();
            terrainShader.LoadClipPlane(clipPlane);
            terrainShader.LoadSkyColor(RED, GREEN, BLUE);
            terrainShader.LoadLights(lights);
            terrainShader.LoadViewMatrix(camera);

            terrainRenderer.Render(terrains);

            terrainShader.Stop();

            skyboxRenderer.Render(camera);

            entities.Clear();
            terrains.Clear();
            normalMapEntities.Clear();
        }
        public void Render(List<Entity> entities,List<Entity> normalMapEntities, List<Terrain> terrains, List<Light> lights, Camera camera, Vector4 clipPlane)
        {
           foreach(Terrain terrain in terrains)
           {
                ProcessTerrain(terrain);
           }
           foreach(Entity entity in entities)
           {
                ProcessEntity(entity);
           }
           foreach(Entity entity in normalMapEntities)
           {
               ProcessNormalMapEntity(entity);
           }
            Render(lights, camera, clipPlane);
        }

        public void RenderShadowMap(List<Entity> entities, Light sun)
        {
            foreach(Entity entity in entities)
            {
                ProcessEntity(entity);
            }
            shadowMapRenderer.Render(this.entities, sun);
            this.entities.Clear();
        }
        /// <summary>
        /// Non renderizza le facce il cui normale non puntano verso la scena
        /// </summary>
        public static void EnableCulling()
        {
            //non renderizza i vertici nascosti
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
        }
        /// <summary>
        /// Renderizza tutte le facce di un oggetto
        /// </summary>
        public static void DisableCulling()
        {
            GL.Disable(EnableCap.CullFace);
        }

        public int GetShadowMapTexture()
        {
            return shadowMapRenderer.GetShadowMap();
        }
        /// <summary>
        /// Elimina dalla memoria tutti gli shader utilizzati
        /// </summary>
        public void Delete()
        {
            shader.Delete();
            terrainShader.Delete();
            normalMappingRenderer.Delete();
            shadowMapRenderer.Delete();
        }

        private void Prepare()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            GL.ClearColor(RED, GREEN, BLUE, 1.0f);
        }
        private void CreateProjectionMatrix()
        {
            ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FOV), Width / (float)Height, NEAR_PLANE, FAR_PLANE);
        }
    }
}
