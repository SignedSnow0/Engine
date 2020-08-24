using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Engine
{
	public class FontType
	{
		/// <summary>
		/// Handle della texture
		/// </summary>
		public int HandleTextureAtlas { get; private set; }
		private TextMeshCreator loader;

		public FontType(int textureAtlas, string fontFile)
		{
			HandleTextureAtlas = textureAtlas;
			loader = new TextMeshCreator(fontFile);
		}

		public TextMeshData LoadText(GUIText text)
		{
			return loader.CreateTextMesh(text);
		}

	}

	public class GUIText
	{
		/// <summary>
		/// Testo 
		/// </summary>
		public string TextString { get; private set; }
		/// <summary>
		/// Dimensione del testo
		/// </summary>
		public float FontSize { get; private set; }
		/// <summary>
		/// Handle del mesh
		/// </summary>
		public int TextMeshVao { get; private set; }
		/// <summary>
		/// Numero di vertici del mesh
		/// </summary>
		public int VertexCount { get; private set; }
		/// <summary>
		/// Colore del testo
		/// </summary>
		public Vector3 Color { get; set; } = new Vector3(0f, 0f, 0f);
		/// <summary>
		/// Posizione dello schermo dell`angolo alto sinistra
		/// </summary>
		public Vector2 Position { get; private set; }
		/// <summary>
		/// Lunghezza massima della linea rispetto lo schermo
		/// </summary>
		public float LineMaxSize { get; private set; }
		/// <summary>
		/// Numero di linee
		/// </summary>
		public int NumberOfLines { get; set; }
		/// <summary>
		/// Font del testo
		/// </summary>
		public FontType Font { get; private set; }
		/// <summary>
		/// Indica se il testo è centrato o meno
		/// </summary>
		public bool CenterText { get; private set; } = false;

		public GUIText(string text, float fontSize, FontType font, Vector2 position, float maxLineLength, bool centered)
		{
			TextString = text;
			FontSize = fontSize;
			Font = font;
			Position = position;
			LineMaxSize = maxLineLength;
			CenterText = centered;
			TextMaster.LoadText(this);
		}

		public void Remove()
		{
			TextMaster.RemoveText(this);
		}

		public void SetMeshInfo(int vao, int verticesCount)
		{
			TextMeshVao = vao;
			VertexCount = verticesCount;
		}
	}

	public class Line
	{
		public double MaxLength { get; private set; }
		public List<Word> words { get; private set; } = new List<Word>();
		public double LineLength { get; private set; } = 0;

		private double spaceSize;

		public Line(double spaceWidth, double fontSize, double maxLength)
		{
			this.spaceSize = spaceWidth * fontSize;
			this.MaxLength = maxLength;
		}

		public bool AttemptToAddWord(Word word)
		{
			double additionalLength = word.width;
			////////////////////
			additionalLength += words.Count == 0 ? 0 : spaceSize;
			if (LineLength + additionalLength <= MaxLength)
			{
				words.Add(word);
				LineLength += additionalLength;
				return true;
			}
			else
			{
				return false;
			}
		}
	}

	public class Word
	{

		public List<Character> characters { get; private set; } = new List<Character>();
		public double width { get; private set; } = 0;

		private double fontSize;

		public Word(double fontSize)
		{
			this.fontSize = fontSize;
		}

		public void AddCharacter(Character character)
		{
			characters.Add(character);
			width += character.XAdvance * fontSize;
		}
	}

	public class Character
	{
		/// <summary>
		/// Valore ascii del carattere
		/// </summary>
		public int Id { get; private set; }
		/// <summary>
		/// Coordinata x dell`anglo alto sinistra del carattere nell textureAtlas
		/// </summary>
		public double XTextureCoord { get; private set; }
		/// <summary>
		/// Coordinata y dell`anglo alto sinistra del carattere nell textureAtlas
		/// </summary>
		public double YTextureCoord { get; private set; }
		/// <summary>
		/// Dimensione x del carattere nell textureAtlas
		/// </summary>
		public double XMaxTextureCoord { get; private set; }
		/// <summary>
		/// Dimensione y del carattere nell textureAtlas
		/// </summary>
		public double YMaxTextureCoord { get; private set; }
		/// <summary>
		/// Distanza che il cursore deve percorrere prima di renderizzare il carattere
		/// </summary>
		public double XOffset { get; private set; }
		/// <summary>
		/// Distanza che il cursore deve percorrere prima di renderizzare il carattere
		/// </summary>
		public double YOffset { get; private set; }
		/// <summary>
		/// Dimensione del carattere
		/// </summary>
		public double SizeX { get; private set; }
		/// <summary>
		/// Dimensione del carattere
		/// </summary>
		public double SizeY { get; private set; }
		/// <summary>
		/// Distanza che il cursore deve percorrere dopo aver renderizzato il carattere
		/// </summary>
		public double XAdvance { get; private set; }

		public Character(int id, double xTextureCoord, double yTextureCoord, double xTexSize, double yTexSize, double xOffset, double yOffset, double sizeX, double sizeY, double xAdvance)
		{
			Id = id;
			XTextureCoord = xTextureCoord;
			YTextureCoord = yTextureCoord;
			XOffset = xOffset;
			YOffset = yOffset;
			SizeX = sizeX;
			SizeY = sizeY;
			XMaxTextureCoord = xTexSize + xTextureCoord;
			YMaxTextureCoord = yTexSize + yTextureCoord;
			XAdvance = xAdvance;
		}
	}

	public class MetaFile
	{
		private int lineCount = 0;
		private const int PAD_TOP = 0;
		private const int PAD_LEFT = 1;
		private const int PAD_BOTTOM = 2;
		private const int PAD_RIGHT = 3;

		private const int DESIRED_PADDING = 8;

		private const char SPLITTER = ' ';
		private const char NUMBER_SEPARATOR = ',';

		private double aspectRatio;

		private double verticalPerPixelSize;
		private double horizontalPerPixelSize;
		public double SpaceWidth { get; private set; }
		private int[] padding;
		private int paddingWidth;
		private int paddingHeight;

		private Dictionary<int, Character> metaData = new Dictionary<int, Character>();
		private Dictionary<string, string> values = new Dictionary<string, string>();
		private string fileName;

		public MetaFile(string fileName)
		{
			this.fileName = fileName;
			aspectRatio = TextMaster.windowWidth / TextMaster.windowHeight;
			LoadPaddingData();
			LoadLineSizes();
			int imageWidth = GetValueOfVariable("scaleW");
			LoadCharacterData(imageWidth);
		}

		public Character GetCharacter(int ascii)
		{
			Character character;
			if (metaData.TryGetValue(ascii, out character))
			{
				return character;
			}
			else
			{
				throw new Exception($"Il carattere con valore ascii:{ascii} non è stato trovato");
			}
		}

		private bool ProcessNextLine()
		{
			values.Clear();
			string line = null;
			lineCount++;
			using (StreamReader sr = new StreamReader($"Fonts/{fileName}"))
			{

                for (int i = 0; i < lineCount; i++)
                {
					line = sr.ReadLine();
				}
				if (line == null)
				{
					return false;
				}
				foreach (string part in line.Split(SPLITTER))
				{
					string[] valuePairs = part.Split('=');
					if (valuePairs.Length == 2)
					{
						values.Add(valuePairs[0], valuePairs[1]);
					}
				}
				return true;
			}
		}

		private int GetValueOfVariable(string variable)
		{
			int valore = int.Parse(values[variable]);

			return valore;
		}

		private int[] GetValuesOfVariable(string variable)
		{
			string stringa = values[variable];
			string[] numbers = stringa.Split(NUMBER_SEPARATOR);
			int[] actualValues = new int[numbers.Length];
			for (int i = 0; i < actualValues.Length; i++)
			{
				int.TryParse(numbers[i], out actualValues[i]);
			}
			return actualValues;
		}

		private void LoadPaddingData()
		{
			ProcessNextLine();
			padding = GetValuesOfVariable("padding");
			paddingWidth = padding[PAD_LEFT] + padding[PAD_RIGHT];
			paddingHeight = padding[PAD_TOP] + padding[PAD_BOTTOM];
		}

		private void LoadLineSizes()
		{
			ProcessNextLine();
			int lineHeightPixels = GetValueOfVariable("lineHeight") - paddingHeight;
			verticalPerPixelSize = TextMeshCreator.LINE_HEIGHT / lineHeightPixels;
			horizontalPerPixelSize = verticalPerPixelSize / aspectRatio;
		}

		private void LoadCharacterData(int imageWidth)
		{
            ProcessNextLine();
            ProcessNextLine();
			while (ProcessNextLine())
			{
				Character c = LoadCharacter(imageWidth);
				if (c != null)
				{
					if (!metaData.ContainsKey(c.Id))
					{
						metaData.Add(c.Id, c);
					}
				}
			}
		}

		private Character LoadCharacter(int imageSize)
		{
			int id = GetValueOfVariable("id");
			if (id == TextMeshCreator.SPACE_ASCII)
			{
				SpaceWidth = (GetValueOfVariable("xadvance") - paddingWidth) * horizontalPerPixelSize;
				return null;
			}
			double xTex = ((double)GetValueOfVariable("x") + (padding[PAD_LEFT] - DESIRED_PADDING)) / imageSize;
			double yTex = ((double)GetValueOfVariable("y") + (padding[PAD_TOP] - DESIRED_PADDING)) / imageSize;
			int width = GetValueOfVariable("width") - (paddingWidth - (2 * DESIRED_PADDING));
			int height = GetValueOfVariable("height") - ((paddingHeight) - (2 * DESIRED_PADDING));
			double quadWidth = width * horizontalPerPixelSize;
			double quadHeight = height * verticalPerPixelSize;
			double xTexSize = (double)width / imageSize;
			double yTexSize = (double)height / imageSize;
			double xOff = (GetValueOfVariable("xoffset") + padding[PAD_LEFT] - DESIRED_PADDING) * horizontalPerPixelSize;
			double yOff = (GetValueOfVariable("yoffset") + (padding[PAD_TOP] - DESIRED_PADDING)) * verticalPerPixelSize;
			double xAdvance = (GetValueOfVariable("xadvance") - paddingWidth) * horizontalPerPixelSize;
			return new Character(id, xTex, yTex, xTexSize, yTexSize, xOff, yOff, quadWidth, quadHeight, xAdvance);
		}
	}

	public class TextMeshCreator
	{

		public const double LINE_HEIGHT = 0.03f;
		public const int SPACE_ASCII = 32;

		private MetaFile metaData;

		public TextMeshCreator(string filename)
		{
			metaData = new MetaFile(filename);
		}

		public TextMeshData CreateTextMesh(GUIText text)
		{
			List<Line> lines = CreateStructure(text);
			TextMeshData data = CreateQuadVertices(text, lines);
			return data;
		}

		private List<Line> CreateStructure(GUIText text)
		{
			char[] chars = text.TextString.ToCharArray();
			List<Line> lines = new List<Line>();
			Line currentLine = new Line(metaData.SpaceWidth, text.FontSize, text.LineMaxSize);
			Word currentWord = new Word(text.FontSize);
			foreach (char c in chars)
			{
				string s = c.ToString();
				byte[] values = Encoding.ASCII.GetBytes(s);
				int ascii = values[0];
				if (ascii == SPACE_ASCII)
				{
					bool added = currentLine.AttemptToAddWord(currentWord);
					if (!added)
					{
						lines.Add(currentLine);
						currentLine = new Line(metaData.SpaceWidth, text.FontSize, text.LineMaxSize);
						currentLine.AttemptToAddWord(currentWord);
					}
					currentWord = new Word(text.FontSize);
					continue;
				}
				Character character = metaData.GetCharacter(ascii);
				currentWord.AddCharacter(character);
			}
			CompleteStructure(lines, currentLine, currentWord, text);
			return lines;
		}

		private void CompleteStructure(List<Line> lines, Line currentLine, Word currentWord, GUIText text)
		{
			bool added = currentLine.AttemptToAddWord(currentWord);
			if (!added)
			{
				lines.Add(currentLine);
				currentLine = new Line(metaData.SpaceWidth, text.FontSize, text.LineMaxSize);
				currentLine.AttemptToAddWord(currentWord);
			}
			lines.Add(currentLine);
		}

		private TextMeshData CreateQuadVertices(GUIText text, List<Line> lines)
		{
			text.NumberOfLines = lines.Count;
			double curserX = 0f;
			double curserY = 0f;
			List<float> vertices = new List<float>();
			List<float> textureCoords = new List<float>();
			foreach (Line line in lines)
			{
				if (text.CenterText)
				{
					curserX = (line.MaxLength - line.LineLength) / 2;
				}
				foreach (Word word in line.words)
				{
					foreach (Character letter in word.characters)
					{
						AddVerticesForCharacter(curserX, curserY, letter, text.FontSize, vertices);
						AddTexCoords(textureCoords, letter.XTextureCoord, letter.YTextureCoord, letter.XMaxTextureCoord, letter.YMaxTextureCoord);
						curserX += letter.XAdvance * text.FontSize;
					}
					curserX += metaData.SpaceWidth * text.FontSize;
				}
				curserX = 0;
				curserY += LINE_HEIGHT * text.FontSize;
			}
			return new TextMeshData(ListToArray(vertices), ListToArray(textureCoords));
		}

		private void AddVerticesForCharacter(double curserX, double curserY, Character character, double fontSize, List<float> vertices)
		{
			double x = curserX + (character.XOffset * fontSize);
			double y = curserY + (character.YOffset * fontSize);
			double maxX = x + (character.SizeX * fontSize);
			double maxY = y + (character.SizeY * fontSize);
			double properX = (2 * x) - 1;
			double properY = (-2 * y) + 1;
			double properMaxX = (2 * maxX) - 1;
			double properMaxY = (-2 * maxY) + 1;
			AddVertices(vertices, properX, properY, properMaxX, properMaxY);
		}

		private static void AddVertices(List<float> vertices, double x, double y, double maxX, double maxY)
		{
			vertices.Add((float)x);
			vertices.Add((float)y);
			vertices.Add((float)x);
			vertices.Add((float)maxY);
			vertices.Add((float)maxX);
			vertices.Add((float)maxY);
			vertices.Add((float)maxX);
			vertices.Add((float)maxY);
			vertices.Add((float)maxX);
			vertices.Add((float)y);
			vertices.Add((float)x);
			vertices.Add((float)y);
		}

		private static void AddTexCoords(List<float> texCoords, double x, double y, double maxX, double maxY)
		{
			texCoords.Add((float)x);
			texCoords.Add((float)y);
			texCoords.Add((float)x);
			texCoords.Add((float)maxY);
			texCoords.Add((float)maxX);
			texCoords.Add((float)maxY);
			texCoords.Add((float)maxX);
			texCoords.Add((float)maxY);
			texCoords.Add((float)maxX);
			texCoords.Add((float)y);
			texCoords.Add((float)x);
			texCoords.Add((float)y);
		}


		private static float[] ListToArray(List<float> listOfFloats)
		{
			float[] array = new float[listOfFloats.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = listOfFloats[i];
			}
			return array;
		}
	}

	public class TextMeshData
	{

		public float[] VertexPositions { get; private set; }
		public float[] TextureCoords { get; private set; }

		public TextMeshData(float[] vertexPositions, float[] textureCoords)
		{
			VertexPositions = vertexPositions;
			TextureCoords = textureCoords;
		}

		public int GetVertexCount()
		{
			return VertexPositions.Length / 2;
		}

	}

	public class FontRenderer
	{

		private FontShader shader;

		public FontRenderer()
		{
			shader = new FontShader();
		}

		public void Render(Dictionary<FontType, List<GUIText>> texts)
        {
			Prepare();
            foreach (FontType font in texts.Keys)
            {
				GL.ActiveTexture(TextureUnit.Texture0);
				GL.BindTexture(TextureTarget.Texture2D, font.HandleTextureAtlas);
				foreach(GUIText text in texts[font])
                {
					RenderText(text);
                }
            }
			EndRendering();
        }

		public void Delete()
		{
			shader.Delete();
		}

		private void Prepare()
		{
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
			GL.Disable(EnableCap.DepthTest);

			shader.Start();
		}

		private void RenderText(GUIText text)
		{
			GL.BindVertexArray(text.TextMeshVao);
			GL.EnableVertexAttribArray(0);
			GL.EnableVertexAttribArray(1);

			shader.LoadColor(text.Color);
			shader.LoadTranslation(text.Position);
			GL.DrawArrays(PrimitiveType.Triangles, 0, text.VertexCount);

			GL.DisableVertexAttribArray(0);
			GL.DisableVertexAttribArray(1);
			GL.BindVertexArray(0);
		}

		private void EndRendering()
		{
			shader.Stop();
			GL.Disable(EnableCap.Blend);
			GL.Enable(EnableCap.DepthTest);
		}
	}

	public class FontShader : Shader
	{

		private const string VERTEX_FILE = "FontVertexShader.vert";
		private const string FRAGMENT_FILE = "FontFragmentShader.frag";

		private int handleColor;
		private int handleTranslation;

		public FontShader() : base(VERTEX_FILE, FRAGMENT_FILE)
		{

		}

		protected override void GetAllUniformLocations()
		{
			handleColor = GetUniformLocation("color");
			handleTranslation = GetUniformLocation("translation");
		}


		protected override void BindAttributes()
		{
			BindAttribute(0, "position");
			BindAttribute(1, "textureCoords");
		}

		public void LoadColor(Vector3 color)
        {
			LoadToUniform(handleColor, color);
        }

		public void LoadTranslation(Vector2 translation)
        {
			LoadToUniform(handleTranslation, translation);
        }
	}

	public static class TextMaster
	{
		public static Loader Loader;
		public static Dictionary<FontType, List<GUIText>> Texts = new Dictionary<FontType, List<GUIText>>();
		public static FontRenderer Renderer;
		public static int windowWidth;
		public static int windowHeight;

		public static void Init(Loader loader, int width, int height)
        {
			Loader = loader;
			windowWidth = width;
			windowHeight = height;
			Renderer = new FontRenderer();
		}

		public static void Render()
        {
			Renderer.Render(Texts);
        }

		public static void LoadText(GUIText text)
        {
			FontType font = text.Font;
			TextMeshData data = font.LoadText(text);
			int vao = Loader.LoadToVao(data.VertexPositions, data.TextureCoords);
			text.SetMeshInfo(vao, data.GetVertexCount());
			List<GUIText> textBatch;
			Texts.TryGetValue(font,out textBatch);
			if(textBatch == null)

            {
				textBatch = new List<GUIText>();
				Texts.Add(font, textBatch);
            }
			textBatch.Add(text);
        }

		public static void RemoveText(GUIText text)
        {
			List<GUIText> textBatch = Texts[text.Font];
			textBatch.Remove(text);
			if(textBatch.Count == 0)
            {
				Texts.Remove(text.Font);
            }		
        }

		public static void Delete()
        {
			Renderer.Delete();
        }
	}
}