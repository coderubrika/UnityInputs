using System;
using System.Collections.Generic;
using Suburb.Utils;
using UniRx;

namespace Suburb.Inputs
{
    public class PointerGestureConnector
    {
        // для этого коннектора все должно выглядеть именно так
        // тоесть должна быть еще одна сущность которая будет превращать
        // события касаний в события pointer тоесть должен быть провайдер
        // соответвующий но в чем соль как понять что мне нужно 2 в 1 в не 2 по отдельности
        // и как это совокупить
        /*
         * итак надо подойти к проблеме с конца решения
         * допустим у меня есть следующая ситуация
         * я хочу обрабатывать 1к драг в области 2к драг в области
         * zoom от мышки если указатель находится в области
         * zoom от 2х касаний в области вращение от 2х касаний
         * вращение от мыши с клавиатурой (колесико + задатая клавиша)
         *
         * и чтобы сессии работали вместе
         * как мать его мне все это организовать
         * простым коннектором дело не обойдется
         * мне нужны правила для всех + индивидуальность
         * с помощью которых можно обьединить работу
         * раз мы тут говорим о том что все эти ребята делять слои взаимодействия то
         * неизбезно мы приходим к менеждеру слоев который мы должны создать где нибудь и использовать
         * причем создание может происходить как вручную так и внедрением
         *
         * правило 1 последний добавленный в приоритете
         * правило 2 меденжер не должен знать о деталях обработки
         * вопрос что должен делать этот менержер? я не знаю бляха муха
         * но он отвечает за слои значит он должен что то перебирать но что
         * он не должен знать что то а что то должен знать
         * у меня есть сессии пока что допуспим даже SwipeGestureSession по сути сесиия
         * это штука которая работает с бизнес логикой и в эту сессию кладут события знающие
         * о возникновении соответсвующих событий сущности и именно про это шла речь
         * итак у нас есть источники исходных событий это провайдеры такие как Mouse, Keyboard, Touch
         * у нас есть сессии это штуки которые работают с бизнес логикой и с которыми работают некие неназванные
         * сущности может назвать их не знаю как назоем их SP но что делают SP
         * SP раз уж они генерят события для сессий они не знают с какой именно сессией они должны работать
         * потому что часто они могут работать с многими сессиями и встает вопрос как распределить
         * события от разных SP к разным сессиям да еще и соблюдая слойность
         *
         * нужен пример как то я хочу отметить что с SP не все так просто еще и потому что
         * напримет есть жесты down up drag от одного касания или от двух касание или еще как может от мышки
         * а может от джостика а хуй его знает вообще одно ясно что между так сказать CoreProvider и Session
         * может быть много промежуточных звеньев и моя задача это организовать
         */
        
        private readonly TouchInputProvider touchInputProvider;
        private readonly MouseInputProvider mouseInputProvider;

        private readonly HashSet<IGestureSession>[] eventsBySessions;
        private readonly LinkedList<IGestureSession> sessions = new();
        private readonly CompositeDisposable compositeDisposable = new();
        private readonly int mouseId;
        
        public PointerGestureConnector(
            TouchInputProvider touchInputProvider,
            MouseInputProvider mouseInputProvider)
        {
            this.touchInputProvider = touchInputProvider;
            this.mouseInputProvider = mouseInputProvider;

            mouseId = touchInputProvider.SupportedTouches;
            
            eventsBySessions = new HashSet<IGestureSession>[mouseId + 1];
            for (int i = 0; i <= mouseId; i++)
                eventsBySessions[i] = new HashSet<IGestureSession>();
        }
        
        public IDisposable Connect(IGestureSession gestureSession)
        {
            if (sessions.Count == 0)
                Enable();
            
            var node = sessions.AddFirst(gestureSession);
            return new DisposableObject(() =>
            {
                foreach (var gestureSessionSet in eventsBySessions)
                    gestureSessionSet.Remove(gestureSession);
                
                sessions.Remove(node);
                if (sessions.Count != 0)
                    return;
                Disable();
            });
        }

        private void Enable()
        {
            touchInputProvider.Enable()
                .AddTo(compositeDisposable);
            mouseInputProvider.Enable()
                .AddTo(compositeDisposable);
            
            mouseInputProvider.OnPointerDown
                .Select(eventData =>
                {
                    eventData.Id = mouseId;
                    return eventData;
                })
                .Subscribe(HandleDown)
                .AddTo(compositeDisposable);
            //
            // touchInputProvider.OnDown
            //     .Subscribe(HandleDown)
            //     .AddTo(compositeDisposable);
            //
            mouseInputProvider.OnDrag
                .Merge(mouseInputProvider.OnDragStart)
                .Merge(mouseInputProvider.OnDragEnd)
                .Select(eventData =>
                {
                    eventData.Id = mouseId;
                    return eventData;
                })
                .Subscribe(HandleDrag)
                .AddTo(compositeDisposable);
            //
            // touchInputProvider.OnDrag
            //     .Merge(touchInputProvider.OnDragStart)
            //     .Merge(touchInputProvider.OnDragEnd)
            //     .Subscribe(HandleDrag)
            //     .AddTo(compositeDisposable);
            //
            mouseInputProvider.OnPointerUp
                .Select(eventData =>
                {
                    eventData.Id = mouseId;
                    return eventData;
                })
                .Subscribe(HandleUp)
                .AddTo(compositeDisposable);
            //
            // touchInputProvider.OnUp
            //     .Subscribe(HandleUp)
            //     .AddTo(compositeDisposable);
        }

        private void Disable()
        {
            compositeDisposable.Clear();
        }

        private void HandleDown(PointerEventData eventData)
        {
            var node = sessions.First;
            while (node != null)
            {
                var session = node.Value;
                if (!session.Contain(eventData.Position))
                {
                    node = node.Next;
                    continue;
                }
                
                node = node.Next;
                
                eventsBySessions[eventData.Id].Add(session);
                session.PutDown(eventData.Position);
                
                if (session.IsBlockOther)
                    break;
            }
        }

        private void HandleDrag(PointerEventData eventData)
        {
            foreach (var session in eventsBySessions[eventData.Id])
                session.PutDrag(eventData.Delta);
        }
        
        private void HandleUp(PointerEventData eventData)
        {
            foreach (var session in eventsBySessions[eventData.Id])
                session.PutUp(eventData.Position);
            
            eventsBySessions[eventData.Id].Clear();
        }
    }
}