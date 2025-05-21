using UnityEngine;

public class BlindnessTimedBuff : TimedBuff
{
    private BlindnessOverlay overlay;

    public BlindnessTimedBuff(ScriptableBuff buff, GameObject obj) : base(buff, obj)
    {
        Duration = buff.Duration;
        overlay = Object.FindFirstObjectByType<BlindnessOverlay>();
    }

    protected override void ApplyEffect()
    {
        if (overlay != null)
            overlay.ShowBlindness(Duration);
    }

    public override void End()
    {
        if (overlay != null)
            overlay.HideBlindness();
    }
    
}