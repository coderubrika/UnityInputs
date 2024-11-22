using System.Linq;
using UniRx;
using UnityEngine;
using Suburb.Utils;

namespace Suburb.Inputs
{
    public class DragZoomGestureSession
    {
        private readonly RectTransform bounds;
        private readonly RectTransform[] excludedRects;
        
        private bool isFirstDownPassed;
        
        public ReactiveCommand<Vector2> OnDown { get; } = new();
        public ReactiveCommand<Vector2> OnUp { get; } = new();
        public ReactiveCommand<Vector2> OnDrag { get; } = new();
        public ReactiveCommand<float> OnZoom { get; } = new();

        public RectTransform Bounds => bounds; 
        
        public DragZoomGestureSession(RectTransform bounds, RectTransform[] excludedRects)
        {
            this.bounds = bounds;
            this.excludedRects = excludedRects;
        }

        public void PutDown(Vector2 position)
        {
            if (bounds != null && !bounds.Contain(position))
                return;
            
            if (!excludedRects.IsNullOrEmpty() && excludedRects.Any(rect => rect.Contain(position)))
                return;
            
            isFirstDownPassed = true;
            OnDown.Execute(position);
        }
        
        public void PutUp(Vector2 position)
        {
            if (isFirstDownPassed)
                OnUp.Execute(position);
            isFirstDownPassed = false;
        }
        
        public void PutDrag(Vector2 delta, Vector2 position)
        {
            if (!isFirstDownPassed)
                return;
            OnDrag.Execute(delta);
        }
        
        public void PutDragTouches(Vector2 delta, Vector2 touch0, Vector2 touch1)
        {
            if (bounds != null && !bounds.Contain(touch0) && !bounds.Contain(touch1))
                return;

            if (!excludedRects.IsNullOrEmpty() && (!isFirstDownPassed || excludedRects.Any(rect => rect.Contain(touch0))))
                return;
            
            OnDrag.Execute(delta);
        }
        
        public void PutZoom(float zoom)
        {
            OnZoom.Execute(zoom);
        }
        
        public void PutZoomTouches(float zoom, Vector2 touch0, Vector2 touch1)
        {
            if (bounds != null && !bounds.Contain(touch0) && !bounds.Contain(touch1))
                return;
            
            if (!excludedRects.IsNullOrEmpty() && (!isFirstDownPassed || excludedRects.Any(rect => rect.Contain(touch0))))
                return;
            
            OnZoom.Execute(zoom);
        }
    }
}