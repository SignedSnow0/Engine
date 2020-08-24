using OpenTK;

namespace Engine
{
    /// <summary>
    /// Clase contenente le informazioni di una luce
    /// </summary>
    public class Light
    {
        public Vector3 Position { get; private set; }
        public Vector3 Color { get; private set; }
        public Vector3 Attenuation { get; private set; } = new Vector3(1.0f, 0.0f, 0.0f);

        /// <summary>
        /// Instanzia un oggetto di classe luce
        /// </summary>
        /// <param name="position">La posizione della fonte</param>
        /// <param name="color">Il colore della luce</param>
        public Light(Vector3 position, Vector3 color)
        {
            Position = position;
            Color = color;
        }
        public Light(Vector3 position, Vector3 color, Vector3 attenuation)
        {
            Position = position;
            Color = color;
            Attenuation = attenuation;
        }
    }
}
