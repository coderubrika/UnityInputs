using System;
using System.Collections.Generic;
using System.Linq;
using Suburb.Utils;
using UniRx;

namespace Suburb.Inputs
{
    public class TouchResourceDistributor : IResourceDistributor
    {
        private readonly TouchProvider touchProvider;
        private readonly CompositeDisposable disposables = new();
        private readonly Dictionary<int, PointerEventData> availableResources = new();
        
        private int usersCount;

        public ReactiveCommand OnAppearResources { get; } = new();
        public bool HaveResources => availableResources.Values.FirstOrDefault(item => item != null) != null;

        public TouchResourceDistributor(TouchProvider touchProvider)
        {
            this.touchProvider = touchProvider;
        }
        
        public IEnumerable<PointerEventData> GetAvailableResources()
        {
            return availableResources.Values
                .FilterNull();
        }

        public void SetBookedResources(IEnumerable<int> ids)
        {
            foreach (int id in ids)
                availableResources[id] = null;
        }
        
        public IDisposable Enable()
        {
            if (usersCount == 0)
            {
                touchProvider.Enable()
                    .AddTo(disposables);

                touchProvider.OnDown
                    .Subscribe(_ =>
                    {
                        availableResources.Fill(null);
                        foreach (var appearEvent in touchProvider.DownEvents)
                            availableResources.AddOrReplace(appearEvent.Id, appearEvent);
                        OnAppearResources.Execute();
                    })
                    .AddTo(disposables);
            }

            usersCount += 1;
            return Disposable.Create(Disable);
        }

        private void Disable()
        {
            if (usersCount == 0)
                return;
            
            usersCount -= 1;

            if (usersCount > 0)
                return;
            
            availableResources.Clear();
            disposables.Clear();
        }
    }
}