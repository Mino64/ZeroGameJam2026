using UnityEngine;

// Cooldown partilhado por todas as zonas de hover (monitor, trás, virar esquerda/direita).
// Depois de uma zona agir, todas ficam bloqueadas durante um instante, para evitar
// interações duplas (ex.: passar o rato por duas zonas coladas, ou uma zona que
// reaparece debaixo do rato quando o monitor abre/fecha).
public static class HoverZoneCooldown
{
    private static float _blockedUntil;

    public static bool Ready => Time.unscaledTime >= _blockedUntil;

    public static void Block(float seconds)
    {
        _blockedUntil = Mathf.Max(_blockedUntil, Time.unscaledTime + seconds);
    }

    // Com "Enter Play Mode Options" (sem domain reload) o valor antigo ficava de uma sessão anterior
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay() => _blockedUntil = 0f;
}
