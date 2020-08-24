using OpenTK;
using OpenTK.Input;
using System;
using System.Collections.Generic;

namespace Engine
{
    public class Player : Entity
    {
        private const int RUN_SPEED = 20;
        private const float TURN_SPEED = 160;
        public const float GRAVITY = -50;
        private const float JUMP_POWER = 30;

        private float currentSpeed = 0;
        private float currentTurnSpeed = 0;
        private float upwardsSpeed = 0;

        private bool isInAir = false;

        public Player(TexturedModel model, Vector3 position, float rx, float ry, float rz, float scale)
             : base(model, position, rx, ry, rz, scale)
        {

        }

        public void Move(List<Terrain> terrains)
        {
            CheckInput();
            base.Rotate(0, currentTurnSpeed * CoreEngine.Delta / 1000, 0);

            float distance = currentSpeed * CoreEngine.Delta / 1000;
            float dx = (float)(distance * Math.Sin(MathHelper.DegreesToRadians(rY)));
            float dz = (float)(distance * Math.Cos(MathHelper.DegreesToRadians(rY)));
            Move(dx, 0, dz);
            upwardsSpeed += GRAVITY * CoreEngine.Delta / 1000;
            Move(0, upwardsSpeed * CoreEngine.Delta / 1000, 0);
            foreach(Terrain terrain in terrains)
            {
                if (Position.X >= terrain.X && Position.Z >= terrain.Z)
                {
                    float terrainHeight = terrain.GetTerrainHeight(Position.X, Position.Z);
                    if (Position.Y < terrainHeight)
                    {
                        upwardsSpeed = 0;
                        Position.Y = terrainHeight;
                        isInAir = false;
                        return;
                    }
                }
            }
        }

        private void Jump()
        {
            if (!isInAir)
            {
                upwardsSpeed = JUMP_POWER;
                isInAir = true;
            }
        }

        private void CheckInput()
        {
            KeyboardState keyboard = Keyboard.GetState();
            if (keyboard.IsKeyDown(Key.W))
            {
                currentSpeed = RUN_SPEED;
            }
            else if (keyboard.IsKeyDown(Key.S))
            {
                currentSpeed = -RUN_SPEED;
            }
            else
            {
                currentSpeed = 0;
            }

            if (keyboard.IsKeyDown(Key.D))
            {
                currentTurnSpeed = -TURN_SPEED;
            }
            else if (keyboard.IsKeyDown(Key.A))
            {
                currentTurnSpeed = TURN_SPEED;
            }
            else
            {
                currentTurnSpeed = 0;
            }
            if(keyboard.IsKeyDown(Key.Space))
            {
                Jump();
            }
        }
    }
}
