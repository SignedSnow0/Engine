using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using System;
using System.Collections.Generic;

namespace Engine
{
    public class CoreEngine : GameWindow
    {
        /// <summary>
        /// Numero di millisecondi passati tra questo frame e quello precedente
        /// </summary>
        public static float Delta { get; private set; }

        public static long FrameTime;

        private Time watch = new Time();

        private Loader loader;

        private ModelData playerData;
        private RawModel playerModel;
        private TexturedModel playerTextured;

        private List<Entity> entities;
        private List<Entity> normalMapEntities;
        private List<GuiTexture> guiTextures;

        private Camera camera;

        private List<Light> lights = new List<Light>();

        private MasterRenderer renderer;

        private Terrain terrain;

        private TexturedModel rocks;

        private Entity rockEntity;
        private Entity bobbleEntity;
        private Entity lampEntity;
        private Entity lampEntityMouse;

        private TerrainTexture backgroundTexture;
        private TerrainTexture rTexture;
        private TerrainTexture gTexture;
        private TerrainTexture bTexture;
        private TerrainTexture blendMap;

        private List<Terrain> terrains;

        private TerrainTexturePack texturePack;

        private Player playerEntity;

        private MousePicker mousePicker;

        private WaterShader waterShader;
        private WaterRenderer waterRenderer;
        private List<WaterTile> waters;
        private WaterFrameBuffer waterFrameBuffers;
        private WaterTile water;
        private GuiRenderer guiRenderer;
        private ParticleSystem particles;
        private ParticleTexture particleTexture;
        public CoreEngine(int width = 800, int height = 600, string title = "Non va, oid!") : base(width, height, GraphicsMode.Default, title, GameWindowFlags.Default, DisplayDevice.Default, 0, 0, GraphicsContextFlags.ForwardCompatible)
        {

        }

