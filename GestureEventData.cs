using UnityEngine;

namespace Suburb.Inputs
{
    public class GestureEventData
    {
        public int Id;
        public Vector2 Position;
        public Vector2 Delta;
        public Vector2 ZoomDelta;
        public GestureType Type;

        public GestureEventData CopyWithType(GestureType Type)
        {
            return new GestureEventData()
            {
                Id = Id,
                Position = Position,
                Delta = Delta,
                ZoomDelta = ZoomDelta,
                Type = Type
            };
        }
    }
}
