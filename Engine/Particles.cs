using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace Engine
{
    public class Particle
	{
		public Vector3 Position { get; private set; }
		public float Rotation { get; private set; }
		public float Scale { get; private set; }
        public ParticleTexture Texture { get; private set; }

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
		public bool Update()
		{
			velocity.Y += Player.GRAVITY * gravityEffect * Time.delta / 1000.0f;
			Vector3 change = velocity;
			change *= Time.delta / 1000.0f;
			Position += change;
			elapsedTime += Time.delta / 1000.0f;			
			return elapsedTime < lifeLength;
		}
	}

	public class ParticleRenderer
	{

		private float[] VERTICES = { -0.5f, 0.5f, -0.5f, -0.5f, 0.5f, 0.5f, 0.5f, -0.5f };

		private RawModel quad;
		private ParticleShader shader;

		public ParticleRenderer(Loader loader, Matrix4 projectionMatrix)
		{
			quad = loader.LoadToVao(VERTICES, 2);
			shader = new ParticleShader();
			shader.Start();
			shader.LoadProjectionMatrix(projectionMatrix);
			shader.Stop();
		}

		public void Render(Dictionary<ParticleTexture ,List<Particle>> particles, Camera camera)
		{
			Matrix4 viewmatrix = Util.CreateViewMatrix(camera);
			Prepare();
            foreach(ParticleTexture texture in particles.Keys)
            {
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, texture.TextureHandle);
                foreach (Particle particle in particles[texture])
                {
                    UpdateModelViewMatrix(particle.Position, particle.Rotation, particle.Scale, viewmatrix);
                    GL.DrawArrays(PrimitiveType.TriangleStrip, 0, quad.VertexCount);
                }
            }

			FinishRendering();
		}

		public void Delete()
		{
			shader.Delete();
		}

		private void UpdateModelViewMatrix(Vector3 position, float rotation, float scale, Matrix4 viewMatrix)
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
			shader.LoadModelViewMatrix(modelViewMatrix);
		}

		private void Prepare()
		{
			shader.Start();
			GL.BindVertexArray(quad.VaoHandle);
			GL.EnableVertexAttribArray(0);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.DepthMask(false);
		}

		private void FinishRendering()
		{
			GL.DepthMask(true);
			GL.Disable(EnableCap.Blend);
			GL.DisableVertexAttribArray(0);
			GL.BindVertexArray(0);
			shader.Stop();
		}
	}

	public class ParticleShader : Shader
	{

		private const string VERTEX_FILE = "ParticleVertexShader.vert";
		private const string FRAGMENT_FILE = "ParticleFragmentShader.frag";

		private int location_modelViewMatrix;
		private int location_projectionMatrix;

		public ParticleShader() : base(VERTEX_FILE, FRAGMENT_FILE)
		{

		}

		protected override void GetAllUniformLocations()
		{
			location_modelViewMatrix = GetUniformLocation("modelViewMatrix");
			location_projectionMatrix = GetUniformLocation("projectionMatrix");
		}

		protected override void BindAttributes()
		{
			BindAttribute(0, "position");
		}

		public void LoadModelViewMatrix(Matrix4 modelView)
		{
			LoadToUniform(location_modelViewMatrix, modelView);
		}

		public void LoadProjectionMatrix(Matrix4 projectionMatrix)
		{
			LoadToUniform(location_projectionMatrix, projectionMatrix);
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
		public static void Update()
        {
            foreach(List<Particle> particlesList in particles.Values)
            {
                particlesList.RemoveAll(p => !p.Update());
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

        public ParticleTexture(int textureHandle, int numberOfRows)
        {
            TextureHandle = textureHandle;
            NumberOfRows = numberOfRows;
        }
    }
}
