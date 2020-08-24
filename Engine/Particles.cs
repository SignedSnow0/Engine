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

		private Vector3 velocity;
		private float gravityEffect;
		private float lifeLength;
		private float elapsedTime = 0;

		public Particle(Vector3 position, Vector3 velocity, float gravityEffect, float lifeLength, float rotation, float scale)
		{
			Position = position;
			this.velocity = velocity;
			this.gravityEffect = gravityEffect;
			this.lifeLength = lifeLength;
			Rotation = rotation;
			Scale = scale;
			ParticleMaster.AddParticle(this);
		}

		public bool Update()
		{
			velocity.Y += Player.GRAVITY * gravityEffect * Time.delta / 1000.0f;
			Vector3 change = new Vector3(velocity);
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

		public void Render(List<Particle> particles, Camera camera)
		{
			Matrix4 viewmatrix = Util.CreateViewMatrix(camera);
			Prepare();
			foreach (Particle particle in particles)
			{
				UpdateModelViewMatrix(particle.Position, particle.Rotation, particle.Scale, viewmatrix);
				GL.DrawArrays(PrimitiveType.TriangleStrip, 0, quad.VertexCount);
			}
			FinishRendering();
		}

		public void Delete()
		{
			shader.Delete();
		}

		private void UpdateModelViewMatrix(Vector3 position, float rotation, float scale, Matrix4 viewMatrix)
		{
			Matrix4 modelMatrix = new Matrix4();
			modelMatrix = Matrix4.CreateTranslation(position);
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
		private static List<Particle> particles = new List<Particle>();
		private static ParticleRenderer renderer;

		public static void Init(Loader loader, Matrix4 projectionMatrix)
        {
			renderer = new ParticleRenderer(loader, projectionMatrix);
        }

		public static void Update()
        {
			particles.RemoveAll(p => !p.Update());

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
			Console.WriteLine($"{particles.Count}");
        }
		public static void AddParticle(Particle particle)
        {
			particles.Add(particle);
        }
		public static void CleanUp()
        {
			renderer.Delete();
        }
	}

	public class ParticleSystem
	{
		private float pps, averageSpeed, gravityComplient, averageLifeLength, averageScale;
		private float speedError, lifeError, scaleError = 0;
		private float directionDeviation = 0;
		private bool randomRotation = false;
		private Random random = new Random();
		private Vector3 direction;

		public ParticleSystem(float pps, float averageSpeed, float gravityComplient, float averageLifeLength, float averageScale)
		{
			this.pps = pps;
			this.averageSpeed = averageSpeed;
			this.gravityComplient = gravityComplient;
			this.averageLifeLength = averageLifeLength;
			this.averageScale = averageScale;
		}

		/// <summary>
		/// Imposta la direzione media delle particelle
		/// </summary>
		/// <param name="direction">La direzione media in cui le particelle si muovono</param>
		/// <param name="deviation">Valore tra 0 e 1 che indica di quanto le particelle possono deviare</param>
		public void SetDirection(Vector3 direction, float deviation)
		{
			this.direction = new Vector3(direction);
			directionDeviation = (float)(deviation * Math.PI);
		}
		/// <summary>
		/// Imposta il valore di rotazione randomica a vero;
		/// </summary>
		public void RandomizeRotation()
		{
			randomRotation = true;
		}
		/// <summary>
		/// Imposta la possibile differenza di velocità tra particelle
		/// </summary>
		/// <param name="error">Valore tra 0 e 1 che indica il margine di errore di una particella</param>
		public void SetSpeedError(float error)
		{
			speedError = error * averageSpeed;
		}
		/// <summary>
		/// Imposta la possibile differenza di vita tra particelle
		/// </summary>
		/// <param name="error">Valore tra 0 e 1 che indica il margine di errore di una particella</param>
		public void SetLifeError(float error)
		{
			lifeError = error * averageLifeLength;
		}
		/// <summary>
		/// Imposta la possibile differenza di dimensione tra particelle
		/// </summary>
		/// <param name="error">Valore tra 0 e 1 che indica il margine di errore di una particella</param>
		public void SetScaleError(float error)
		{
			scaleError = error * averageScale;
		}
		/// <summary>
		/// Genera delle particelle in base al punto di origine
		/// </summary>
		/// <param name="systemCenter">Coordinate nel mondo del punto di origine</param>
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
			if (random.Next(1) < partialParticle)
			{
				EmitParticle(systemCenter);
			}
		}

		private void EmitParticle(Vector3 center)
		{
			Vector3 velocity;
			if (direction != Vector3.Zero)
			{
				velocity = GenerateRandomUnitVectorWithinCone(direction, directionDeviation);
			}
			else
			{
				velocity = GenerateRandomUnitVector();
			}
			velocity.Normalize();
			velocity *= GenerateValue(averageSpeed, speedError);
			float scale = GenerateValue(averageScale, scaleError);
			float lifeLength = GenerateValue(averageLifeLength, lifeError);
			new Particle(center, velocity, gravityComplient, lifeLength, GenerateRotation(), scale);
		}
		private float GenerateValue(float average, float errorMargin)
		{
			float offset = ((float)random.NextDouble() - 0.5f) * 2f * errorMargin;
			return average + offset;
		}
		private float GenerateRotation()
		{
			if (randomRotation)
			{
				return (float)random.NextDouble() * 360f;
			}
			else
			{
				return 0;
			}
		}
		private static Vector3 GenerateRandomUnitVectorWithinCone(Vector3 coneDirection, float angle)
		{
			float cosAngle = (float)Math.Cos(angle);
			Random random = new Random();
			float theta = (float)((float)random.NextDouble() * 2.0f * Math.PI);
			float z = cosAngle + ((float)random.NextDouble() * (1 - cosAngle));
			float rootOneMinusZSquared = (float)Math.Sqrt(1 - z * z);
			float x = (float)(rootOneMinusZSquared * Math.Cos(theta));
			float y = (float)(rootOneMinusZSquared * Math.Sin(theta));

			Vector4 direction = new Vector4(x, y, z, 1);
			if (coneDirection.X != 0 || coneDirection.Y != 0 || (coneDirection.Z != 1 && coneDirection.Z != -1))
			{
				Vector3 rotateAxis = Vector3.Cross(coneDirection, new Vector3(0, 0, 1));
				rotateAxis.Normalize();
				float rotateAngle = (float)Math.Acos(Vector3.Dot(coneDirection, new Vector3(0, 0, 1)));
				Matrix4 rotation = Matrix4.CreateFromAxisAngle(rotateAxis, -rotateAngle);				
				Matrix4 transform = Matrix4.CreateTranslation(direction.Xyz);
				Matrix4 matrix = rotation * transform;

				direction = Vector4.Transform(matrix, direction);
			}
			else if (coneDirection.Z == -1)
			{
				direction.Z *= -1;
			}
			return new Vector3(direction);
		}
		private Vector3 GenerateRandomUnitVector()
		{
			float theta = (float)((float)random.NextDouble() * 2.0f * Math.PI);
			float z = ((float)random.NextDouble() * 2.0f) - 1.0f;
			float rootOneMinusZSquared = (float)Math.Sqrt(1.0f - z * z);
			float x = (float)(rootOneMinusZSquared * Math.Cos(theta));
			float y = (float)(rootOneMinusZSquared * Math.Sin(theta));
			return new Vector3(x, y, z);
		}
	}
}
