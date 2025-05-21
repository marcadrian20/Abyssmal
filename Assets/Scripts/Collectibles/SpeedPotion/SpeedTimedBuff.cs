using UnityEngine;

public class SpeedTimedBuff : TimedBuff
{
    private PlayerMovement movement;
    private float originalMoveSpeed;
    private float originalAirMoveSpeed;

    public SpeedTimedBuff(ScriptableBuff buff, GameObject obj) : base(buff, obj)
    {
        Duration = buff.Duration;
        movement = obj.GetComponent<PlayerMovement>();
    }

    protected override void ApplyEffect()
    {
        if (movement != null && EffectStacks == 0)
        {
            var speedBuff = (ScriptableSpeedBuff)Buff;
            // Use reflection or expose moveSpeed/airMoveSpeed as public if needed
            var moveSpeedField = typeof(PlayerMovement).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var airMoveSpeedField = typeof(PlayerMovement).GetField("airMoveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            originalMoveSpeed = (float)moveSpeedField.GetValue(movement);
            originalAirMoveSpeed = (float)airMoveSpeedField.GetValue(movement);

            moveSpeedField.SetValue(movement, originalMoveSpeed * speedBuff.speedMultiplier);
            airMoveSpeedField.SetValue(movement, originalAirMoveSpeed * speedBuff.speedMultiplier);
        }
    }

    public override void End()
    {
        if (movement != null)
        {
            var moveSpeedField = typeof(PlayerMovement).GetField("moveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var airMoveSpeedField = typeof(PlayerMovement).GetField("airMoveSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            moveSpeedField.SetValue(movement, originalMoveSpeed);
            airMoveSpeedField.SetValue(movement, originalAirMoveSpeed);
        }
    }
}