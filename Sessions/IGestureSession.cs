using System.Collections.Generic;
using UnityEngine;

namespace Suburb.Inputs
{
    public interface IGestureSession
    {
        public bool IsBlockOther { get; }
        
        public bool Contain(Vector2 position);
        public void PutDown(Vector2 position);
        public void PutDrag(Vector2 delta);

        public void PutUp(Vector2 position);
    }
}