using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

namespace Suburb.Inputs
{
    public class TwoTouchRotatePlugin : IInputPlugin
    {
        private readonly TouchProvider touchProvider;
        private readonly CompositeDisposable touchProviderDisposables = new();
        private readonly CompositeDisposable compositorDisposables = new();
        
        private OneTwoTouchPluginCompositor compositor;
        private RotateMember member;

        private Vector2 firstPosition;
        private Vector2 secondPosition;
        private float previousAngle;
        private float angle;
        
        public TwoTouchRotatePlugin(TouchProvider touchProvider)
        {
            this.touchProvider = touchProvider;
        }
        
        public bool SetSender(object sender)
        {
            compositor = sender as OneTwoTouchPluginCompositor;

            if (compositor == null)
                return false;

            compositor.First
                .Skip(1)
                .Subscribe(SetFirstTouchId)
                .AddTo(compositorDisposables);
            
            compositor.Second
                .Skip(1)
                .Subscribe(SetSecondTouchId)
                .AddTo(compositorDisposables);
            
            return true;
        }

        public bool SetReceiver(object candidate)
        {
            member = candidate as RotateMember;
            return member != null;
        }
        
        
        public void Unlink()
        {
            member = null;
            compositor = null;
            touchProviderDisposables.Clear();
            compositorDisposables.Clear();
        }

        private void SetFirstTouchId(int id)
        {
            if (id == -1)
            {
                firstPosition = Vector2.zero;
                secondPosition = Vector2.zero;
                previousAngle = 0;
                angle = 0;
                return;
            }

            if (id == compositor.PreviousSecond)
            {
                firstPosition = secondPosition;
                secondPosition = Vector2.zero;
                previousAngle = 0;
                angle = 0;
                return;
            }
            
            firstPosition = touchProvider.DownEvents.First(item => item.Id == id).Position;
        }
        
        private void SetSecondTouchId(int id)
        {
            if (id == -1)
            {
                touchProviderDisposables.Clear();
                return;
            }
            
            secondPosition = touchProvider.DownEvents.First(item => item.Id == id).Position;
            previousAngle = Vector2.SignedAngle(secondPosition - firstPosition, Vector2.right);
            
            touchProvider.OnDragStart
                .Subscribe(_ => Rotate(touchProvider.DragStartEvents))
                .AddTo(touchProviderDisposables);
            
            touchProvider.OnDrag
                .Subscribe(_ => Rotate(touchProvider.DragEvents))
                .AddTo(touchProviderDisposables);
        }

        private void Rotate(IEnumerable<PointerEventData> events)
        {
            var pointers = events
                .Where(item => item.Id == compositor.First.Value || item.Id == compositor.Second.Value)
                .ToArray();
            
            if (pointers.Length == 0)
                return;

            if (pointers.Length == 1)
            {
                var pointer = pointers[0];
                if (pointer.Id == compositor.First.Value)
                    firstPosition = pointer.Position;
                else
                    secondPosition = pointer.Position;
            }
            else
            {
                firstPosition = pointers.First(item => item.Id == compositor.First.Value).Position;
                secondPosition = pointers.First(item => item.Id == compositor.Second.Value).Position;
            }

            angle = Vector2.SignedAngle(secondPosition - firstPosition, Vector2.right);
            member.PutRotate(previousAngle - angle);
            previousAngle = angle;
        }
    }
}