﻿using System;
using System.Collections.Generic;
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
        private readonly List<PointerEventData> downEvents = new();
        private readonly List<PointerEventData> dragStartEvents = new();
        private readonly List<PointerEventData> dragEvents = new();
        private readonly List<PointerEventData> dragEndEvents = new();
        private readonly List<PointerEventData> upEvents = new();
        
        private bool isEnabled;
        private IDisposable updateDisposable;
        private int usersCount;
        
        public int SupportedTouches => Touchscreen.current == null ? 0 : Touchscreen.current.touches.Count;
        
        public ReactiveCommand<IReadOnlyCollection<PointerEventData>> OnDown { get; } = new();
        public ReactiveCommand<IReadOnlyCollection<PointerEventData>> OnUp { get; } = new();
        public ReactiveCommand<IReadOnlyCollection<PointerEventData>> OnDragStart { get; } = new();
        public ReactiveCommand<IReadOnlyCollection<PointerEventData>> OnDrag { get; } = new();
        public ReactiveCommand<IReadOnlyCollection<PointerEventData>> OnDragEnd { get; } = new();

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
            ClearEvents();
            
            for(int touchId = 0; touchId < touchStates.Length; touchId++)
                SetupTouch(touchId);

            PushEvents();
        }

        private void PushEvents()
        {
            if (downEvents.Count > 0)
                OnDown.Execute(downEvents);

            if (dragStartEvents.Count > 0)
                OnDragStart.Execute(dragStartEvents);
            
            if (dragEvents.Count > 0)
                OnDrag.Execute(dragEvents);
            
            if (upEvents.Count > 0)
                OnUp.Execute(upEvents);
            
            if (dragEndEvents.Count > 0)
                OnDragEnd.Execute(dragEndEvents);
        }

        private void ClearEvents()
        {
            downEvents.Clear();
            dragStartEvents.Clear();
            dragEvents.Clear();
            dragEndEvents.Clear();
            upEvents.Clear();
        }

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
            downEvents.Add(GetEventData(touchId));
        }

        private void SendPointerUp(int touchId)
        {
            upEvents.Add(GetEventData(touchId));
        }

        private void SendDragStart(int touchId)
        {
            isDragged[touchId] = true;
            dragStartEvents.Add(GetEventData(touchId));
        }

        private void SendDrag(int touchId)
        {
            dragEvents.Add(GetEventData(touchId));
        }

        private void SendDragEnd(int touchId)
        {
            isDragged[touchId] = false;
            dragEndEvents.Add(GetEventData(touchId));
        }

        private void CalcPositionAndDelta(int touchId)
        {
            Vector2 newPosition = Touchscreen.current.touches[touchId].position.ReadValue();
            deltas[touchId] = newPosition - positions[touchId];
            positions[touchId] = newPosition;
        }
    }
}
