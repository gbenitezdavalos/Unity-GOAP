using System;
using UnityEngine;

public interface IReservable {
    bool IsFree { get; }
    void Reserve();
    void Release();
}

public class PrinterStatus : MonoBehaviour, IReservable {
    public bool IsFree { get; private set; } = true;
    public event Action OnStatusChanged = delegate { };

    public void Reserve() {
        if (!IsFree) return;
        IsFree = false;
        OnStatusChanged.Invoke();
    }

    public void Release() {
        if (IsFree) return;
        IsFree = true;
        OnStatusChanged.Invoke();
    }
}