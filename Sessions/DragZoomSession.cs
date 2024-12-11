using System.Collections.Generic;
using UnityEngine;

namespace Suburb.Inputs
{
    public class DragZoomSession //: SwipeGestureSession
    {
        protected int SecondId;
        private Vector2 complexPosition;
        
        // public DragZoomSession(RectTransform bounds, RectTransform[] excludedRects)
        // : base(bounds, excludedRects)
        // {
        // }

        // public override bool Contain(Vector2 position)
        // {
        //     if (FirstId == -1)
        //         return base.Contain(position);
        //     
        //     if (SecondId == -1)
        //     {
        //         IsBlockOther = CheckBound(position);
        //         return IsBlockOther;
        //     }
        //
        //     IsBlockOther = false;
        //     return false;
        // }

        // public override void PutDown(PointerEventData eventData)
        // {
        //     // if (FirstId == -1)
        //     // {
        //     //     complexPosition = eventData.Position;
        //     //     base.PutDown(eventData);
        //     //     return;
        //     // }
        //
        //     SecondId = eventData.Id;
        // }

        // public override void PutDrag(PointerEventData eventData)
        // {
        //     if (SecondId == -1)
        //     {
        //         complexPosition += eventData.Delta;
        //         base.PutDrag(eventData);
        //         return;
        //     }
        //
        //     //Vector2 complexDelta = (First.Delta + Second.Delta) * 0.5f;
        //     //complexPosition += complexDelta;
        //     //OnDrag.Execute(complexDelta);
        // }

        // public override void PutUp(PointerEventData eventData)
        // {
        //     // if (SecondId == -1)
        //     // {
        //     //     OnUp.Execute(complexPosition);
        //     //     FirstId = -1;
        //     //     return;
        //     // }
        //     
        //     //First = Second.
        //     
        // }
    }
}