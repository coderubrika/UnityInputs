using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace Suburb.Inputs
{
    public class CompositorsSession : BaseSession
    {
        private readonly Dictionary<IResourceDistributor, LinkedList<IPluginCompositor>> compositorsStore = new();
        
        public IDisposable AddCompositor(IPluginCompositor compositor)
        {
            if (compositorsStore.TryGetValue(compositor.Distributor, out var compositors))
            {
                var node = compositors.AddFirst(compositor);
                return Disposable.Create(() =>
                {
                    compositors.Remove(node);
                    if (compositors.Count == 0)
                        compositorsStore.Remove(compositor.Distributor);
                });
            }

            var newCompositors = new LinkedList<IPluginCompositor>();
            var newNode = newCompositors.AddFirst(compositor);
            compositorsStore.Add(compositor.Distributor, newCompositors);

            return Disposable.Create(() =>
            {
                newCompositors.Remove(newNode);
                if (newCompositors.Count == 0)
                    compositorsStore.Remove(compositor.Distributor);
            });
        }
        
        public override IResourceDistributor[] GetResourceDistributors()
        {
            return compositorsStore.Keys.ToArray();
        }

        public override void HandleResources(IResourceDistributor distributor)
        {
            var compositors = compositorsStore[distributor];
            foreach (var compositor in compositors)
            {
                if (compositor.CheckBusy())
                    continue;
                
                compositor.Handle();
            }
        }
    }
}