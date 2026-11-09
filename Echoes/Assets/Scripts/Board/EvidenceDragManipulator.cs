using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Captura o ponteiro sem retirar o cartão de sua hierarquia durante o arrasto.</summary>
public class EvidenceDragManipulator : PointerManipulator
{
    private readonly Action<Vector2> begin;
    private readonly Action<Vector2> move;
    private readonly Action<Vector2> drop;
    private readonly Action cancel;
    private readonly Action click;
    private Vector2 start;
    private int pointer = -1;
    private bool dragging;

    public EvidenceDragManipulator(Action<Vector2> begin, Action<Vector2> move,
        Action<Vector2> drop, Action cancel, Action click)
    {
        this.begin = begin; this.move = move; this.drop = drop; this.cancel = cancel; this.click = click;
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerDownEvent>(Down);
        target.RegisterCallback<PointerMoveEvent>(Move);
        target.RegisterCallback<PointerUpEvent>(Up);
        target.RegisterCallback<PointerCancelEvent>(Cancel);
        target.RegisterCallback<PointerCaptureOutEvent>(CaptureOut);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerDownEvent>(Down);
        target.UnregisterCallback<PointerMoveEvent>(Move);
        target.UnregisterCallback<PointerUpEvent>(Up);
        target.UnregisterCallback<PointerCancelEvent>(Cancel);
        target.UnregisterCallback<PointerCaptureOutEvent>(CaptureOut);
    }

    private void Down(PointerDownEvent e)
    {
        if (e.button != 0 || pointer >= 0 || e.target is Button) return;
        pointer = e.pointerId; start = e.position; dragging = false;
        target.CapturePointer(pointer); target.Focus(); e.StopPropagation();
    }

    private void Move(PointerMoveEvent e)
    {
        if (pointer != e.pointerId) return;
        if (!dragging && Vector2.Distance(start, e.position) > 6f) { dragging = true; begin(e.position); }
        if (dragging) move(e.position);
        e.StopPropagation();
    }

    private void Up(PointerUpEvent e)
    {
        if (pointer != e.pointerId || e.button != 0) return;
        bool wasDragging = dragging;
        pointer = -1; dragging = false;
        target.ReleasePointer(e.pointerId);
        if (wasDragging) drop(e.position); else click();
        e.StopPropagation();
    }

    private void Cancel(PointerCancelEvent e) => Reset();
    private void CaptureOut(PointerCaptureOutEvent e) { if (pointer == e.pointerId) Reset(); }
    private void Reset()
    {
        int old = pointer; pointer = -1;
        if (old >= 0 && target.HasPointerCapture(old)) target.ReleasePointer(old);
        if (dragging) cancel();
        dragging = false;
    }
}
