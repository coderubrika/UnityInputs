﻿using System;
using System.Linq;
using Suburb.Utils;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Suburb.Inputs
{
    public class TouchInputProvider
    {
        private readonly GestureType[] touchStates;
        private readonly bool[] isDragged;
        private readonly Vector2[] positions;
        private readonly Vector2[] deltas;
        private bool isEnabled;
        //private bool isDoubleTouchDragging;
        //private Vector2 middlePoint;
        //private float doubleTouchDistance;
        private IDisposable updateDisposable;
        private int usersCount;
        
        public int SupportedTouches => Touchscreen.current == null ? 0 : Touchscreen.current.touches.Count;
        
        public ReactiveCommand<PointerEventData> OnPointerDown { get; } = new();
        public ReactiveCommand<PointerEventData> OnPointerUp { get; } = new();
        public ReactiveCommand<PointerEventData> OnDragStart { get; } = new();
        public ReactiveCommand<PointerEventData> OnDrag { get; } = new();
        public ReactiveCommand<PointerEventData> OnDragEnd { get; } = new();
        
        //public ReactiveCommand<GestureEventData> OnDragWithDoubleTouch { get; } = new();
        //public ReactiveCommand<GestureEventData> OnDragStartWithDoubleTouch { get; } = new();
        //public ReactiveCommand<GestureEventData> OnDragEndWithDoubleTouch { get; } = new();

        public TouchInputProvider()
        {
            touchStates = Enumerable.Repeat(GestureType.None, SupportedTouches).ToArray();
            isDragged = new bool[SupportedTouches];
            positions = Enumerable.Repeat(Vector2.zero, SupportedTouches).ToArray();
            deltas = Enumerable.Repeat(Vector2.zero, SupportedTouches).ToArray();
        }
        
        private void Disable()
        {
            if (usersCount == 0)
                return;

            usersCount -= 1;

            if (usersCount > 0)
                return;
            
            for (int i = 0; i < isDragged.Length; i++)
            {
                isDragged[i] = false;
                positions[i] = Vector2.zero;
                deltas[i] = Vector2.zero;
            }

            // isDoubleTouchDragging = false;
            // middlePoint = Vector2.zero;
            // doubleTouchDistance = 0;
            
            updateDisposable?.Dispose();
            isEnabled = false;
        }

        public IDisposable Enable()
        {
            usersCount += 1;
            
            if (isEnabled || touchStates.Length == 0)
                return new DisposableObject(Disable);

            isEnabled = true;

            updateDisposable = Observable.EveryUpdate()
                .Subscribe(_ => Update());
            
            return new DisposableObject(Disable);
        }

        private void Update()
        {
            for(int touchId = 0; touchId < touchStates.Length; touchId++)
                SetupTouch(touchId);

            //SetupDoubleTouch();
        }

        // private void SetupDoubleTouch()
        // {
        //     bool isAllowedDoubleTouch = touchStates[0] == GestureType.Drag && touchStates[1] == GestureType.Drag;
        //
        //     if (!isAllowedDoubleTouch)
        //     {
        //         if (isDoubleTouchDragging)
        //         {
        //             int touchId = touchStates[0] != GestureType.Drag ? 0 : 1;
        //             isDoubleTouchDragging = false;
        //             OnDragEndWithDoubleTouch.Execute(GetEventData(touchId, GestureType.DragEnd));
        //         }
        //
        //         return;
        //     }
        //     
        //     Vector2 newMiddlePoint = (positions[0] + positions[1]) / 2;
        //     float newDoubleTouchDistance = (positions[1] - positions[0]).magnitude;
        //
        //     if (!isDoubleTouchDragging)
        //     {
        //         isDoubleTouchDragging = true;
        //
        //         Vector2 touch0Position = positions[0] - deltas[0];
        //         Vector2 touch1Position = positions[1] - deltas[1];
        //
        //         middlePoint = (touch0Position + touch1Position) / 2;
        //         doubleTouchDistance = (touch1Position - touch0Position).magnitude;
        //
        //         OnDragStartWithDoubleTouch.Execute(new GestureEventData()
        //         {
        //             Id = supportedTouches + 1,
        //             Delta = deltas[0],
        //             Position = middlePoint,
        //             Zoom = 1,
        //             Type = GestureType.DragStart
        //         });
        //     }
        //
        //     Vector2 moveDelta = newMiddlePoint - middlePoint;
        //
        //     float resultZoom = 1f;
        //     
        //     if (doubleTouchDistance != 0)
        //         resultZoom = newDoubleTouchDistance / doubleTouchDistance;
        //
        //     middlePoint = newMiddlePoint;
        //     doubleTouchDistance = newDoubleTouchDistance;
        //
        //     OnDragWithDoubleTouch.Execute(new GestureEventData()
        //     {
        //         Id = supportedTouches + 1,
        //         Delta = moveDelta,
        //         Position = middlePoint,
        //         Zoom = resultZoom,
        //         Type = GestureType.Drag
        //     });
        // }

        private void SetupTouch(int touchId)
        {
            bool isTouched = (int)Touchscreen.current.touches[touchId].press.ReadValue() == 1;

            if (isTouched)
            {
                CalcPositionAndDelta(touchId);
                
                if (touchStates[touchId] == GestureType.None)
                {
                    touchStates[touchId] = GestureType.Down;
                    positions[touchId] = Touchscreen.current.touches[touchId].position.ReadValue();
                    deltas[touchId] = Vector2.zero;
                    SendPointerDown(touchId);
                    return;
                }
                
                if (touchStates[touchId] == GestureType.Down && deltas[touchId] != Vector2.zero)
                {
                    touchStates[touchId] = GestureType.DragStart;
                    SendDragStart(touchId);
                    return;
                }

                if (touchStates[touchId] == GestureType.DragStart)
                {
                    touchStates[touchId] = GestureType.Drag;
                    
                    if (deltas[touchId] == Vector2.zero)
                        return;
                    
                    SendDrag(touchId);
                    return;
                }

                if (touchStates[touchId] == GestureType.Drag && deltas[touchId] != Vector2.zero)
                {
                    SendDrag(touchId);
                }
            }
            else if (touchStates[touchId] != GestureType.None)
            {
                touchStates[touchId] = GestureType.Up;
                CalcPositionAndDelta(touchId);
                SendPointerUp(touchId);

                if (isDragged[touchId])
                {
                    touchStates[touchId] = GestureType.DragEnd;
                    SendDragEnd(touchId);
                }

                touchStates[touchId] = GestureType.None;
            }
        }

        private PointerEventData GetEventData(int touchId)
        {
            return new PointerEventData()
            {
                Id = touchId,
                Position = positions[touchId],
                Delta = deltas[touchId],
            };
        }

        private void SendPointerDown(int touchId)
        {
            OnPointerDown.Execute(GetEventData(touchId));
        }

        private void SendPointerUp(int touchId)
        {
            OnPointerUp.Execute(GetEventData(touchId));
        }

        private void SendDragStart(int touchId)
        {
            isDragged[touchId] = true;
            OnDragStart.Execute(GetEventData(touchId));
        }

        private void SendDrag(int touchId)
        {
            OnDrag.Execute(GetEventData(touchId));
        }

        private void SendDragEnd(int touchId)
        {
            isDragged[touchId] = false;
            OnDragEnd.Execute(GetEventData(touchId));
        }

        private void CalcPositionAndDelta(int touchId)
        {
            Vector2 newPosition = Touchscreen.current.touches[touchId].position.ReadValue();
            deltas[touchId] = newPosition - positions[touchId];
            positions[touchId] = newPosition;
        }
    }
}
