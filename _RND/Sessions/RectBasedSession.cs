using System.Linq;
using Suburb.Utils;
using UnityEngine;

namespace Suburb.Inputs
{
    public class RectBasedSession : CompositorsSession, IPointerSession
    {
        private readonly RectTransform bounds;
        private readonly RectTransform[] excludedRects;
        
        public RectBasedSession(RectTransform bounds, RectTransform[] excludedRects)
        {
            this.bounds = bounds;
            this.excludedRects = excludedRects;
        }
        
        public bool CheckIncludeInBounds(Vector2 point)
        {
            if (bounds != null && !bounds.Contain(point))
                return false;
            
            return excludedRects.IsNullOrEmpty() || !excludedRects.Any(rect => rect.Contain(point));
        }
    }
}