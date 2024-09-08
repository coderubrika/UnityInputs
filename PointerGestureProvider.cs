using System;
using System.Collections.Generic;
using Suburb.Utils;
using UniRx;

namespace Suburb.Inputs
{
    public class PointerGestureProvider
    {
        private readonly TouchGestureProvider touchGestureProvider;
        private readonly MouseGestureProvider mouseGestureProvider;

        private readonly HashSet<GestureSession>[] eventsBySessions;
        private readonly LinkedList<GestureSession> sessions = new();
        private readonly CompositeDisposable compositeDisposable = new();
        private readonly int mouseId;
        
        public PointerGestureProvider(
            TouchGestureProvider touchGestureProvider,
            MouseGestureProvider mouseGestureProvider)
        {
            this.touchGestureProvider = touchGestureProvider;
            this.mouseGestureProvider = mouseGestureProvider;

            mouseId = touchGestureProvider.SupportedTouches;
            
            eventsBySessions = new HashSet<GestureSession>[mouseId + 1];
            for (int i = 0; i <= mouseId; i++)
                eventsBySessions[i] = new HashSet<GestureSession>();
        }
        
        public IDisposable AddSession(GestureSession gestureSession)
        {
            if (sessions.Count == 0)
                Enable();
            
            var node = sessions.AddFirst(gestureSession);
            return new DisposableObject(() =>
            {
                foreach (var gestureSessionSet in eventsBySessions)
                    gestureSessionSet.Remove(gestureSession);
                
                sessions.Remove(node);
                if (sessions.Count != 0)
                    return;
                Disable();
            });
        }

        private void Enable()
        {
            touchGestureProvider.Enable();
            mouseGestureProvider.Enable();
            
            mouseGestureProvider.OnPointerDown
                .Select(eventData =>
                {
                    eventData.Id = mouseId;
                    return eventData;
                })
                .Subscribe(HandleDown)
                .AddTo(compositeDisposable);

            touchGestureProvider.OnPointerDown
                .Subscribe(HandleDown)
                .AddTo(compositeDisposable);
            
            mouseGestureProvider.OnDrag
                .Merge(mouseGestureProvider.OnDragStart)
                .Merge(mouseGestureProvider.OnDragEnd)
                .Select(eventData =>
                {
                    eventData.Id = mouseId;
                    return eventData;
                })
                .Subscribe(HandleDrag)
                .AddTo(compositeDisposable);
            
            touchGestureProvider.OnDrag
                .Merge(touchGestureProvider.OnDragStart)
                .Merge(touchGestureProvider.OnDragEnd)
                .Subscribe(HandleDrag)
                .AddTo(compositeDisposable);
            
            mouseGestureProvider.OnPointerUp
                .Select(eventData =>
                {
                    eventData.Id = mouseId;
                    return eventData;
                })
                .Subscribe(HandleUp)
                .AddTo(compositeDisposable);

            touchGestureProvider.OnPointerUp
                .Subscribe(HandleUp)
                .AddTo(compositeDisposable);
        }

        private void Disable()
        {
            compositeDisposable.Clear();
            touchGestureProvider.Disable();
            mouseGestureProvider.Disable();
        }

        private void HandleDown(PointerEventData eventData)
        {
            var node = sessions.First;
            while (node != null)
            {
                var session = node.Value;
                if (!session.Contain(eventData.Position))
                {
                    node = node.Next;
                    continue;
                }
                
                node = node.Next;
                eventsBySessions[eventData.Id].Add(session);
                session.PutDown(eventData);
                
                if (session.IsBlockOther)
                    break;
            }
        }

        private void HandleDrag(PointerEventData eventData)
        {
            foreach (var session in eventsBySessions[eventData.Id])
                session.PutDrag(eventData);
        }
        
        private void HandleUp(PointerEventData eventData)
        {
            foreach (var session in eventsBySessions[eventData.Id])
                session.PutUp(eventData);
            
            eventsBySessions[eventData.Id].Clear();
        }
    }
}