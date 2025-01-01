namespace Suburb.Inputs
{
    public interface ISession
    {
        public IResourceDistributor[] GetResourceDistributors();
        public void HandleResources(IResourceDistributor distributor);

        public bool IsBookResources { get; }
        
        public void SetBookResources(bool isBook);
        
        public TMember GetMember<TMember>()
            where TMember : class, new();
    }
}