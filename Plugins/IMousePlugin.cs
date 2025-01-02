namespace Suburb.Inputs
{
    public interface IMousePlugin : IInputPlugin
    {
        public bool CheckBusy();

        public void TakeResources();
    }
}