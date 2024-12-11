using UniRx;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;
using System;

namespace Suburb.Inputs
{
    public class MouseProvider
    {
        private readonly MouseControls inputControls;

        private IDisposable updateDisposable;
        private bool isDragging;
        private bool isEnabled;
        private Vector2 position;
        private Vector2 delta;
        private GestureType currentGesture = GestureType.None;
        private int usersCount;
        
        public ReactiveCommand OnDown { get; } = new();
        public ReactiveCommand OnUp { get; } = new();
        public ReactiveCommand OnMove { get; } = new();
        public ReactiveCommand OnDragStart { get; } = new();
        public ReactiveCommand OnDrag { get; } = new();
        public ReactiveCommand OnDragEnd { get; } = new();
        public ReactiveCommand OnZoom { get; } = new();

        public MouseEventData EventData { get; private set; }
        
        public MouseProvider()
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
                return Disposable.Create(Disable);

            isEnabled = true;
            inputControls.Enable();

            updateDisposable = Observable.EveryUpdate()
                .Subscribe(_ => Update());
            
            return Disposable.Create(Disable);
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
                EventData = GetEventData();
                OnDragStart.Execute();
                return;
            }

            if (currentGesture == GestureType.DragStart)
            {
                currentGesture = GestureType.Drag;
                
                if (delta == Vector2.zero)
                    return;
                
                EventData = GetEventData();
                OnDrag.Execute();
                return;
            }

            if (currentGesture == GestureType.Drag && delta != Vector2.zero)
            {
                EventData = GetEventData();
                OnDrag.Execute();
            }
        }

        private void PointerDown(CallbackContext context)
        {
            currentGesture = GestureType.Down;
            position = inputControls.Mouse.Position.ReadValue<Vector2>();
            delta = Vector2.zero;
            EventData = GetEventData();
            OnDown.Execute();
        }

        private void PointerUp(CallbackContext context)
        {
            currentGesture = GestureType.Up;
            CalcPositionAndDelta();
            EventData = GetEventData();
            OnUp.Execute();
            if (isDragging)
            {
                currentGesture = GestureType.DragEnd;
                isDragging = false;
                EventData = GetEventData();
                OnDragEnd.Execute();
            }

            currentGesture = GestureType.None;
        }

        private void Zoom(CallbackContext context)
        {
            EventData = GetEventData();
            OnZoom.Execute();
        }

        private float GetZoom(float wheel)
        {
            return (150f + Mathf.Clamp(wheel, -100f, 100f)) / 150f;
        }
        
        private void PointerMove(CallbackContext context)
        {
            OnMove.Execute();
        }

        private MouseEventData GetEventData()
        {
            return new MouseEventData
            {
                Id = inputControls.Mouse.Id.ReadValue<int>(),
                Position = position,
                Delta = delta,
                Zoom = GetZoom(inputControls.Mouse.Zoom.ReadValue<Vector2>().y)
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