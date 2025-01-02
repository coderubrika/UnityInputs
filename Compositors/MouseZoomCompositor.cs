using System;
using UniRx;

namespace Suburb.Inputs
{
    public class MouseZoomCompositor : BaseMouseCompositor<ZoomMember, IPointerSession>
    {
        private IDisposable disposables;
        
        public MouseZoomCompositor(MouseProvider mouseProvider, MouseResourceDistributor distributor) : base(mouseProvider, distributor)
        {
            distributor.OnNext
                .Subscribe(_ =>
                {
                    disposables?.Dispose();
                    disposables = null;
                });
        }

        public override void Handle()
        {
            if (!distributor.CheckAvailabilityZoom()
                || !Session.CheckIncludeInBounds(mouseProvider.Position))
                return;
            
            if (Session.IsBookResources)
                distributor.BookZoom();

            disposables = mouseProvider.OnZoom
                .Where(_ => Session.CheckIncludeInBounds(mouseProvider.Position))
                .Subscribe(_ => Member.PutZoom(mouseProvider.Zoom, mouseProvider.Position));
        }
        
        public override bool CheckBusy()
        {
            return disposables != null;
        }

        public override void Reset()
        {
            base.Reset();
            disposables?.Dispose();
            disposables = null;
        }
    }
}