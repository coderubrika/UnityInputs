using UnityEngine;

namespace Suburb.Inputs
{
    public abstract class GestureSession
    {
        public bool IsBlockOther { get; protected set; }

        public void SetBlockOther(bool isOn)
        {
            IsBlockOther = isOn;
        }
        
        public abstract bool Contain(Vector2 position);
        public abstract void PutDown(PointerEventData position);
        public abstract void PutDrag(PointerEventData pointerEventData);

        public abstract void PutUp(PointerEventData pointerEventData);
    }
}