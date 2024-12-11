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

        private bool isUsed;
        
        public ReactiveCommand<Vector2> OnDown { get; } = new();
        public ReactiveCommand<Vector2> OnUp { get; } = new();
        public ReactiveCommand<Vector2> OnDrag { get; } = new();
        public bool IsBlockOther { get; private set; }
        
        public SwipeGestureSession(RectTransform bounds, RectTransform[] excludedRects)
        {
            this.bounds = bounds;
            this.excludedRects = excludedRects;
        }

        public virtual bool Contain(Vector2 position)
        {
            if (!isUsed)
            {
                IsBlockOther = CheckBound(position);
                return IsBlockOther;
            }
            
            IsBlockOther = false;
            return false;
        }

        public virtual void PutDown(Vector2 position)
        {
            isUsed = true;
            OnDown.Execute(position);
        }

        public virtual void PutDrag(Vector2 delta)
        {
            OnDrag.Execute(delta);
        }

        public virtual void PutUp(Vector2 position)
        {
            OnUp.Execute(position);
            isUsed = false;
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