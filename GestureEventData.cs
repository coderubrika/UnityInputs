using UnityEngine;

namespace Suburb.Inputs
{
    public struct GestureEventData
    {
        public int Id;
        public Vector2 Position;
        public Vector2 Delta;
        public float Zoom;
        public GestureType Type;

        public GestureEventData CopyWithType(GestureType type)
        {
            return new GestureEventData()
            {
                Id = Id,
                Position = Position,
                Delta = Delta,
                Zoom = Zoom,
                Type = type
            };
        }
    }
}
