using System;
using System.Collections.Generic;

namespace Suburb.Inputs
{
    public abstract class BaseSession : ISession
    {
        private readonly Dictionary<Type, object> members = new();
        
        public abstract IResourceDistributor[] GetResourceDistributors();

        public abstract void HandleResources(IResourceDistributor distributor);

        public TMember GetMember<TMember>() 
            where TMember : class, new()
        {
            Type memberType = typeof(TMember);
            if (members.TryGetValue(memberType, out object member))
                return member as TMember;
            
            return null;
        }
    }
}