using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>On-screen mobile driving buttons: shared state read by KartController.</summary>
public static class TouchControls
{
    public static bool Left, Right, Gas, Brake, Drift;
    public static void ResetAll() => Left = Right = Gas = Brake = Drift = false;
}

public class TouchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public string action;

    void Set(bool value)
    {
        switch (action)
        {
            case "left": TouchControls.Left = value; break;
            case "right": TouchControls.Right = value; break;
            case "gas": TouchControls.Gas = value; break;
            case "brake": TouchControls.Brake = value; break;
            case "drift": TouchControls.Drift = value; break;
        }
    }

    public void OnPointerDown(PointerEventData eventData) => Set(true);
    public void OnPointerUp(PointerEventData eventData) => Set(false);
    public void OnPointerExit(PointerEventData eventData) => Set(false);
    void OnDisable() => Set(false);
}
