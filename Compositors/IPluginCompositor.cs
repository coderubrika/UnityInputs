namespace Suburb.Inputs
{
    public interface IPluginCompositor
    {
        public IResourceDistributor Distributor { get; }
        public void Handle();
        public bool CheckBusy();
    }
}