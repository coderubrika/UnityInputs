using System;
using System.Collections.Generic;
using Suburb.Utils;
using UniRx;

namespace Suburb.Inputs
{
    public class LayerOrderer
    {
        private readonly HashSet<ISession> sessionsStore = new();
        private readonly Dictionary<IResourceDistributor, LinkedList<ISession>> distributorsSessionsStore = new();
        private readonly Dictionary<IResourceDistributor, (IDisposable Subscription, IDisposable Enabling)> distributorSubscriptions = new();

        public IDisposable ConnectFirst(ISession session)
        {
            if (!sessionsStore.Add(session))
            {
                this.LogError("Session already connected");
                return Disposable.Empty;
            }

            IDisposable addDisposable = session.OnDistributorAdded
                .Subscribe(distributor => AddDistributorFromSession(session, distributor));
            
            IDisposable removeDisposable = session.OnDistributorRemoved
                .Subscribe(distributor => RemoveDistributorFromSession(session, distributor));
            
            foreach (var distributor in session.GetResourceDistributors())
                AddDistributorFromSession(session, distributor);
            
            return Disposable.Create(() =>
            {
                addDisposable.Dispose();
                removeDisposable.Dispose();
                sessionsStore.Remove(session);
                foreach (var distributor in session.GetResourceDistributors())
                    RemoveDistributorFromSession(session, distributor);
            });
        }

        private void AddDistributorFromSession(ISession session, IResourceDistributor distributor)
        {
            if (distributorsSessionsStore.TryGetValue(distributor, out var sessionsInDistributor))
                sessionsInDistributor.AddFirst(session);
            else
            {
                var newList = new LinkedList<ISession>();
                newList.AddFirst(session);
                distributorsSessionsStore.Add(distributor, newList);

                if (distributorSubscriptions.ContainsKey(distributor)) 
                    return;
                    
                IDisposable disposable = distributor.OnAppearResources
                    .Subscribe(_ => HandleSessions(distributor));
                distributorSubscriptions.Add(distributor, (disposable, distributor.Enable()));
            }
        }

        private void RemoveDistributorFromSession(ISession session, IResourceDistributor distributor)
        {
            var sessionsInDistributor = distributorsSessionsStore[distributor];
            sessionsInDistributor.Remove(session);
            if (sessionsInDistributor.Count > 0)
                return;
                    
            distributorsSessionsStore.Remove(distributor);
            var disposables = distributorSubscriptions[distributor];
            disposables.Subscription.Dispose();
            disposables.Enabling.Dispose();
            distributorSubscriptions.Remove(distributor);
        }

        private void HandleSessions(IResourceDistributor distributor)
        {
            LinkedList<ISession> sessions = distributorsSessionsStore[distributor];
            var nextNode = sessions.First;
            while (nextNode != null)
            {
                var session = nextNode.Value;
                if (!distributor.HaveResources)
                    break;
                
                session.HandleResources(distributor);
                
                if (session.IsPreventNext)
                    break;
                
                nextNode = nextNode.Next;
            }
        }
    }
}