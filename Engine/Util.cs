using OpenTK;

namespace Engine
{
    public static class Util
    {
        public static Matrix4 CreateTransformationMatrix(Vector3 translation, float rx, float ry, float rz, float scale)
        {
            Matrix4 matrixTranslation = Matrix4.CreateTranslation(translation);

            Matrix4 matrixRx = Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rx)); 
            Matrix4 matrixRy = Matrix4.CreateRotationY(MathHelper.DegreesToRadians(ry));
            Matrix4 matrixRz = Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rz));

            Matrix4 matrixScale = Matrix4.CreateScale(scale);

            //Ordine importante 1 scala 2 rotazione 3 traslazione enastacrop
            Matrix4 matrix = matrixScale * matrixRz * matrixRy * matrixRx * matrixTranslation;

            return matrix;
        }

        public static Matrix4 CreateTransformationMatrix(Vector2 translation, Vector2 scale)
        {                           
            Vector3 translation3d = new Vector3(translation.X, translation.Y, 0.0f);
            Matrix4 matrixTranslation = Matrix4.CreateTranslation(translation3d);

            Vector3 scale3d = new Vector3(scale.X, scale.Y, 0.0f);
            Matrix4 matrixScale = Matrix4.CreateScale(scale3d);

            Matrix4 matrix = matrixScale * matrixTranslation;
            return matrix;           
        }

        public static Matrix4 CreateViewMatrix(Camera camera)
        {
            return Matrix4.LookAt(camera.Position, camera.Player.Position, new Vector3(0.0f, 1.0f, 0.0f));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="p1">Primo vertice del triangolo</param>
        /// <param name="p2">Secondo vertice del triangolo</param>
        /// <param name="p3">Terzo vertice del triangolo</param>
        /// <param name="pos">Coordinate x z sul piano del triangolo</param>
        /// <returns>L'altezza del triangolo nella posizione scelta</returns>
        public static float BarryCentric(Vector3 p1, Vector3 p2, Vector3 p3, Vector2 pos)
        {
            float det = (p2.Z - p3.Z) * (p1.X - p3.X) + (p3.X - p2.X) * (p1.Z - p3.Z);
            float l1 = ((p2.Z - p3.Z) * (pos.X - p3.X) + (p3.X - p2.X) * (pos.Y - p3.Z)) / det;
            float l2 = ((p3.Z - p1.Z) * (pos.X - p3.X) + (p1.X - p3.X) * (pos.Y - p3.Z)) / det;
            float l3 = 1.0f - l1 - l2;
            return l1 * p1.Y + l2 * p2.Y + l3 * p3.Y;
        }

    }
}
  