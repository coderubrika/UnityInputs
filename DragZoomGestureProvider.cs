using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

namespace Suburb.Inputs
{
    public class DragZoomGestureProvider
    {
        private readonly MouseGestureProvider mouseGestureProvider;
        private readonly TouchGestureProvider touchGestureProvider;
        
        private readonly CompositeDisposable disposables = new();
        private readonly HashSet<DragZoomGestureSession> sessions = new();
        private readonly HashSet<int> activeTouches = new();

        private int currentTouchId = -1;
        private Vector2 touch0;
        private Vector2 touch1;
        private bool isBreakInput;
        
        public DragZoomGestureProvider(
            MouseGestureProvider mouseGestureProvider,
            TouchGestureProvider touchGestureProvider)
        {
            this.mouseGestureProvider = mouseGestureProvider;
            this.touchGestureProvider = touchGestureProvider;
        }

        // как следует модифицировать код
        // 1) конкретно в данном случае использования предполагаю
        // заполнение списка сессий по порядку
        // какую конкретно цель я преследую
        // вообще цель такая что мне нужно организовать использование нескольких
        // сессий одновременно
        // прикол в том что иногда я не знаю в каком порядке они должны отрабатывать если накладываются 
        // друг на друга
        // самый простой случай если мы вообще за этим не следим, но бывает что я просто активирую 2 сервиса
        // и тут лучше бы использовать заранее записанный приоритет но тогда эти сервисы должны типо знать друг о друге
        // и тогда вообще нет смысла в приоритетах
        // приоритеты заранее записанные херня и лучше их не использовать
        // приоритов вообще не должно быть должен быть только связный список с сессиями, мы вызываем их по очереди
        // мы имеем только одну активную сессию
        // если мы пользовались сессией и подключилась новая старая должна завершить свое выполнение
        
        
        public void Disable(DragZoomGestureSession session)
        {
            if (sessions.Remove(session))
                BreakInputForOneFrame();
            
            if (sessions.Count > 0)
                return;
            
            activeTouches.Clear();
            disposables.Clear();
            mouseGestureProvider.Disable();
            touchGestureProvider.Disable();
        }

        public void Enable(DragZoomGestureSession session)
        {
            if (!sessions.Add(session))
                return;
            
            BreakInputForOneFrame();
            
            if (sessions.Count > 1)
                return;
            
            mouseGestureProvider.OnPointerDown
                .Subscribe(data => Down(data.Position))
                .AddTo(disposables);
            
            mouseGestureProvider.OnPointerUp
                .Subscribe(data => Up(data.Position))
                .AddTo(disposables);

            mouseGestureProvider.OnZoom
                .Subscribe(data => Zoom(data.Zoom))
                .AddTo(disposables);
            
            mouseGestureProvider.OnDrag
                .Merge(mouseGestureProvider.OnDragStart)
                .Merge(mouseGestureProvider.OnDragEnd)
                .Subscribe(data => Drag(data.Delta, data.Position))
                .AddTo(disposables);

            touchGestureProvider.OnPointerDown
                .Subscribe(data =>
                {
                    PutTouch(data);
                    currentTouchId = data.Id;
                    activeTouches.Add(data.Id);
                    Down(data.Position);
                })
                .AddTo(disposables);
            
            touchGestureProvider.OnPointerUp
                .Subscribe(data =>
                {
                    PutTouch(data);
                    activeTouches.Remove(data.Id);
                    
                    if (currentTouchId != data.Id)
                        return;

                    Up(data.Position);
                    
                    currentTouchId = activeTouches.Count == 0 ? -1 : activeTouches.First();
                })
                .AddTo(disposables);
            
            touchGestureProvider.OnDragStart
                .Subscribe(data =>
                {
                    PutTouch(data);
                    
                    if (currentTouchId != data.Id)
                        return;

                    DragTouches(data.Delta);
                })
                .AddTo(disposables);

            touchGestureProvider.OnDrag
                .Subscribe(data =>
                {
                    PutTouch(data);
                    
                    if (currentTouchId != data.Id)
                        return;

                    DragTouches(data.Delta);
                })
                .AddTo(disposables);
            
            touchGestureProvider.OnDragEnd
                .Subscribe(data =>
                {
                    PutTouch(data);
                    
                    if (currentTouchId != data.Id)
                        return;
                    
                    DragTouches(data.Delta);
                })
                .AddTo(disposables);
            
            // touchGestureProvider.OnDragStartWithDoubleTouch
            //     .Subscribe(data =>
            //     {
            //         currentTouchId = data.Id;
            //         activeTouches.Add(data.Id);
            //         DragTouches(data.Delta);
            //         ZoomTouches(data.Zoom);
            //     })
            //     .AddTo(disposables);

            // touchGestureProvider.OnDragEndWithDoubleTouch
            //     .Subscribe(data =>
            //     {
            //         activeTouches.Remove(data.Id);
            //         currentTouchId = activeTouches.Count == 0 ? -1 : activeTouches.First();
            //         DragTouches(data.Delta);
            //     })
            //     .AddTo(disposables);
            
            // touchGestureProvider.OnDragWithDoubleTouch
            //     .Subscribe(data =>
            //     {
            //         DragTouches(data.Delta);
            //         ZoomTouches(data.Zoom);
            //     })
            //     .AddTo(disposables);
            
            mouseGestureProvider.Enable();
            touchGestureProvider.Enable();
        }
        
        private void Down(Vector2 position)
        {
            foreach (var session in sessions)
            {
                session.PutDown(position);
                if (isBreakInput)
                    break;
            }
        }
        
        private void Up(Vector2 position)
        {
            foreach (var session in sessions)
            {
                session.PutUp(position);
                if (isBreakInput)
                    break;
            }
        }

        private void Drag(Vector2 delta, Vector2 position)
        {
            foreach (var session in sessions)
            {
                session.PutDrag(delta, position);
                if (isBreakInput)
                    break;
            }
        }
        
        private void DragTouches(Vector2 delta)
        {
            foreach (var session in sessions)
            {
                session.PutDragTouches(delta, touch0, touch1);
                if (isBreakInput)
                    break;
            }
        }

        private void Zoom(float zoom)
        {
            foreach (var session in sessions)
            {
                session.PutZoom(zoom);
                if (isBreakInput)
                    break;
            }
        }
        
        // private void ZoomTouches(float zoom)
        // {
        //     foreach (var session in sessions)
        //     {
        //         session.PutZoomTouches(zoom, touch0, touch1);
        //         if (isBreakInput)
        //             break;
        //     }
        // }

        private void PutTouch(PointerEventData data)
        {
            if (data.Id == 0)
                touch0 = data.Position;
            else if (data.Id == 1)
                touch1 = data.Position;
        }

        private void BreakInputForOneFrame()
        {
            isBreakInput = true;
            Observable.NextFrame()
                .Subscribe(_ => isBreakInput = false);
        }
    }
}