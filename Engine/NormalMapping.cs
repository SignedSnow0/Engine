using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Engine
{
	public class NormalMappingRenderer
	{

		private NormalMappingShader shader;

		public NormalMappingRenderer(Matrix4 projectionMatrix)
		{
			shader = new NormalMappingShader();
			shader.Start();
			shader.LoadProjectionMatrix(projectionMatrix);
			shader.ConnectTextureUnits();
			shader.Stop();
		}

		public void Render(Dictionary<TexturedModel, List<Entity>> entities, Vector4 clipPlane, List<Light> lights, Camera camera)
		{
			shader.Start();
			Prepare(clipPlane, lights, camera);
			foreach (TexturedModel model in entities.Keys)
			{
				PrepareTexturedModel(model);
				List<Entity> batch = new List<Entity>();
				entities.TryGetValue(model, out batch);
				foreach (Entity entity in batch)
				{
					PrepareInstance(entity);
					GL.DrawElements(BeginMode.Triangles, model.model.VertexCount, DrawElementsType.UnsignedInt, 0);
				}
				UnbindTexturedModel();
			}
			shader.Stop();
		}

		public void Delete()
		{
			shader.Delete();
		}

		private void PrepareTexturedModel(TexturedModel model)
		{
			RawModel rawModel = model.model;
			GL.BindVertexArray(rawModel.VaoHandle);
			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);
			GL.EnableVertexAttribArray(2);
			GL.EnableVertexAttribArray(3);
			ModelTexture texture = model.Texture;
			shader.LoadNumberOfRows(texture.NumberOfRows);
			if (texture.hasTransparency)
			{
				MasterRenderer.DisableCulling();
			}
			shader.LoadShineVariables(texture.shineDamper, texture.reflectivity);
			GL.ActiveTexture(TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2D, model.Texture.handle);
			GL.ActiveTexture(TextureUnit.Texture1);
			GL.BindTexture(TextureTarget.Texture2D, model.Texture.NormalMap);
		}

		private void UnbindTexturedModel()
		{
			MasterRenderer.EnableCulling();
			GL.DisableVertexAttribArray(0);
			GL.DisableVertexAttribArray(1);
			GL.DisableVertexAttribArray(2);
			GL.DisableVertexAttribArray(3);
			GL.BindVertexArray(0);
		}

		private void PrepareInstance(Entity entity)
		{
			Matrix4 transformationMatrix = Util.CreateTransformationMatrix(entity.Position, entity.rX, entity.rY, entity.rZ, entity.Scale);
			shader.LoadTransformationMatrix(transformationMatrix);
			shader.LoadOffset(entity.GetTextureOffsetX(), entity.GetTextureOffsetY());
		}

		private void Prepare(Vector4 clipPlane, List<Light> lights, Camera camera)
		{
			shader.LoadClipPlane(clipPlane);
			shader.LoadSkyColour(MasterRenderer.RED, MasterRenderer.GREEN, MasterRenderer.BLUE);
			Matrix4 viewMatrix = Util.CreateViewMatrix(camera);

			shader.LoadLights(lights, viewMatrix);
			shader.LoadViewMatrix(viewMatrix);
		}

	}
	public class NormalMappingShader : Shader
	{


		private const int MAX_LIGHTS = 4;

		private const string VERTEX_FILE = "NormalMapVertexShader.vert";
		private const string FRAGMENT_FILE = "NormalMapFragmentShader.frag";

		private int handleTransformationMatrix;
		private int handleProjectionMatrix;
		private int handleViewMatrix;
		private int[] handleLightPositionEyeSpace;
		private int[] handleLightColour;
		private int[] handleAttenuation;
		private int handleShineDamper;
		private int handleReflectivity;
		private int handleSkyColour;
		private int handleNumberOfRows;
		private int handleOffset;
		private int handlePlane;
		private int handleModelTexture;
		private int handleNormalMap;

		public NormalMappingShader() : base(VERTEX_FILE, FRAGMENT_FILE)
		{

		}

		protected override void BindAttributes()
		{
			BindAttribute(0, "position");
			BindAttribute(1, "textureCoordinates");
			BindAttribute(2, "normal");
			BindAttribute(3, "tangent");
		}
		protected override void GetAllUniformLocations()
		{
			handleTransformationMatrix = GetUniformLocation("transformationMatrix");
			handleProjectionMatrix = GetUniformLocation("projectionMatrix");
			handleViewMatrix = GetUniformLocation("viewMatrix");
			handleShineDamper = GetUniformLocation("shineDamper");
			handleReflectivity = GetUniformLocation("reflectivity");
			handleSkyColour = GetUniformLocation("skyColour");
			handleNumberOfRows = GetUniformLocation("numberOfRows");
			handleOffset = GetUniformLocation("offset");
			handlePlane = GetUniformLocation("plane");
			handleModelTexture = GetUniformLocation("modelTexture");
			handleNormalMap = GetUniformLocation("normalMap");

			handleLightPositionEyeSpace = new int[MAX_LIGHTS];
			handleLightColour = new int[MAX_LIGHTS];
			handleAttenuation = new int[MAX_LIGHTS];
			for (int i = 0; i < MAX_LIGHTS; i++)
			{
				handleLightPositionEyeSpace[i] = GetUniformLocation("lightPositionEyeSpace[" + i + "]");
				handleLightColour[i] = GetUniformLocation("lightColour[" + i + "]");
				handleAttenuation[i] = GetUniformLocation("attenuation[" + i + "]");
			}
		}

		public void ConnectTextureUnits()
		{
			LoadToUniform(handleModelTexture, 0);
			LoadToUniform(handleNormalMap, 1);
		}

		public void LoadClipPlane(Vector4 plane)
		{
			LoadToUniform(handlePlane, plane);
		}

		public void LoadNumberOfRows(int numberOfRows)
		{
			LoadToUniform(handleNumberOfRows, (float)numberOfRows);
		}

		public void LoadOffset(float x, float y)
		{
			LoadToUniform(handleOffset, new Vector2(x, y));
		}

		public void LoadSkyColour(float r, float g, float b)
		{
			LoadToUniform(handleSkyColour, new Vector3(r, g, b));
		}

		public void LoadShineVariables(float damper, float reflectivity)
		{
			LoadToUniform(handleShineDamper, damper);
			LoadToUniform(handleReflectivity, reflectivity);
		}

		public void LoadTransformationMatrix(Matrix4 matrix)
		{
			LoadToUniform(handleTransformationMatrix, matrix);
		}

		public void LoadLights(List<Light> lights, Matrix4 viewMatrix)
		{
			for (int i = 0; i < MAX_LIGHTS; i++)
			{
				if (i < lights.Count())
				{
					LoadToUniform(handleLightPositionEyeSpace[i], GetEyeSpacePosition(lights[i], viewMatrix));
					LoadToUniform(handleLightColour[i], lights[i].Color);
					LoadToUniform(handleAttenuation[i], lights[i].Attenuation);
				}
				else
				{
					LoadToUniform(handleLightPositionEyeSpace[i], new Vector3(0, 0, 0));
					LoadToUniform(handleLightColour[i], new Vector3(0, 0, 0));
					LoadToUniform(handleAttenuation[i], new Vector3(1, 0, 0));
				}
			}
		}

		public void LoadViewMatrix(Matrix4 viewMatrix)
		{
			LoadToUniform(handleViewMatrix, viewMatrix);
		}

		public void LoadProjectionMatrix(Matrix4 projection)
		{
			LoadToUniform(handleProjectionMatrix, projection);
		}

		private Vector3 GetEyeSpacePosition(Light light, Matrix4 viewMatrix)
		{
			Vector3 position = light.Position;
			Vector4 eyeSpacePos = new Vector4(position.X, position.Y, position.Z, 1.0f);
			eyeSpacePos = Vector4.Transform(viewMatrix, eyeSpacePos);
			return new Vector3(eyeSpacePos);

		}
	}

	public class NormalMappedObjLoader
	{


		public static RawModel LoadOBJ(string objFileName, Loader loader)
		{
			string line;
			List<VertexNM> vertices = new List<VertexNM>();
			List<Vector2> textures = new List<Vector2>();
			List<Vector3> normals = new List<Vector3>();
			List<int> indices = new List<int>();
			using (StreamReader sr = new StreamReader($"Models/{objFileName}"))
			{
				while (true)
				{
					line = sr.ReadLine();
					if (line.StartsWith("v "))
					{
						string[] currentLine = line.Split(' ');
						Vector3 vertex = new Vector3(float.Parse(currentLine[1], CultureInfo.InvariantCulture.NumberFormat),
													 float.Parse(currentLine[2], CultureInfo.InvariantCulture.NumberFormat),
													 float.Parse(currentLine[3], CultureInfo.InvariantCulture.NumberFormat));
						VertexNM newVertex = new VertexNM(vertices.Count, vertex);
						vertices.Add(newVertex);

					}
					else if (line.StartsWith("vt "))
					{
						string[] currentLine = line.Split(' ');
						Vector2 texture = new Vector2(float.Parse(currentLine[1], CultureInfo.InvariantCulture.NumberFormat),
													  float.Parse(currentLine[2], CultureInfo.InvariantCulture.NumberFormat));
						textures.Add(texture);
					}
					else if (line.StartsWith("vn "))
					{
						string[] currentLine = line.Split(' ');
						Vector3 normal = new Vector3(float.Parse(currentLine[1], CultureInfo.InvariantCulture.NumberFormat),
													 float.Parse(currentLine[2], CultureInfo.InvariantCulture.NumberFormat),
													 float.Parse(currentLine[3], CultureInfo.InvariantCulture.NumberFormat));
						normals.Add(normal);
					}
					else if (line.StartsWith("f "))
					{
						break;
					}
				}
				while (line != null && line.StartsWith("f "))
				{
					String[] currentLine = line.Split(' ');
					String[] vertex1 = currentLine[1].Split('/');
					String[] vertex2 = currentLine[2].Split('/');
					String[] vertex3 = currentLine[3].Split('/');
					VertexNM v0 = processVertex(vertex1, vertices, indices);
					VertexNM v1 = processVertex(vertex2, vertices, indices);
					VertexNM v2 = processVertex(vertex3, vertices, indices);
					calculateTangents(v0, v1, v2, textures);
					line = sr.ReadLine();
				}
			}

			RemoveUnusedVertices(vertices);
			float[] verticesArray = new float[vertices.Count * 3];
			float[] texturesArray = new float[vertices.Count * 2];
			float[] normalsArray = new float[vertices.Count * 3];
			float[] tangentsArray = new float[vertices.Count * 3];
			float furthest = ConvertDataToArrays(vertices, textures, normals, verticesArray, texturesArray, normalsArray, tangentsArray);
			uint[] indicesArray = ConvertIndicesListToArray(indices);

			return loader.LoadToVao(verticesArray, texturesArray, normalsArray, tangentsArray, indicesArray);
		}

		//NEW 
		private static void calculateTangents(VertexNM v0, VertexNM v1, VertexNM v2, List<Vector2> textures)
		{
			Vector3 delatPos1 = v1.Position - v0.Position;
			Vector3 delatPos2 = v2.Position - v0.Position;
			Vector2 uv0 = textures[v0.TextureIndex];
			Vector2 uv1 = textures[v1.TextureIndex];
			Vector2 uv2 = textures[v2.TextureIndex];
			Vector2 deltaUv1 = uv1 - uv0;
			Vector2 deltaUv2 = uv2 - uv0;

			float r = 1.0f / (deltaUv1.X * deltaUv2.Y - deltaUv1.Y * deltaUv2.X);
			delatPos1 *= deltaUv2.Y;
			delatPos2 *= deltaUv1.Y;
			Vector3 tangent = delatPos1 - delatPos2;
			tangent *= r;
			v0.AddTangent(tangent);
			v1.AddTangent(tangent);
			v2.AddTangent(tangent);
		}

		private static VertexNM processVertex(string[] vertex, List<VertexNM> vertices, List<int> indices)
		{
			int.TryParse(vertex[0], out int index);
			index--;
			VertexNM currentVertex = vertices[index];
			int.TryParse(vertex[1], out int textureIndex);
			textureIndex--;
			int.TryParse(vertex[2], out int normalIndex);
			normalIndex--;
			if (!currentVertex.IsSet())
			{
				currentVertex.TextureIndex = textureIndex;
				currentVertex.NormalIndex = normalIndex;
				indices.Add(index);
				return currentVertex;
			}
			else
			{
				return DealWithAlreadyProcessedVertex(currentVertex, textureIndex, normalIndex, indices,
						vertices);
			}
		}

		private static uint[] ConvertIndicesListToArray(List<int> indices)
		{
			uint[] indicesArray = new uint[indices.Count];
			for (int i = 0; i < indicesArray.Length; i++)
			{
				indicesArray[i] = (uint)indices[i];
			}
			return indicesArray;
		}

		private static float ConvertDataToArrays(List<VertexNM> vertices, List<Vector2> textures, List<Vector3> normals, float[] verticesArray, float[] texturesArray, float[] normalsArray, float[] tangentsArray)
		{
			float furthestPoint = 0;
			for (int i = 0; i < vertices.Count; i++)
			{
				VertexNM currentVertex = vertices[i];
				if ((currentVertex.Length) > furthestPoint)
				{
					furthestPoint = currentVertex.Length;
				}
				Vector3 position = currentVertex.Position;
				Vector2 textureCoord = textures[currentVertex.TextureIndex];
				Vector3 normalVector = normals[currentVertex.NormalIndex];
				Vector3 tangent = currentVertex.AveragedTangent;
				verticesArray[i * 3] = position.X;
				verticesArray[i * 3 + 1] = position.Y;
				verticesArray[i * 3 + 2] = position.Z;
				texturesArray[i * 2] = textureCoord.X;
				texturesArray[i * 2 + 1] = 1 - textureCoord.Y;
				normalsArray[i * 3] = normalVector.X;
				normalsArray[i * 3 + 1] = normalVector.Y;
				normalsArray[i * 3 + 2] = normalVector.Z;
				tangentsArray[i * 3] = tangent.X;
				tangentsArray[i * 3 + 1] = tangent.Y;
				tangentsArray[i * 3 + 2] = tangent.Z;

			}
			return furthestPoint;
		}

		private static VertexNM DealWithAlreadyProcessedVertex(VertexNM previousVertex, int newTextureIndex,
				int newNormalIndex, List<int> indices, List<VertexNM> vertices)
		{
			if (previousVertex.HasSameTextureAndNormal(newTextureIndex, newNormalIndex))
			{
				indices.Add(previousVertex.Index);
				return previousVertex;
			}
			else
			{
				VertexNM anotherVertex = previousVertex.DuplicateVertex;
				if (anotherVertex != null)
				{
					return DealWithAlreadyProcessedVertex(anotherVertex, newTextureIndex,
							newNormalIndex, indices, vertices);
				}
				else
				{
					VertexNM duplicateVertex = previousVertex.Duplicate(vertices.Count);
					duplicateVertex.TextureIndex = newTextureIndex;
					duplicateVertex.NormalIndex = newNormalIndex;
					previousVertex.DuplicateVertex = duplicateVertex;
					vertices.Add(duplicateVertex);
					indices.Add(duplicateVertex.Index);
					return duplicateVertex;
				}
			}
		}

		private static void RemoveUnusedVertices(List<VertexNM> vertices)
		{
			foreach (VertexNM vertex in vertices)
			{
				vertex.AverageTangents();
				if (!vertex.IsSet())
				{
					vertex.TextureIndex = 0;
					vertex.NormalIndex = 0;
				}
			}
		}

	}


	public class ModelDataNM
	{

		private float[] vertices;
		private float[] textureCoords;
		private float[] normals;
		private float[] tangents;
		private int[] indices;
		private float furthestPoint;

		public ModelDataNM(float[] vertices, float[] textureCoords, float[] normals, float[] tangents, int[] indices,
				float furthestPoint)
		{
			this.vertices = vertices;
			this.textureCoords = textureCoords;
			this.normals = normals;
			this.indices = indices;
			this.furthestPoint = furthestPoint;
			this.tangents = tangents;
		}

		public float[] getVertices()
		{
			return vertices;
		}

		public float[] getTextureCoords()
		{
			return textureCoords;
		}

		public float[] getTangents()
		{
			return tangents;
		}

		public float[] getNormals()
		{
			return normals;
		}

		public int[] getIndices()
		{
			return indices;
		}

		public float getFurthestPoint()
		{
			return furthestPoint;
		}

	}

	public class VertexNM
	{
		public int TextureIndex { get; set; } = NO_INDEX;
		public int NormalIndex { get; set; } = NO_INDEX;
		public VertexNM DuplicateVertex { get; set; } = null;
		public Vector3 Position { get; private set; }
		public int Index { get; private set; }
		public float Length { get; private set; }
		public Vector3 AveragedTangent { get; private set; } = new Vector3(0, 0, 0);

		private const int NO_INDEX = -1;
		private List<Vector3> tangents = new List<Vector3>();

		public VertexNM(int index, Vector3 position)
		{
			Index = index;
			Position = position;
			Length = position.Length;
		}
		public void AddTangent(Vector3 tangent)
		{
			tangents.Add(tangent);
		}
		public VertexNM Duplicate(int newIndex)
		{
			VertexNM vertex = new VertexNM(newIndex, Position);
			vertex.tangents = tangents;
			return vertex;
		}
		public void AverageTangents()
		{
			if (tangents.Count == 0)
			{
				return;
			}
			foreach(Vector3 tangent in tangents)
			{
				AveragedTangent += tangent;
			}
			AveragedTangent.Normalize();
		}
		public bool IsSet()
		{
			return TextureIndex != NO_INDEX && NormalIndex != NO_INDEX;
		}
		public bool HasSameTextureAndNormal(int textureIndexOther, int normalIndexOther)
		{
			return textureIndexOther == TextureIndex && normalIndexOther == NormalIndex;
		}
	}
}