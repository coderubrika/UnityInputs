using System;
using Suburb.Utils;
using UnityEngine.InputSystem;

namespace Suburb.Inputs
{
    public class KeyboardSession : DisposableObject
    {
        private readonly Func<KeyboardSession, Key, IObservable<bool>> eventsGetter;
        
        public KeyboardSession(Func<KeyboardSession, Key, IObservable<bool>> eventsGetter, Action onDispose)
        : base(onDispose)
        {
            this.eventsGetter = eventsGetter;
        }

        public IObservable<bool> OnKey(Key key)
        {
            return eventsGetter.Invoke(this, key);
        }
    }
}