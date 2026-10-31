using System;
using UnityEngine;

public class NoiseSystem : MonoBehaviour
{
    public static event Action<Room> OnNoiseMade;

    [SerializeField] private float cooldown = 5f;

    private float nextAllowedTime;

    public bool CanMakeNoise => Time.time >= nextAllowedTime;
    public void MakeNoise(Room room, bool force = false, bool silent = false)
    {
        if ((!force && !CanMakeNoise) || room == null) return;
        nextAllowedTime = Time.time + cooldown;

        if (!silent) room.PlayNoise();
        OnNoiseMade?.Invoke(room);
    }
}