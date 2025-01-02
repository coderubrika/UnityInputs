using System;
using UniRx;

namespace Suburb.Inputs
{
    public class MouseSwipeCompositor : IPluginCompositor
    {
        private readonly MouseProvider mouseProvider;
        private readonly MouseResourceDistributor distributor;
        private readonly MouseButtonType buttonType;
        
        private readonly CompositeDisposable disposables = new();
        
        private GestureType gestureType = GestureType.None;
        private IPointerSession session;
        private SwipeMember member;
        
        public MouseSwipeCompositor(
            MouseProvider mouseProvider, 
            MouseResourceDistributor distributor, 
            MouseButtonType buttonType)
        {
            this.mouseProvider = mouseProvider;
            this.distributor = distributor;
            this.buttonType = buttonType;
        }

        public IResourceDistributor Distributor => distributor;

        public void Handle()
        {
            if (!distributor.CheckAvailabilityButton(buttonType)
                || !session.CheckIncludeInBounds(mouseProvider.Position))
                return;

            mouseProvider.OnMove
                .Subscribe(_ =>
                {
                    if (gestureType == GestureType.Down)
                    {
                        member.PutDragStart(mouseProvider.Delta);
                        gestureType = GestureType.Drag;
                        return;
                    }
                    
                    if (gestureType != GestureType.Drag)
                        return;
                    
                    member.PutDrag(mouseProvider.Delta);
                })
                .AddTo(disposables);

            mouseProvider.OnUp
                .Where(type => type == buttonType)
                .Subscribe(_ =>
                {
                    if (gestureType == GestureType.Drag)
                        member.PutDragEnd();
                    
                    gestureType = GestureType.None;
                    member.PutUp(mouseProvider.Position);
                    disposables.Clear();
                })
                .AddTo(disposables);
            
            if (session.IsBookResources)
                distributor.SetBookedButton(buttonType);
            
            gestureType = GestureType.Down;
            member.PutDown(mouseProvider.Position);
            
        }

        public bool CheckBusy()
        {
            return gestureType != GestureType.None;
        }

        public bool SetupSession(ISession session)
        {
            this.session = session as IPointerSession;

            if (session != null)
                member = session.GetMember<SwipeMember>();
            
            return this.session != null;
        }

        public void Reset()
        {
            session = null;
        }
    }
}