using SnakeRoguelite.Gameplay.Snake;
using UnityEngine;

namespace SnakeRoguelite.Gameplay.Enemies
{
    public sealed class TankEnemy : PrototypeEnemyBase
    {
        [SerializeField, Min(0.5f)] private float moveSpeed = 2f;

        protected override void TickMovement(float deltaTime)
        {
            if (TargetSnake == null)
            {
                return;
            }

            TargetSnake.TryGetClosestContactSample(transform.position, out var sample);
            var targetPoint = sample.Zone == SnakeContactZone.Head
                ? TargetSnake.TailPosition
                : sample.Point;

            MoveTowards(targetPoint, moveSpeed, deltaTime);
        }
    }
}
