using System.Collections.Generic;
using System;
using UniRx;

namespace Suburb.Inputs
{
    public abstract class PluginCompositor<TResourceDistributor, TSession> : IPluginCompositor
        where TResourceDistributor : IResourceDistributor
        where TSession : ISession
    {
        protected readonly TResourceDistributor distributor;
        protected readonly TSession session;

        protected readonly LinkedList<IInputPlugin> plugins = new();

        public IResourceDistributor Distributor => distributor;
        
        protected PluginCompositor(TResourceDistributor distributor, TSession session)
        {
            this.distributor = distributor;
            this.session = session;
        }

        public virtual IDisposable Link<TMember>(IInputPlugin plugin)
            where TMember : class, new()
        {
            if (!plugin.SetReceiver(session.GetMember<TMember>()) || !plugin.SetSender(this)) 
                return Disposable.Empty;
            
            var node = plugins.AddFirst(plugin);
            return Disposable.Create(() =>
            {
                node.Value.Unlink();
                plugins.Remove(node);
            });
        }

        public abstract void Handle();

        public abstract bool CheckBusy();
    }
}