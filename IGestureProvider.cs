using UniRx;

namespace Suburb.Inputs
{
    public interface IGestureProvider
    {
        public ReactiveCommand<GestureEventData> OnPointerDown { get; }
        public ReactiveCommand<GestureEventData> OnPointerUp { get; }
        public ReactiveCommand<GestureEventData> OnDragStart { get; }
        public ReactiveCommand<GestureEventData> OnDrag { get; }
        public ReactiveCommand<GestureEventData> OnDragEnd { get; }
        public void Disable();
        public void Enable();
    }
}
