using DG.Tweening;
using Suburb.Utils;
using UniRx;
using UnityEngine;

namespace Suburb.Inputs
{
    public class VirtualJoystick : MonoBehaviour
    {
        [SerializeField] private RectTransform joystickOrigin;
        [SerializeField] private RectTransform joystickHandler;
        [SerializeField] private CanvasGroup canvasGroup;
        
        private readonly CompositeDisposable compositeDisposable = new();
        
        private float rectRadius;
        private Vector2 currentPosition;
        private Vector2 currentDelta;
        
        public ReactiveProperty<(Vector2 Direction, float Force)> OnDirectionAndForce { get; } = new();
        
        public void Connect(SwipeGestureSession gestureSession)
        {
            canvasGroup.alpha = 0;
            rectRadius = joystickOrigin.sizeDelta.x / 2;
            gestureSession.OnDown
                .Subscribe(position =>
                {
                    joystickOrigin.position = position;
                    joystickHandler.position = position;
                    
                    currentPosition = position;
                    currentDelta = Vector2.zero;
                    OnDirectionAndForce.Value = (Vector2.zero, 0);
                    DOTween.Kill(canvasGroup);
                    canvasGroup.DOFade(1, 0.4f);
                })
                .AddTo(compositeDisposable);

            gestureSession.OnDrag
                .Subscribe(newDelta =>
                {
                    currentPosition += newDelta;
                    currentDelta += newDelta;

                    var anchoredPosition = joystickOrigin.InverseTransformPoint(currentPosition).To2();
                    float magnitude = anchoredPosition.magnitude;
                    Vector2 normAncPos = anchoredPosition / magnitude;
                    magnitude = magnitude > rectRadius ? rectRadius : magnitude;
                    anchoredPosition = normAncPos * magnitude;
                    joystickHandler.anchoredPosition = anchoredPosition;
                    OnDirectionAndForce.Value = (normAncPos, magnitude / rectRadius);

                    // for second variant in future
                    // joystickHandler.position += newDelta.To3();
                    // Vector2 handlerAnchoredPosition = joystickHandler.anchoredPosition;
                    // float anchoredPositionMagnitude = handlerAnchoredPosition.magnitude;
                    // Vector2 normalizedHandlerAnchoredPosition = handlerAnchoredPosition / anchoredPositionMagnitude;
                    //
                    // if (anchoredPositionMagnitude > rectRadius)
                    // {
                    //     Vector2 newHandlerAnchoredPosition = normalizedHandlerAnchoredPosition * rectRadius;
                    //     joystickHandler.anchoredPosition = newHandlerAnchoredPosition;
                    // }

                })
                .AddTo(compositeDisposable);
            
            gestureSession.OnUp
                .Subscribe(_ =>
                {
                    OnDirectionAndForce.Value = (Vector2.zero, 0);
                    DOTween.Kill(canvasGroup);
                    canvasGroup.DOFade(0, 0.4f);
                })
                .AddTo(compositeDisposable);
        }

        public void Disconnect()
        {
            compositeDisposable.Clear();
        }
    }
}