using System;
using System.Collections.Generic;
using Suburb.Utils;
using UniRx;

namespace Suburb.Inputs
{
    public class LayerOrderer
    {
        private readonly Dictionary<IResourceDistributor, LinkedList<ISession>> sessionsStore = new();
        private readonly Dictionary<IResourceDistributor, (IDisposable Subscription, IDisposable Enabling)> distributorSubscriptions = new();
        
        public IDisposable Connect(ISession session)
        {
            var distributors = session.GetResourceDistributors();
            IDisposable[] disposables = new IDisposable[distributors.Length];
            for (int i = 0; i < distributors.Length; i++)
            {
                var distributor = distributors[i];
                disposables[i] = AddSession(distributor, session);
            }
            return Disposable.Create(() => disposables.Dispose());
        }

        private IDisposable AddSession(IResourceDistributor distributor, ISession session)
        {
            SetupDistributor(distributor);
            var sessions = GetSessions(distributor);
            var node = sessions.AddFirst(session);
            
            return Disposable.Create(() =>
            {
                sessions.Remove(node);
                if (sessions.Count > 0)
                    return;

                var disposables = distributorSubscriptions[distributor];
                disposables.Subscription.Dispose();
                disposables.Enabling.Dispose();
                distributorSubscriptions.Remove(distributor);
                sessionsStore.Remove(distributor);
            });
        }

        private LinkedList<ISession> GetSessions(IResourceDistributor distributor)
        {
            if (sessionsStore.TryGetValue(distributor, out var sessions)) 
                return sessions;
            
            var list = new LinkedList<ISession>();
            sessionsStore.Add(distributor, list);
            return list;
        }
        
        private void SetupDistributor(IResourceDistributor distributor)
        {
            if (distributorSubscriptions.ContainsKey(distributor))
                return;
            
            IDisposable disposable = distributor.OnAppearResources
                .Subscribe(_ => HandleSessions(distributor));
            distributorSubscriptions.Add(distributor, (disposable, distributor.Enable()));
        }

        private void HandleSessions(IResourceDistributor distributor)
        {
            LinkedList<ISession> sessions = sessionsStore[distributor];
            
            foreach (var session in sessions)
            {
                if (!distributor.HaveResources)
                    break;
                
                session.HandleResources(distributor);
            }
        }
    }
}