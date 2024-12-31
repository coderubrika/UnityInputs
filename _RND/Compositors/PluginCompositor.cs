using System.Collections.Generic;
using System;
using Suburb.Utils;
using UniRx;

namespace Suburb.Inputs
{
    public abstract class PluginCompositor<TResourceDistributor, TSession> : IPluginCompositor
        where TResourceDistributor : IResourceDistributor
        where TSession : ISession
    {
        protected readonly TResourceDistributor distributor;

        protected readonly LinkedList<IInputPlugin> plugins = new();

        protected TSession session;
        
        public IResourceDistributor Distributor => distributor;
        
        protected PluginCompositor(TResourceDistributor distributor, TSession session)
        {
            this.distributor = distributor;
            this.session = session;
        }
        
        public IDisposable Link<TMember>(IInputPlugin plugin)
            where TMember : class, new()
        {
            if (!plugin.SetReceiver(session.GetMember<TMember>()))
            {
                this.LogError($"Can't link plugin typeOf '{plugin.GetType().Name}' " +
                              $"because it doesn't support the '{typeof(TMember).Name}' member");
                plugin.Unlink();
                return Disposable.Empty;
            }
            
            if (!plugin.SetSender(this))
            {
                this.LogError($"Can't link plugin typeOf '{plugin.GetType().Name}' " +
                              $"because it doesn't support the '{GetType().Name}' plugin compositor");
                plugin.Unlink();
                return Disposable.Empty;
            }
            
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