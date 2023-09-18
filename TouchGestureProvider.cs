using Suburb.Utils;
using System;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Suburb.Inputs
{
    public class TouchGestureProvider : IGestureProvider
    {
        private readonly GestureType[] touchStates = Enumerable.Repeat(GestureType.None, Touchscreen.current.touches.Count).ToArray();
        // TODO move to project settings
        private readonly float dragTreshold = 5;
        private readonly float zoomFactor = 0.01f;
        private bool[] isDraggings = new bool[Touchscreen.current.touches.Count];
        private bool isDoubleTouchDragging;
        private Vector2 middlePoint;
        private float doubleTouchDistance;
        private IDisposable updateDisposable;

        public ReactiveCommand<GestureEventData> OnPointerDown { get; } = new();
        public ReactiveCommand<GestureEventData> OnPointerUp { get; } = new();
        public ReactiveCommand<GestureEventData> OnDragStart { get; } = new();
        public ReactiveCommand<GestureEventData> OnDrag { get; } = new();
        public ReactiveCommand<GestureEventData> OnDragEnd { get; } = new();
        public ReactiveCommand<GestureEventData> OnZoom { get; } = new();
        public ReactiveCommand<GestureEventData> OnDragWithDoubleTouch { get; } = new();
        public ReactiveCommand<GestureEventData> OnDragStartWithDoubleTouch { get; } = new();
        public ReactiveCommand<GestureEventData> OnDragEndWithDoubleTouch { get; } = new();

        public bool IsDragging(int touchId)
        {
            return isDraggings[touchId];
        }

        public void Disable()
        {
            updateDisposable?.Dispose();
        }

        public void Enable()
        {
            updateDisposable?.Dispose();

            updateDisposable = Observable.EveryUpdate()
                .Subscribe(_ => Update());
        }

        public void Update()
        {
            for(int touchId = 0; touchId < touchStates.Length; touchId++)
                SetupTouch(touchId);

            SetupDoubleTouch();
        }

        private void SetupDoubleTouch()
        {
            bool isAllowedDoubleTouch = touchStates[0] == GestureType.Drag && touchStates[1] == GestureType.Drag;

            TouchControl touch0 = Touchscreen.current.touches[0];
            TouchControl touch1 = Touchscreen.current.touches[1];

            Vector2 position0 = touch0.position.ReadValue();
            Vector2 position1 = touch1.position.ReadValue();

            Vector2 newMiddlePoint = (position0 + position1) / 2;
            float newDoubleTouchDistance = (position1 - position0).magnitude;

            if (!isAllowedDoubleTouch)
            {
                if (isDoubleTouchDragging)
                {
                    isDoubleTouchDragging = false;
                    OnDragEndWithDoubleTouch.Execute(GetEventData(0, GestureType.DragEnd));
                }

                return;
            }

            if (!isDoubleTouchDragging)
            {
                isDoubleTouchDragging = true;

                Vector2 delta0 = touch0.delta.ReadValue();
                Vector2 delta1 = touch1.delta.ReadValue();

                Vector2 touch0Position = position0 - delta0;
                Vector2 touch1Position = position1 - delta1;

                middlePoint = (touch0Position + touch1Position) / 2;
                doubleTouchDistance = (touch1Position - touch0Position).magnitude;

                OnDragStartWithDoubleTouch.Execute(new GestureEventData()
                {
                    Id = 0,
                    Delta = Vector2.zero,
                    Position = middlePoint,
                    ZoomDelta = Vector2.zero,
                    Type = GestureType.DragStart
                });
            }

            Vector2 moveDelta = newMiddlePoint - middlePoint;

            float resultZoom = (newDoubleTouchDistance - doubleTouchDistance) * zoomFactor;

            middlePoint = newMiddlePoint;
            doubleTouchDistance = newDoubleTouchDistance;

            OnZoom.Execute(new GestureEventData()
            {
                Id = 0,
                Delta = moveDelta,
                Position = middlePoint,
                ZoomDelta = new Vector2(0, resultZoom),
                Type = GestureType.Zoom
            });

            OnDragWithDoubleTouch.Execute(new GestureEventData()
            {
                Id = 0,
                Delta = moveDelta,
                Position = middlePoint,
                ZoomDelta = new Vector2(0, resultZoom),
                Type = GestureType.Drag
            });
        }

        private void SetupTouch(int touchId)
        {
            bool isTouched = (int)Touchscreen.current.touches[touchId].press.ReadValue() == 1;

            if (isTouched)
            {
                if (touchStates[touchId] == GestureType.None)
                {
                    touchStates[touchId] = GestureType.Down;
                    SendPointerDown(touchId);
                    return;
                }

                Vector2 delta = Touchscreen.current.touches[touchId].delta.ReadValue();

                if (touchStates[touchId] == GestureType.Down && !delta.IsClose(dragTreshold))
                {
                    touchStates[touchId] = GestureType.DragStart;
                    SendDragStart(touchId);
                    return;
                }

                if (touchStates[touchId] == GestureType.DragStart)
                {
                    touchStates[touchId] = GestureType.Drag;
                    SendDrag(touchId);
                    return;
                }

                if (touchStates[touchId] == GestureType.Drag)
                {
                    SendDrag(touchId);
                    return;
                }
            }
            else if (touchStates[touchId] != GestureType.None)
            {
                touchStates[touchId] = GestureType.Up;
                SendPointerUp(touchId);

                if (isDraggings[touchId])
                {
                    touchStates[touchId] = GestureType.DragEnd;
                    SendDragEnd(touchId);
                }

                touchStates[touchId] = GestureType.None;
            }
        }

        private GestureEventData GetEventData(int touchId, GestureType gestureType)
        {
            return new GestureEventData()
            {
                Id = touchId,
                Position = Touchscreen.current.touches[touchId].position.ReadValue(),
                Delta = Touchscreen.current.touches[touchId].delta.ReadValue(),
                ZoomDelta = Vector2.zero,
                Type = gestureType
            };
        }

        private void SendPointerDown(int touchId)
        {
            OnPointerDown.Execute(GetEventData(touchId, GestureType.Down));
        }

        private void SendPointerUp(int touchId)
        {
            OnPointerUp.Execute(GetEventData(touchId, GestureType.Up));
        }

        private void SendDragStart(int touchId)
        {
            isDraggings[touchId] = true;
            OnDragStart.Execute(GetEventData(touchId, GestureType.DragStart));
        }

        private void SendDrag(int touchId)
        {
            OnDrag.Execute(GetEventData(touchId, GestureType.Drag));
        }

        private void SendDragEnd(int touchId)
        {
            isDraggings[touchId] = false;
            OnDragEnd.Execute(GetEventData(touchId, GestureType.DragEnd));
        }
    }
}
