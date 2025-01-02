namespace Suburb.Inputs
{
    public abstract class TouchPluginCompositor : PluginCompositor<TouchResourceDistributor, IPointerSession, IInputPlugin>
    {
        protected readonly TouchProvider touchProvider;
        
        public TouchPluginCompositor(
            TouchResourceDistributor distributor,
            TouchProvider touchProvider) : base(distributor)
        {
            this.touchProvider = touchProvider;
        }
    }
}