        protected override void OnLoad(EventArgs e)
        {
            Random random = new Random();
            loader = new Loader();
            entities = new List<Entity>();
            guiTextures = new List<GuiTexture>();
            //********************Player***********************
            playerData = OBJLoader.LoadOBJ("Person.obj");
            playerModel = loader.LoadToVao(playerData.vertices, playerData.textureCoords, playerData.normals, playerData.indices);
            playerTextured = new TexturedModel(playerModel, new ModelTexture(loader.LoadTexture("PlayerTexture.png")));
            playerEntity = new Player(playerTextured, new Vector3(77.0f, 85.0f, -80.0f), 0.0f, -209.0f, 0.0f, 0.7f);
            entities.Add(playerEntity);
            //*************************************************

            //********************Camera***********************
            camera = new Camera(playerEntity);
            //*************************************************

            TextMaster.Init(loader, Width, Height);
            renderer = new MasterRenderer(loader, camera, Width, Height);

            terrains = new List<Terrain>();

            normalMapEntities = new List<Entity>();
            lights = new List<Light>();

            //*****************Particelle**********************
            particleTexture = new ParticleTexture(loader.LoadTexture("ParticleAtlas.png"), 4);

            ParticleMaster.Init(loader, renderer.ProjectionMatrix);
            particles = new ParticleSystem(50.0f, 25.0f, 0.3f, 4.0f, particleTexture);
            #region Sistema di particelle avanzato (da fixare)
            //particles = new ParticleSystem(50.0f, 25.0f, 0.3f, 4.0f, 1.0f);
            //particles.RandomizeRotation();
            //particles.SetDirection(new Vector3(0.0f, 1.0f, 0.0f), 0.1f);
            //particles.SetLifeError(0.1f);
            //particles.SetSpeedError(0.4f);
            //particles.SetScaleError(0.8f);
            #endregion
            //*************************************************

            //*********************Font************************
            FontType font = new FontType(loader.LoadFontTexture("FontDistanceField.png"), "FontDistanceField.fnt");
            GUIText text = new GUIText("Enacoid text!", 1, font, new Vector2(0.0f, 0.0f), 1.0f, true);
            //*************************************************

            //********************TerrainTexture***************
            backgroundTexture = new TerrainTexture(loader.LoadTexture("Grassy2.png"));
            rTexture = new TerrainTexture(loader.LoadTexture("Mud.png"));
            gTexture = new TerrainTexture(loader.LoadTexture("GrassFlowers.png"));
            bTexture = new TerrainTexture(loader.LoadTexture("Path.png"));
            texturePack = new TerrainTexturePack(backgroundTexture, rTexture, gTexture, bTexture);
            blendMap = new TerrainTexture(loader.LoadTexture("BlendMap.png"));
            //*************************************************

            //********************Terreno**********************
            terrain = new Terrain(0.0f, -1.0f, loader, texturePack, blendMap, "HeightMap.png");
            terrains.Add(terrain);
            //*************************************************

            //*******************TexturedModel*****************
            rocks = new TexturedModel(loader.LoadToVao(OBJLoader.LoadOBJ("rocks.obj")), new ModelTexture(loader.LoadTexture("rocks.png")));

            ModelTexture fernTextureAtlas = new ModelTexture(loader.LoadTexture("TextureAtlas.png"));
            fernTextureAtlas.NumberOfRows = 2;
            TexturedModel fern = new TexturedModel(loader.LoadToVao(OBJLoader.LoadOBJ("fern.obj")), fernTextureAtlas);
            fern.Texture.hasTransparency = true;

            TexturedModel bobble = new TexturedModel(loader.LoadToVao(OBJLoader.LoadOBJ("pine.obj")), new ModelTexture(loader.LoadTexture("pine.png")));
            bobble.Texture.hasTransparency = true;

            TexturedModel lamp = new TexturedModel(loader.LoadToVao(OBJLoader.LoadOBJ("lamp.obj")), new ModelTexture(loader.LoadTexture("lamp.png")));
            lamp.Texture.useFakeLighting = true;
            //*************************************************

            //******************Entità*************************
            rockEntity = new Entity(rocks, new Vector3(75.0f, terrain.GetHeightOfTerrain(75.0f, -75.0f), -75.0f), 0.0f, 0.0f, 0.0f, 10.0f);
            entities.Add(rockEntity);
            bobbleEntity = new Entity(bobble, new Vector3(85.0f, terrain.GetHeightOfTerrain(85.0f, -75.0f), -75.0f), 0.0f, 0.0f, 0.0f, 1.0f);
            entities.Add(bobbleEntity);
            lampEntity = new Entity(lamp, new Vector3(65.0f, terrain.GetHeightOfTerrain(65.0f, -75.0f), -75.0f), 0.0f, 0.0f, 0.0f, 1.0f);
            entities.Add(lampEntity);

            for (int i = 0; i < 60; i++)
            {
                if (i % 3 == 0)
                {
                    float x = (float)random.NextDouble() * 150;
                    float z = (float)random.NextDouble() * -150;
                    if (!((x > 50 && x < 100) || (z < -50 && z > -100)))
                    {
                        float y = terrain.GetHeightOfTerrain(x, z);
                        entities.Add(new Entity(fern, new Vector3(x, y, z), 0, (float)random.NextDouble() * 360, 0, 0.9f, random.Next(3)));
                    }
                }
                if (i % 2 == 0)
                {
                    float x = (float)random.NextDouble() * 150;
                    float z = (float)random.NextDouble() * -150;
                    if (!((x > 50 && x < 100) || (z < -50 && z > -100)))
                    {
                        float y = terrain.GetHeightOfTerrain(x, z);
                        entities.Add(new Entity(bobble, new Vector3(x, y, z), 0, (float)random.NextDouble() * 360, 0, (float)random.NextDouble() * 0.6f + 0.8f, random.Next(3)));
                    }
                }
            }
            //*************************************************

            //*******************NormalMapped******************
            TexturedModel barrelModel = new TexturedModel(NormalMappedObjLoader.LoadOBJ("Barrel.obj", loader), new ModelTexture(loader.LoadTexture("Barrel.png")));
            barrelModel.Texture.shineDamper = 10.0f;
            barrelModel.Texture.reflectivity = 0.5f;
            barrelModel.Texture.NormalMap = loader.LoadTexture("BarrelNormal.png");
            normalMapEntities.Add(new Entity(barrelModel, new Vector3(163.0f, terrain.GetHeightOfTerrain(163.0f, -67.0f) + 6.5f, -67.0f), 0.0f, 0.0f, 0.0f, 1.0f));

            TexturedModel boulderModel = new TexturedModel(NormalMappedObjLoader.LoadOBJ("Boulder.obj", loader), new ModelTexture(loader.LoadTexture("Boulder.png")));
            boulderModel.Texture.shineDamper = 5.0f;
            boulderModel.Texture.reflectivity = 0.3f;
            boulderModel.Texture.NormalMap = loader.LoadTexture("BoulderNormal.png");
            normalMapEntities.Add(new Entity(boulderModel, new Vector3(59.0f, terrain.GetHeightOfTerrain(59, -149) + 6.5f, -149), 0.0f, 0.0f, 0.0f, 1.0f));
            ///*******************Luci*************************
            lights.Add(new Light(new Vector3(199000.0f, 200000.0f, -84000.0f), new Vector3(0.6f, 0.6f, 0.6f)));
            
            //*************************************************

            //********************Acqua************************
            waterShader = new WaterShader();
            waterFrameBuffers = new WaterFrameBuffer(Width, Height);
            waterRenderer = new WaterRenderer(loader, waterShader, renderer.ProjectionMatrix, waterFrameBuffers);
            waters = new List<WaterTile>();
            water = new WaterTile(187.0f, -199.0f, terrain.GetHeightOfTerrain(187.0f, -199.0f) + 7.0f);
            waters.Add(water);
            //*************************************************

            mousePicker = new MousePicker(camera, renderer.ProjectionMatrix);

            //*********************GUIs************************
            GuiTexture shadowMap = new GuiTexture(renderer.GetShadowMapTexture(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            //guiTextures.Add(shadowMap);

            guiRenderer = new GuiRenderer(loader);
            //*************************************************

            base.OnLoad(e);

            FrameTime = watch.GetCurrentTime();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            long currentFrameTime = watch.GetCurrentTime();
            Delta = watch.Delta(currentFrameTime, FrameTime);

            playerEntity.Move(terrains);
            camera.Move();
            mousePicker.Update();

            ParticleMaster.Update(camera);

            KeyboardState input = Keyboard.GetState();
            if (input.IsKeyDown(Key.LShift))
            {
                particles.GenerateParticles(playerEntity.Position);
            }
            base.OnUpdateFrame(e);

            //Console.WriteLine($"{playerEntity.Position}");

            FrameTime = currentFrameTime;
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            renderer.RenderShadowMap(entities, lights[0]);


            GL.Enable(EnableCap.ClipDistance0);

            waterFrameBuffers.BindReflectionFrameBuffer();

            float distance = 2 * (camera.Position.Y - water.Height);
            camera.Position.Y -= distance;
            camera.Pitch = -camera.Pitch;
            renderer.Render(entities, normalMapEntities, terrains, lights, camera, new Vector4(0.0f, 1.0f, 0.0f, -water.Height + 0.5f));
            camera.Position.Y += distance;
            camera.Pitch = -camera.Pitch;

            waterFrameBuffers.BindRefractionFrameBuffer();
            renderer.Render(entities, normalMapEntities, terrains, lights, camera, new Vector4(0.0f, -1.0f, 0.0f, water.Height));

            GL.Disable(EnableCap.ClipDistance0);

            waterFrameBuffers.UnbindCurrentFrameBuffer(Width, Height);
            renderer.Render(entities, normalMapEntities, terrains, lights, camera, new Vector4(0.0f, 1.0f, 0.0f, 0.0f));
            waterRenderer.Render(waters, camera, lights[0]);
            ParticleMaster.Render(camera);
            TextMaster.Render();
            guiRenderer.Render(guiTextures);

            Context.SwapBuffers();
            base.OnRenderFrame(e);
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            TextMaster.windowWidth = Width;
            TextMaster.windowHeight = Height;
            renderer.Width = Width;
            renderer.Height = Height;
            mousePicker.width = Width;
            mousePicker.height = Height;
            base.OnResize(e);
        }

        protected override void OnUnload(EventArgs e)
        {
            ParticleMaster.CleanUp();
            loader.Delete();
            renderer.Delete();
            waterFrameBuffers.Delete();
            TextMaster.Delete();
            base.OnUnload(e);
        }
    }
}
