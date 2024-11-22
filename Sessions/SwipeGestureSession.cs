using System.Linq;
using Suburb.Utils;
using UniRx;
using UnityEngine;

namespace Suburb.Inputs
{
    public class SwipeGestureSession : IGestureSession
    {
        private readonly RectTransform bounds;
        private readonly RectTransform[] excludedRects;

        protected int FirstId = -1;
        
        public ReactiveCommand<Vector2> OnDown { get; } = new();
        public ReactiveCommand<Vector2> OnUp { get; } = new();
        public ReactiveCommand<Vector2> OnDrag { get; } = new();
        public bool IsBlockOther { get; protected set; }
        public SwipeGestureSession(RectTransform bounds, RectTransform[] excludedRects)
        {
            this.bounds = bounds;
            this.excludedRects = excludedRects;
        }

        public virtual bool Contain(PointerEventData eventData)
        {
            if (FirstId == -1)
            {
                IsBlockOther = CheckBound(eventData.Position);
                return IsBlockOther;
            }
            
            IsBlockOther = false;
            return false;
        }

        public virtual void PutDown(PointerEventData eventData)
        {
            FirstId = eventData.Id;
            OnDown.Execute(eventData.Position);
        }

        public virtual void PutDrag(PointerEventData eventData)
        {
            OnDrag.Execute(eventData.Delta);
        }

        public virtual void PutUp(PointerEventData eventData)
        {
            OnUp.Execute(eventData.Position);
            FirstId = -1;
        }

        public Vector3 ToCanvasDirection(Vector2 delta)
        {
            return bounds.InverseTransformDirection(delta);
        }

        public Vector3 ToCanvasPoint(Vector2 point)
        {
            return bounds.InverseTransformPoint(point);
        }
        
        private bool CheckBound(Vector2 position)
        {
            if (bounds != null && !bounds.Contain(position))
                return false;
            
            return excludedRects.IsNullOrEmpty() || !excludedRects.Any(rect => rect.Contain(position));
        }
    }
}