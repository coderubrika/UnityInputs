namespace Suburb.Inputs
{
    public interface ISession
    {
        public IResourceDistributor[] GetResourceDistributors();
        public void HandleResources(IResourceDistributor distributor);
        
        public TMember GetMember<TMember>()
            where TMember : class, new();
    }
}