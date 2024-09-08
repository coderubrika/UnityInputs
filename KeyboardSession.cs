using System;
using UniRx;
using UnityEngine.InputSystem;

namespace Suburb.Inputs
{
    public class KeyboardSession
    {
        private readonly KeyboardProvider keyboardProvider;

        public KeyboardSession(KeyboardProvider keyboardProvider)
        {
            this.keyboardProvider = keyboardProvider;
        }

        public IDisposable Connect()
        {
            return keyboardProvider.Connect(this);
        }

        public IObservable<bool> OnKey(Key key)
        {
            return keyboardProvider.SubscribeSessionOnKey(this, key);
        }
    }
}