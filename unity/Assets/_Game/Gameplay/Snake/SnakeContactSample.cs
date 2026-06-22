using UnityEngine;

namespace SnakeRoguelite.Gameplay.Snake
{
    public readonly struct SnakeContactSample
    {
        public SnakeContactSample(
            Vector3 point,
            SnakeContactZone zone,
            float radius,
            float sqrDistance)
        {
            Point = point;
            Zone = zone;
            Radius = radius;
            SqrDistance = sqrDistance;
        }

        public Vector3 Point { get; }
        public SnakeContactZone Zone { get; }
        public float Radius { get; }
        public float SqrDistance { get; }
    }
}
