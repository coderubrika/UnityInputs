using System.Collections.Generic;
using UnityEngine;

namespace Suburb.Inputs
{
    public interface IGestureSession
    {
        public bool IsBlockOther { get; }
        
        public bool Contain(PointerEventData pointerEventData);
        public void PutDown(PointerEventData pointerEventData);
        public void PutDrag(PointerEventData pointerEventData);

        public void PutUp(PointerEventData pointerEventData);
    }
}