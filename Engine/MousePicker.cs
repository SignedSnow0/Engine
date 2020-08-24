using OpenTK;
using OpenTK.Input;

namespace Engine
{
    public class MousePicker
    {
        public Vector3 CurrentRay { get; private set; }
        public Matrix4 ProjectionMatrix { get; private set; }
        public Matrix4 VievMatrix { get; private set; }
        public Camera Camera { get; private set; }
        public int width;
        public int height;

        public MousePicker(Camera camera, Matrix4 projectionMatrix)
        {
            Camera = camera;
            ProjectionMatrix = projectionMatrix;
            VievMatrix = Util.CreateViewMatrix(camera);
        }

        public void Update()
        {
            VievMatrix = Util.CreateViewMatrix(Camera);
            CurrentRay = CalculatMouseRay();           
        }

        private Vector3 CalculatMouseRay()
        {
            MouseState mouse = Mouse.GetCursorState();
            float mouseX = mouse.X;
            float mouseY = mouse.Y;
            Vector2 normalizedCoords = NormalizedDeviceCoords(mouseX, mouseY);
            Vector4 clipCoords = new Vector4(normalizedCoords.X, normalizedCoords.Y, 1.0f, 1.0f);
            Vector4 eyeCoords = ToEyeCoords(clipCoords);
            Vector3 worldRay = ToWorldCoords(eyeCoords);
            return worldRay;
        }
        private Vector3 ToWorldCoords(Vector4 eyeCoords)
        {
            Matrix4 invertedView = Matrix4.Invert(VievMatrix);
            Vector4 rayWorld = Vector4.Transform(eyeCoords, invertedView);
            Vector3 mouseRay = new Vector3(rayWorld.Xyz);
            mouseRay.Normalize();
            return mouseRay;
        }
        private Vector4 ToEyeCoords(Vector4 clipCoords)
        {
            Matrix4 invertedProjection = Matrix4.Invert(ProjectionMatrix);
            Vector4 eyeCoords = Vector4.Transform(clipCoords, invertedProjection);
            return new Vector4(eyeCoords.X, eyeCoords.Y, 1.0f, 0.0f);
        }
        private Vector2 NormalizedDeviceCoords(float mouseX, float mouseY)
        {
            float x = (2.0f * mouseX) / width - 1.0f;
            float y = (2.0f * mouseY) / height - 1.0f;
            return new Vector2(-x, y);
        }            
    }
}
