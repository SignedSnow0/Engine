using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Engine
{
    public class Particle
	{
		public Vector3 Position { get; private set; }
		public float Rotation { get; private set; }
		public float Scale { get; private set; }
        public ParticleTexture Texture { get; private set; }
        public Vector2 TexOffset1 { get; private set; } = new Vector2();
        public Vector2 TexOffset2 { get; private set; } = new Vector2();
        public float BlendFactor { get; private set; }
        public float Distance { get; private set; }

        private Vector3 velocity;
		private float gravityEffect;
		private float lifeLength;
		private float elapsedTime = 0;

        public Particle(Vector3 position, Vector3 velocity, float gravityEffect, float lifeLength, float rotation, float scale, ParticleTexture texture)
		{
			Position = position;
			this.velocity = velocity;
			this.gravityEffect = gravityEffect;
			this.lifeLength = lifeLength;
			Rotation = rotation;
			Scale = scale;
            Texture = texture;

			ParticleMaster.AddParticle(this);
		}

		/// <summary>
		/// Aggiorna le proprietà della particella in base al tempo passato
		/// </summary>
		/// <returns>False se la particella è attiva più della sua vita massima</returns>
		public bool Update(Camera camera)
		{
			velocity.Y += Player.GRAVITY * gravityEffect * Time.delta / 1000.0f;
			Vector3 change = velocity;
			change *= Time.delta / 1000.0f;
			Position += change;
            Distance = (camera.Position - Position).LengthSquared;
            UpdateTextureCoordInfo();
            elapsedTime += Time.delta / 1000.0f;			
			return elapsedTime < lifeLength;
		}


        private void UpdateTextureCoordInfo()
        {
            float lifeFactor = elapsedTime / lifeLength;
            int stageCount = Texture.NumberOfRows * Texture.NumberOfRows;
            float atlasProgression = lifeFactor * stageCount;
            int index1 = (int)Math.Floor(atlasProgression);
            int index2 = index1 < stageCount - 1 ? index1 + 1 : index1;
            BlendFactor = atlasProgression % 1;
            TexOffset1 = SetTextureOffset(index1);
            TexOffset2 = SetTextureOffset(index2);
        }

        private Vector2 SetTextureOffset(int index)
        {
            int column = index % Texture.NumberOfRows;
            int row = index / Texture.NumberOfRows;
            Vector2 offset;
            offset.X = column / (float)Texture.NumberOfRows;
            offset.Y = column / (float)Texture.NumberOfRows;
            return offset;
        }
    }

	public class ParticleRenderer
	{

		private float[] VERTICES = { -0.5f, 0.5f, -0.5f, -0.5f, 0.5f, 0.5f, 0.5f, -0.5f };
        private const int MAX_INSTANCES = 10000;
        private const int INSTANCE_DATA_LENGTH = 21;
        private float[] buffer = new float[MAX_INSTANCES * INSTANCE_DATA_LENGTH];
        //16 per la matrice, 4 per la texture offset, 1 per il blendFactor

		private RawModel quad;
		private ParticleShader shader;
        private Loader loader;
        private int vbo;
        private int pointer = 0;

		public ParticleRenderer(Loader loader, Matrix4 projectionMatrix)
		{
            this.loader = loader;
            vbo = loader.CreateEmptyVbo(INSTANCE_DATA_LENGTH * MAX_INSTANCES);
			quad = loader.LoadToVao(VERTICES, 2);
            loader.AddInstancedAttribute(quad.VaoHandle, vbo, 1, 4, INSTANCE_DATA_LENGTH,  0);
            loader.AddInstancedAttribute(quad.VaoHandle, vbo, 2, 4, INSTANCE_DATA_LENGTH,  4);
            loader.AddInstancedAttribute(quad.VaoHandle, vbo, 3, 4, INSTANCE_DATA_LENGTH,  8);
            loader.AddInstancedAttribute(quad.VaoHandle, vbo, 4, 4, INSTANCE_DATA_LENGTH, 12);
            loader.AddInstancedAttribute(quad.VaoHandle, vbo, 5, 4, INSTANCE_DATA_LENGTH, 16);
            loader.AddInstancedAttribute(quad.VaoHandle, vbo, 6, 1, INSTANCE_DATA_LENGTH, 20);
            shader = new ParticleShader();
			shader.Start();
			shader.LoadProjectionMatrix(projectionMatrix);
			shader.Stop();
		}

		public void Render(Dictionary<ParticleTexture ,List<Particle>> particles, Camera camera)
		{
			Matrix4 viewmatrix = Util.CreateViewMatrix(camera);
			Prepare();
            foreach (ParticleTexture texture in particles.Keys)
            {
                BindTexture(texture);
                pointer = 0;
                float[] vboData = new float[particles[texture].Count * INSTANCE_DATA_LENGTH];
                foreach (Particle particle in particles[texture])
                {
                    UpdateModelViewMatrix(particle.Position, particle.Rotation, particle.Scale, viewmatrix, vboData);
                    UpdateTexCoorInfo(particle, vboData);
                }
                loader.UpdateVbo(vbo, vboData);
                GL.DrawArraysInstanced(PrimitiveType.TriangleStrip, 0, quad.VertexCount, particles[texture].Count);
            }

			FinishRendering();
		}

		public void Delete()
		{
			shader.Delete();
		}
        
        private void BindTexture(ParticleTexture texture)
        {
            if (texture.Additive)
            {
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
            }
            else
            {
                GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            }
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture.TextureHandle);
            shader.LoadNumberOfRows(texture.NumberOfRows);
        }

		private void UpdateModelViewMatrix(Vector3 position, float rotation, float scale, Matrix4 viewMatrix, float[] vboData)
		{
			Matrix4 modelMatrix = Matrix4.CreateTranslation(position);
			modelMatrix.M11 = viewMatrix.M11;
			modelMatrix.M12 = viewMatrix.M21;
			modelMatrix.M13 = viewMatrix.M31;
			modelMatrix.M21 = viewMatrix.M12;
			modelMatrix.M22 = viewMatrix.M22;
			modelMatrix.M23 = viewMatrix.M32;
			modelMatrix.M31 = viewMatrix.M13;
			modelMatrix.M32 = viewMatrix.M23;
			modelMatrix.M33 = viewMatrix.M33;
			Matrix4 rotationMatrix = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rotation));
			Matrix4 scaleMatrix = Matrix4.CreateScale(scale);
			Matrix4 scaleRotationMatrix = scaleMatrix * rotationMatrix;
			modelMatrix *= scaleRotationMatrix;
			Matrix4 modelViewMatrix = modelMatrix * viewMatrix;
            StoreMatrixData(modelViewMatrix, vboData);
		}

        private void UpdateTexCoorInfo(Particle particle, float[] data)
        {
            data[pointer++] = particle.TexOffset1.X;
            data[pointer++] = particle.TexOffset1.Y;
            data[pointer++] = particle.TexOffset2.X;
            data[pointer++] = particle.TexOffset2.Y;
            data[pointer++] = particle.BlendFactor;
        }

        private void StoreMatrixData(Matrix4 matrix, float[] vboData)
        {
            vboData[pointer++] = matrix.M11;
            vboData[pointer++] = matrix.M12;
            vboData[pointer++] = matrix.M13;
            vboData[pointer++] = matrix.M14;
            vboData[pointer++] = matrix.M21;
            vboData[pointer++] = matrix.M22;
            vboData[pointer++] = matrix.M23;
            vboData[pointer++] = matrix.M24;
            vboData[pointer++] = matrix.M31;
            vboData[pointer++] = matrix.M32;
            vboData[pointer++] = matrix.M33;
            vboData[pointer++] = matrix.M34;
            vboData[pointer++] = matrix.M41;
            vboData[pointer++] = matrix.M42;
            vboData[pointer++] = matrix.M43;
            vboData[pointer++] = matrix.M44;
        }

		private void Prepare()
		{
			shader.Start();
			GL.BindVertexArray(quad.VaoHandle);
			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			GL.EnableVertexAttribArray(2);
			GL.EnableVertexAttribArray(3);
			GL.EnableVertexAttribArray(4);
			GL.EnableVertexAttribArray(5);
			GL.EnableVertexAttribArray(6);
            GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.DepthMask(false);
		}

		private void FinishRendering()
		{
			GL.DepthMask(true);
			GL.Disable(EnableCap.Blend);
			GL.DisableVertexAttribArray(0);
			GL.DisableVertexAttribArray(1);
			GL.DisableVertexAttribArray(2);
			GL.DisableVertexAttribArray(3);
			GL.DisableVertexAttribArray(4);
			GL.DisableVertexAttribArray(5);
			GL.DisableVertexAttribArray(6);
            GL.BindVertexArray(0);
			shader.Stop();
		}
	}

	public class ParticleShader : Shader
	{

		private const string VERTEX_FILE = "ParticleVertexShader.vert";
		private const string FRAGMENT_FILE = "ParticleFragmentShader.frag";

		private int handleProjectionMatrix;
		private int handleNumberOfRows;

        public ParticleShader() : base(VERTEX_FILE, FRAGMENT_FILE)
		{

		}

		protected override void GetAllUniformLocations()
		{
			handleNumberOfRows = GetUniformLocation("numberOfRows");
			handleProjectionMatrix = GetUniformLocation("projectionMatrix");

        }

		protected override void BindAttributes()
		{
			BindAttribute(0, "position");
			BindAttribute(1, "modelViewMatrix");
			BindAttribute(5, "texOffsets");
			BindAttribute(6, "blendFactor");
        }

        public void LoadNumberOfRows(int numberOfRows)
        {
            LoadToUniform(handleNumberOfRows, (float)numberOfRows);
        }

		public void LoadProjectionMatrix(Matrix4 projectionMatrix)
		{
			LoadToUniform(handleProjectionMatrix, projectionMatrix);
		}
	}

	public static class ParticleMaster
	{
        private static Dictionary<ParticleTexture, List<Particle>> particles = new Dictionary<ParticleTexture, List<Particle>>();
		private static ParticleRenderer renderer;

		public static void Init(Loader loader, Matrix4 projectionMatrix)
        {
			renderer = new ParticleRenderer(loader, projectionMatrix);
        }
		public static void Update(Camera camera)
        {
            foreach(List<Particle> particlesList in particles.Values)
            {
                particlesList.RemoveAll(p => !p.Update(camera));
                if (particlesList.Count > 0 && !particlesList[0].Texture.Additive)
                {
                    InsertionSort.SortHighToLow(particlesList);
                }
            }

			//IEnumerator<Particle> enumerator = particles.GetEnumerator();
			//while(enumerator.MoveNext())
			//{
			//	Particle p = enumerator.Current;
			//	bool stillAlive = p.Update();
			//	if (!stillAlive)
			//	{
			//		particles.Remove(p);
			//	}
			//}
		}
		public static void Render(Camera camera)
        {
			renderer.Render(particles, camera);
        }
		public static void AddParticle(Particle particle)
        {
            List<Particle> list;
			if(!particles.TryGetValue(particle.Texture, out list))
            {
                list = new List<Particle>();
                particles.Add(particle.Texture, list);
            }
            list.Add(particle);
        }
		public static void CleanUp()
        {
			renderer.Delete();
        }
	}
    #region Particle Complicato
    //public class ParticleSystem
    //{

    //	private float pps, averageSpeed, gravityComplient, averageLifeLength, averageScale;

    //	private float speedError, lifeError, scaleError = 0;
    //	private bool randomRotation = false;
    //	private Vector3 direction;
    //	private float directionDeviation = 0;

    //	private Random random = new Random();

    //	public ParticleSystem(float pps, float speed, float gravityComplient, float lifeLength, float scale)
    //	{
    //		this.pps = pps;
    //		this.averageSpeed = speed;
    //		this.gravityComplient = gravityComplient;
    //		this.averageLifeLength = lifeLength;
    //		this.averageScale = scale;
    //	}

    //	/**
    //	 * @param direction - The average direction in which particles are emitted.
    //	 * @param deviation - A value between 0 and 1 indicating how far from the chosen direction particles can deviate.
    //	 */
    //	public void SetDirection(Vector3 direction, float deviation)
    //	{
    //		this.direction = new Vector3(direction);
    //		this.directionDeviation = MathHelper.DegreesToRadians(deviation);
    //	}

    //	public void RandomizeRotation()
    //	{
    //		randomRotation = true;
    //	}

    //	/**
    //	 * @param error
    //	 *            - A number between 0 and 1, where 0 means no error margin.
    //	 */
    //	public void SetSpeedError(float error)
    //	{
    //		this.speedError = error * averageSpeed;
    //	}

    //	/**
    //	 * @param error
    //	 *            - A number between 0 and 1, where 0 means no error margin.
    //	 */
    //	public void SetLifeError(float error)
    //	{
    //		this.lifeError = error * averageLifeLength;
    //	}

    //	/**
    //	 * @param error
    //	 *            - A number between 0 and 1, where 0 means no error margin.
    //	 */
    //	public void SetScaleError(float error)
    //	{
    //		this.scaleError = error * averageScale;
    //	}

    //	public void GenerateParticles(Vector3 systemCenter)
    //	{
    //		float delta = Time.delta / 1000.0f;
    //		float particlesToCreate = pps * delta;
    //		int count = (int)Math.Floor(particlesToCreate);
    //		float partialParticle = particlesToCreate % 1;
    //		for (int i = 0; i < count; i++)
    //		{
    //			EmitParticle(systemCenter);
    //		}
    //		if (random.NextDouble() < partialParticle)
    //		{
    //			EmitParticle(systemCenter);
    //		}
    //	}

    //	private void EmitParticle(Vector3 center)
    //	{
    //		Vector3 velocity;
    //		if (direction != Vector3.Zero)
    //		{
    //			velocity = GenerateRandomUnitVectorWithinCone(direction, directionDeviation);
    //		}
    //		else
    //		{
    //			velocity = GenerateRandomUnitVector();
    //		}
    //		velocity = Vector3.Normalize(velocity);
    //		velocity *= GenerateValue(averageSpeed, speedError);
    //		float scale = GenerateValue(averageScale, scaleError);
    //		float lifeLength = GenerateValue(averageLifeLength, lifeError);
    //		new Particle(new Vector3(center), velocity, gravityComplient, lifeLength, GenerateRotation(), scale);
    //	}

    //	private float GenerateValue(float average, float errorMargin)
    //	{
    //		float offset = ((float)random.NextDouble() - 0.5f) * 2f * errorMargin;
    //		return average + offset;
    //	}

    //	private float GenerateRotation()
    //	{
    //		if (randomRotation)
    //		{
    //			return (float)random.NextDouble() * 360f;
    //		}
    //		else
    //		{
    //			return 0;
    //		}
    //	}

    //	private static Vector3 GenerateRandomUnitVectorWithinCone(Vector3 coneDirection, float angle)
    //	{
    //		float cosAngle = (float)Math.Cos(angle);
    //		Random random = new Random();
    //		float theta = (float)((float)random.NextDouble() * 2f * Math.PI);
    //		float z = cosAngle + ((float)random.NextDouble() * (1 - cosAngle));
    //		float rootOneMinusZSquared = (float)Math.Sqrt(1 - z * z);
    //		float x = (float)(rootOneMinusZSquared * Math.Cos(theta));
    //		float y = (float)(rootOneMinusZSquared * Math.Sin(theta));

    //		Vector4 direction = new Vector4(x, y, z, 1);
    //		if (coneDirection.X != 0 || coneDirection.Y != 0 || (coneDirection.Z != 1 && coneDirection.Z != -1))
    //		{
    //			Vector3 rotateAxis = Vector3.Cross(coneDirection, new Vector3(0, 0, 1));
    //			rotateAxis = Vector3.Normalize(rotateAxis);
    //			float rotateAngle = (float)Math.Acos(Vector3.Dot(coneDirection, new Vector3(0, 0, 1)));
    //			Matrix4 rotationMatrix = Matrix4.CreateFromAxisAngle(rotateAxis, -rotateAngle);
    //			direction = Vector4.Transform(rotationMatrix, direction);
    //		}
    //		else if (coneDirection.Z == -1)
    //		{
    //			direction.Z *= -1;
    //		}
    //		return new Vector3(direction.Xyz);
    //	}

    //	private Vector3 GenerateRandomUnitVector()
    //	{
    //		float theta = (float)MathHelper.DegreesToRadians(random.NextDouble());
    //		float z = (float)(random.NextDouble() * 2) - 1.0f;
    //		float rootOneMinusZSquared = (float)Math.Sqrt(1 - z * z);
    //		float x = (float)(rootOneMinusZSquared * Math.Cos(theta));
    //		float y = (float)(rootOneMinusZSquared * Math.Sin(theta));
    //		return new Vector3(x, y, z);
    //	}

    //}
    #endregion

    public class ParticleSystem
    {

        private float pps;
        private float speed;
        private float gravityComplient;
        private float lifeLength;
        private ParticleTexture texture;
        private Random random = new Random();
        
        public ParticleSystem(float pps, float speed, float gravityComplient, float lifeLength, ParticleTexture texture)
        {
            this.pps = pps;
            this.speed = speed;
            this.gravityComplient = gravityComplient;
            this.lifeLength = lifeLength;
            this.texture = texture;
        }

        public void GenerateParticles(Vector3 systemCenter)
        {
            float delta = Time.delta / 1000.0f;
            float particlesToCreate = pps * delta;
            int count = (int)Math.Floor(particlesToCreate);
            float partialParticle = particlesToCreate % 1;
            for (int i = 0; i < count; i++)
            {
                EmitParticle(systemCenter);
            }
            if (random.NextDouble() < partialParticle)
            {
                EmitParticle(systemCenter);
            }
        }

        private void EmitParticle(Vector3 center)
        {
            float dirX = (float)random.NextDouble() * 2f - 1f;
            float dirZ = (float)random.NextDouble() * 2f - 1f;
            Vector3 velocity = new Vector3(dirX, 1, dirZ);
            velocity.Normalize();
            velocity *= speed;
            new Particle(new Vector3(center), velocity, gravityComplient, lifeLength, 0, 1, texture);
        }
    }   

    public class ParticleTexture
    {
        public int TextureHandle { get; private set; }
        public int NumberOfRows { get; private set; }
        public bool Additive { get; private set; } = false;

        public ParticleTexture(int textureHandle, int numberOfRows)
        {
            TextureHandle = textureHandle;
            NumberOfRows = numberOfRows;
        }
    }
}
