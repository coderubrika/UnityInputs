using System;
using System.Linq;
using UniRx;
using UnityEngine;

namespace Suburb.Inputs
{
    public class OneTouchPluginCompositor : PluginCompositor<TouchResourceDistributor, IPointerSession>
    {
        private readonly TouchProvider touchProvider;
        
        private IDisposable upDisposable;
        
        public ReactiveProperty<int> Id { get; } = new(-1);
        public int PreviousId { get; private set; } = -1;
        
        public OneTouchPluginCompositor(
            TouchResourceDistributor distributor, 
            TouchProvider touchProvider,
            IPointerSession session) : 
            base(distributor, session)
        {
            this.touchProvider = touchProvider;
        }

        public override void Handle()
        {
            if (Id.Value != -1)
                return;

            var pointer = distributor
                .GetAvailableResources()
                .FirstOrDefault(pointer => session.CheckIncludeInBounds(pointer.Position));
            
            if (pointer == null)
                return;
            
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
    }
}