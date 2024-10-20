using UniRx;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;
using System;
using Suburb.Utils;

namespace Suburb.Inputs
{
    public class MouseInputProvider
    {
        private readonly MouseControls inputControls;

        private IDisposable updateDisposable;
        private bool isDragging;
        private bool isEnabled;
        private Vector2 position;
        private Vector2 delta;
        private GestureType currentGesture = GestureType.None;
        private int usersCount;
        
        public ReactiveCommand<MouseEventData> OnPointerDown { get; } = new();
        public ReactiveCommand<MouseEventData> OnPointerUp { get; } = new();
        public ReactiveCommand<MouseEventData> OnPointerMove { get; } = new();
        public ReactiveCommand<MouseEventData> OnDragStart { get; } = new();
        public ReactiveCommand<MouseEventData> OnDrag { get; } = new();
        public ReactiveCommand<MouseEventData> OnDragEnd { get; } = new();
        public ReactiveCommand<MouseEventData> OnZoom { get; } = new();

        public MouseInputProvider()
        {
            inputControls = new MouseControls();

            inputControls.Mouse.Down.performed += PointerDown;
            inputControls.Mouse.Up.performed += PointerUp;
            inputControls.Mouse.Zoom.performed += Zoom;
            inputControls.Mouse.Delta.performed += PointerMove;
        }

        private void Disable()
        {
            if (usersCount == 0)
                return;

            usersCount -= 1;

            if (usersCount > 0)
                return;
            
            currentGesture = GestureType.None;
            position = Vector2.zero;
            delta = Vector2.zero;
            updateDisposable?.Dispose();
            inputControls.Disable();
            isDragging = false;
            isEnabled = false;
        }

        public IDisposable Enable()
        {
            usersCount += 1;
            if (isEnabled)
                return new DisposableObject(Disable);

            isEnabled = true;
            inputControls.Enable();

            updateDisposable = Observable.EveryUpdate()
                .Subscribe(_ => Update());
            
            return new DisposableObject(Disable);
        }

        private void Update()
        {
            if (currentGesture == GestureType.None)
                return;
            
            CalcPositionAndDelta();
            if (currentGesture == GestureType.Down && delta != Vector2.zero)
            {
                isDragging = true;
                currentGesture = GestureType.DragStart;
                OnDragStart.Execute(GetEventData());
                return;
            }

            if (currentGesture == GestureType.DragStart)
            {
                currentGesture = GestureType.Drag;
                
                if (delta == Vector2.zero)
                    return;
                
                OnDrag.Execute(GetEventData());
                return;
            }

            if (currentGesture == GestureType.Drag && delta != Vector2.zero)
            {
                OnDrag.Execute(GetEventData());
            }
        }

        private void PointerDown(CallbackContext context)
        {
            currentGesture = GestureType.Down;
            position = inputControls.Mouse.Position.ReadValue<Vector2>();
            delta = Vector2.zero;
            OnPointerDown.Execute(GetEventData());
        }

        private void PointerUp(CallbackContext context)
        {
            currentGesture = GestureType.Up;
            CalcPositionAndDelta();
            OnPointerUp.Execute(GetEventData());
            if (isDragging)
            {
                currentGesture = GestureType.DragEnd;
                isDragging = false;
                OnDragEnd.Execute(GetEventData());
            }

            currentGesture = GestureType.None;
        }

        private void Zoom(CallbackContext context)
        {
            OnZoom.Execute(GetEventData());
        }

        private float GetZoom(float wheel)
        {
            return (150f + Mathf.Clamp(wheel, -100f, 100f)) / 150f;
        }
        
        private void PointerMove(CallbackContext context)
        {
            OnPointerMove.Execute(GetEventData());
        }

        private MouseEventData GetEventData()
        {
            return new MouseEventData
            {
                Id = inputControls.Mouse.Id.ReadValue<int>(),
                Position = position,
                Delta = delta,
                Zoom =  GetZoom(inputControls.Mouse.Zoom.ReadValue<Vector2>().y)
            };
        }

        private void CalcPositionAndDelta()
        {
            Vector2 newPosition = inputControls.Mouse.Position.ReadValue<Vector2>();
            delta = newPosition - position;
            position = newPosition;
        }
    }
}