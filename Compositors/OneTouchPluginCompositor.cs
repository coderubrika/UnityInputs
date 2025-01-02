using System;
using System.Linq;
using UniRx;
using UnityEngine;

namespace Suburb.Inputs
{
    public class OneTouchPluginCompositor : TouchPluginCompositor
    {
        private IDisposable upDisposable;
        
        public ReactiveProperty<int> Id { get; } = new(-1);
        public int PreviousId { get; private set; } = -1;
        
        public OneTouchPluginCompositor(
            TouchResourceDistributor distributor, 
            TouchProvider touchProvider) : 
            base(distributor, touchProvider)
        {
        }

        public override void Handle()
        {
            if (Id.Value != -1)
                return;

            var pointer = distributor
                .GetAvailableResources()
                .FirstOrDefault(pointer => Session.CheckIncludeInBounds(pointer.Position));
            
            if (pointer == null)
                return;
            
            if (Session.IsBookResources)
                distributor.SetBookedResources(new[]{pointer.Id});
            
            PreviousId = Id.Value;
            Id.Value = pointer.Id;
            upDisposable?.Dispose();
            upDisposable = touchProvider.OnUp
                .Subscribe(_ => UpHandler());
        }

        private void UpHandler()
        {
            var pointer = touchProvider.UpEvents
                .FirstOrDefault(item => item.Id == Id.Value);
            
            if (pointer == null)
                return;

            PreviousId = Id.Value;
            Id.Value = -1;
            upDisposable?.Dispose();
        }
        
        public override bool CheckBusy()
        {
            return Id.Value != -1;
        }

        public override void Reset()
        {
            base.Reset();
            PreviousId = Id.Value;
            Id.Value = -1;
            upDisposable?.Dispose();
        }
    }
}