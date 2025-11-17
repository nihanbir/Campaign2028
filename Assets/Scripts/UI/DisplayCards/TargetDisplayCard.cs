using UnityEngine;

public struct TargetDisplayCard
{
    public StateDisplayCard state;
    public InstitutionDisplayCard inst;

    // public bool HasAny => state != null || inst != null;
    public bool IsNull() => state == null && inst == null;

    public void Clear()
    {
        if (state != null)
            Object.Destroy(state.gameObject);

        if (inst != null)
            Object.Destroy(inst.gameObject);

        state = null;
        inst = null;
    }

    public bool IsTarget(Card card)
    {
        if (state && state.GetCard() == card) return true;
        if (inst && inst.GetCard() == card) return true;
        return false;
    }

    public Transform Transform =>
        (state != null ? state.transform :
            inst  != null ? inst.transform : null);
    
    public GameObject GameObject =>
        state != null ? state.gameObject :
        inst  != null ? inst.gameObject :
        null;
}