using OpenTK;
using OpenTK.Input;
using System;

namespace Engine
{
    public class Camera
    {
        /// <summary>
        /// La posizione nel mondo della camera
        /// </summary>
        public Vector3 Position = new Vector3(0.0f, 20.0f, 10.0f);
        /// <summary>
        /// Langolo di rotazione attorno l`asse Y
        /// </summary>
        public float Pitch = 10;
        /// <summary>
        /// L`angolo di rotazione attorno l`asse Z
        /// </summary>
        public float Yaw;
        private float distanceFromPlayer = 50;
        private float angleAroundPlayer;
        private MouseState mousePrevious;
        private MouseState mousePreviousAngle;

        public Player Player { get; private set; }

        /// <summary>
        /// Crea un`istanza della classe Camera che segue il giocatore
        /// </summary>
        /// <param name="player">Il giocatore da seguire</param>
        public Camera(Player player)
        {
            Player = player;
        }
        /// <summary>
        /// Aggiorna la posizione relativa della telecamera rispetto il giocatore
        /// </summary>
        public void Move()
        {
            CalculateZoom();
            CalculatePitch();
            CalculateAngleAroundPlayer();
            float horizontalDistance = CalculateHorizontalDistance();
            float verticalDistance = CalculateVerticalDistance();
            CalculateCameraPosition(horizontalDistance, verticalDistance);
            Yaw = (Player.rY + angleAroundPlayer) - 180;
        }
        private void CalculateCameraPosition(float horizontalDistance, float verticalDistance)
        {
            float theta = Player.rY + angleAroundPlayer;
            float offsetX = (float)(horizontalDistance * Math.Sin(MathHelper.DegreesToRadians(theta)));
            float offsetZ = (float)(horizontalDistance * Math.Cos(MathHelper.DegreesToRadians(theta)));
            Position.X = Player.Position.X - offsetX;
            Position.Z = Player.Position.Z - offsetZ;
            Position.Y = Player.Position.Y + verticalDistance;
        }
        private float CalculateHorizontalDistance()
        {
            return (float)(distanceFromPlayer * Math.Cos(MathHelper.DegreesToRadians(Pitch)));
        }
        private float CalculateVerticalDistance()
        {
            return (float)(distanceFromPlayer * Math.Sin(MathHelper.DegreesToRadians(Pitch)));
        }
        private void CalculateZoom()
        {
            MouseState mouse = Mouse.GetState();
            var delta = mouse.WheelPrecise - mousePrevious.WheelPrecise;
            if(delta != 0)
            {
                float zoomLevel = delta * 1.5f;
                distanceFromPlayer -= zoomLevel;
                mousePrevious = mouse;
            }
        }
        private void CalculatePitch()
        {
            var mouse = Mouse.GetState();
            var delta = mouse.Y - mousePrevious.Y;

            if (mouse.IsButtonDown(MouseButton.Right))
            {
                Pitch -= delta * 0.1f;
            }

            mousePrevious = mouse;
        }
        private void CalculateAngleAroundPlayer()
        {
            var mouse = Mouse.GetState();
            var delta = mouse.X - mousePreviousAngle.X;

            if (mouse.IsButtonDown(MouseButton.Right))
            {
                angleAroundPlayer -= delta * 0.1f;
            }

            mousePreviousAngle = mouse;
        }
    }
}
