using UniRx;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;
using System;

namespace Suburb.Inputs
{
    public class MouseProvider
    {
        public enum MouseButton
        {
            Left,
            Right,
            Middle,
        }
        
        private readonly MouseControls inputControls;

        private IDisposable updateDisposable;
        private bool isEnabled;
        private int usersCount;
        private Vector2 delta;
        
        public ReactiveCommand<MouseButton> OnDown { get; } = new();
        public ReactiveCommand<MouseButton> OnUp { get; } = new();
        public ReactiveCommand<Vector2> OnMove { get; } = new();
        public ReactiveCommand<float> OnZoom { get; } = new();

        public Vector2 Position { get; private set; }
        
        public MouseProvider()
        {
            inputControls = new MouseControls();

            inputControls.Mouse.DownLeft.performed += _ => Down(MouseButton.Left);
            inputControls.Mouse.DownRight.performed += _ => Down(MouseButton.Right);
            inputControls.Mouse.DownMiddle.performed += _ => Down(MouseButton.Middle);
            inputControls.Mouse.UpLeft.performed += _ => Up(MouseButton.Left);
            inputControls.Mouse.UpRight.performed += _ => Up(MouseButton.Right);
            inputControls.Mouse.UpMiddle.performed += _ => Up(MouseButton.Middle);
            inputControls.Mouse.Zoom.performed += Zoom;
        }

        private void Disable()
        {
            if (usersCount == 0)
                return;

            usersCount -= 1;

            if (usersCount > 0)
                return;
            
            Position = Vector2.zero;
            delta = Vector2.zero;
            updateDisposable?.Dispose();
            inputControls.Disable();
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
                .Subscribe(_ => CalcPositionAndDelta());
            
            return Disposable.Create(Disable);
        }

        private void Down(MouseButton button)
        {
            CalcPositionAndDelta();
            OnDown.Execute(button);
        }

        private void Up(MouseButton button)
        {
            CalcPositionAndDelta();
            OnUp.Execute(button);
        }

        private void Zoom(CallbackContext context)
        {
            var zoom = GetZoom(inputControls.Mouse.Zoom.ReadValue<Vector2>().y);
            OnZoom.Execute(zoom);
        }

        private float GetZoom(float wheel)
        {
            return (150f + Mathf.Clamp(wheel, -100f, 100f)) / 150f;
        }
        
        private void CalcPositionAndDelta()
        {
            Vector2 newPosition = inputControls.Mouse.Position.ReadValue<Vector2>();
            delta = newPosition - Position;
            if (delta != Vector2.zero)
                OnMove.Execute(delta);
            Position = newPosition;
        }
    }
}