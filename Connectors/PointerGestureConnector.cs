using System;
using System.Collections.Generic;
using Suburb.Utils;
using UniRx;

namespace Suburb.Inputs
{
    public class PointerGestureConnector
    {
        private readonly TouchInputProvider touchInputProvider;
        private readonly MouseInputProvider mouseInputProvider;

        private readonly HashSet<GestureSession>[] eventsBySessions;
        private readonly LinkedList<GestureSession> sessions = new();
        private readonly CompositeDisposable compositeDisposable = new();
        private readonly int mouseId;
        
        public PointerGestureConnector(
            TouchInputProvider touchInputProvider,
            MouseInputProvider mouseInputProvider)
        {
            this.touchInputProvider = touchInputProvider;
            this.mouseInputProvider = mouseInputProvider;

            mouseId = touchInputProvider.SupportedTouches;
            
            eventsBySessions = new HashSet<GestureSession>[mouseId + 1];
            for (int i = 0; i <= mouseId; i++)
                eventsBySessions[i] = new HashSet<GestureSession>();
        }
        
        public IDisposable Connect(GestureSession gestureSession)
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
            touchInputProvider.Enable()
                .AddTo(compositeDisposable);
            mouseInputProvider.Enable()
                .AddTo(compositeDisposable);
            
            mouseInputProvider.OnPointerDown
                .Select(eventData =>
                {
                    eventData.Id = mouseId;
                    return eventData;
                })
                .Subscribe(HandleDown)
                .AddTo(compositeDisposable);

            touchInputProvider.OnPointerDown
                .Subscribe(HandleDown)
                .AddTo(compositeDisposable);
            
            mouseInputProvider.OnDrag
                .Merge(mouseInputProvider.OnDragStart)
                .Merge(mouseInputProvider.OnDragEnd)
                .Select(eventData =>
                {
                    eventData.Id = mouseId;
                    return eventData;
                })
                .Subscribe(HandleDrag)
                .AddTo(compositeDisposable);
            
            touchInputProvider.OnDrag
                .Merge(touchInputProvider.OnDragStart)
                .Merge(touchInputProvider.OnDragEnd)
                .Subscribe(HandleDrag)
                .AddTo(compositeDisposable);
            
            mouseInputProvider.OnPointerUp
                .Select(eventData =>
                {
                    eventData.Id = mouseId;
                    return eventData;
                })
                .Subscribe(HandleUp)
                .AddTo(compositeDisposable);

            touchInputProvider.OnPointerUp
                .Subscribe(HandleUp)
                .AddTo(compositeDisposable);
        }

        private void Disable()
        {
            compositeDisposable.Clear();
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