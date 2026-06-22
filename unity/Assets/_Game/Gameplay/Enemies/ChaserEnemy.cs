using UnityEngine;

namespace SnakeRoguelite.Gameplay.Enemies
{
    public sealed class ChaserEnemy : PrototypeEnemyBase
    {
        [SerializeField, Min(0.5f)] private float moveSpeed = 3.6f;

        protected override void TickMovement(float deltaTime)
        {
            if (TargetSnake == null)
            {
                return;
            }

            MoveTowards(TargetSnake.HeadPosition, moveSpeed, deltaTime);
        }
    }
}
