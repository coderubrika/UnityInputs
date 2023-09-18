using UniRx;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;
using Suburb.Utils;
using System;

namespace Suburb.Inputs
{
    public class MouseGestureProvider : IGestureProvider
    {
        private readonly MouseControls inputControls;

        // TODO move to project settings
        private readonly float dragTreshold = 5f;

        private IDisposable updateDisposable;
        private bool isDragging;

        private GestureType currentGesture = GestureType.None;

        public ReactiveCommand<GestureEventData> OnPointerDown { get; } = new();
        public ReactiveCommand<GestureEventData> OnPointerUp { get; } = new();
        public ReactiveCommand<GestureEventData> OnDragStart { get; } = new();
        public ReactiveCommand<GestureEventData> OnDrag { get; } = new();
        public ReactiveCommand<GestureEventData> OnDragEnd { get; } = new();
        public ReactiveCommand<GestureEventData> OnZoom { get; } = new();

        public bool IsDragging(int pointerId)
        {
            return isDragging;
        }

        public MouseGestureProvider()
        {
            inputControls = new MouseControls();

            inputControls.Mouse.Down.performed += PointerDown;
            inputControls.Mouse.Up.performed += PointerUp;
            inputControls.Mouse.Zoom.performed += Zoom;
        }

        public void Disable()
        {
            updateDisposable?.Dispose();
            inputControls.Disable();
        }

        public void Enable()
        {
            updateDisposable?.Dispose();

            inputControls.Enable();

            updateDisposable = Observable.EveryUpdate()
                .Subscribe(_ => Update());
        }

        private void Update()
        {
            if (currentGesture == GestureType.None)
                return;

            if (currentGesture == GestureType.Down
                && !inputControls.Mouse.Delta.ReadValue<Vector2>().IsClose(dragTreshold))
            {
                isDragging = true;
                currentGesture = GestureType.DragStart;
                OnDragStart.Execute(GetEventData(GestureType.DragStart));
                return;
            }

            if (currentGesture == GestureType.DragStart)
            {
                currentGesture = GestureType.Drag;
                OnDrag.Execute(GetEventData(GestureType.Drag));
                return;
            }

            if (currentGesture == GestureType.Drag)
            {
                OnDrag.Execute(GetEventData(GestureType.Drag));
                return;
            }
        }

        private void PointerDown(CallbackContext context)
        {
            currentGesture = GestureType.Down;
            OnPointerDown.Execute(GetEventData(GestureType.Down));
        }

        private void PointerUp(CallbackContext context)
        {
            currentGesture = GestureType.Up;
            OnPointerUp.Execute(GetEventData(GestureType.Up));

            if (isDragging)
            {
                currentGesture = GestureType.DragEnd;
                isDragging = false;
                OnDragEnd.Execute(GetEventData(GestureType.DragEnd));
            }

            currentGesture = GestureType.None;
            return;
        }

        private void Zoom(CallbackContext context)
        {
            OnZoom.Execute(GetEventData(GestureType.Zoom));
        }

        private GestureEventData GetEventData (GestureType gestureType)
        {
            return new GestureEventData()
            {
                Id = inputControls.Mouse.Id.ReadValue<int>(),
                Position = inputControls.Mouse.Position.ReadValue<Vector2>(),
                Delta = inputControls.Mouse.Delta.ReadValue<Vector2>(),
                ZoomDelta = inputControls.Mouse.Zoom.ReadValue<Vector2>() / 360,
                Type = gestureType
            };
        }
    }
}