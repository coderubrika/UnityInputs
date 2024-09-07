using System.Linq;
using Suburb.Utils;
using UniRx;
using UnityEngine;

namespace Suburb.Inputs
{
    public class SwipeGestureSession : GestureSession
    {
        private readonly RectTransform bounds;
        private readonly RectTransform[] excludedRects;
        
        private bool isFirstDownPassed;
        
        public RectTransform Bounds => bounds; 
        public ReactiveCommand<Vector2> OnDown { get; } = new();
        public ReactiveCommand<Vector2> OnUp { get; } = new();
        public ReactiveCommand<Vector2> OnDrag { get; } = new();
        
        public SwipeGestureSession(RectTransform bounds, RectTransform[] excludedRects)
        {
            this.bounds = bounds;
            this.excludedRects = excludedRects;
        }
        
        public override bool Contain(Vector2 position)
        {
            if (bounds != null && !bounds.Contain(position))
                return false;
            
            if (!excludedRects.IsNullOrEmpty() && excludedRects.Any(rect => rect.Contain(position)))
                return false;

            return true;
        }

        public override void PutDown(PointerEventData eventData)
        {
            isFirstDownPassed = true;
            OnDown.Execute(eventData.Position);
        }

        public override void PutDrag(PointerEventData eventData)
        {
            if (!isFirstDownPassed)
                return;
            OnDrag.Execute(eventData.Delta);
        }

        public override void PutUp(PointerEventData eventData)
        {
            if (isFirstDownPassed)
                OnUp.Execute(eventData.Position);
            isFirstDownPassed = false;
        }
    }
}