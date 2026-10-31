using System;
using UnityEngine;

public class Room : MonoBehaviour
{
    [SerializeField] private string roomName;
    [SerializeField] private Room[] neighbors;
    [SerializeField] private AudioSource noiseSource;
    [SerializeField] private AudioClip[] noiseClips;

    [Header("Sprites")]
    [SerializeField] private Sprite emptySprite;
    [SerializeField] private Sprite monsterSprite;

    public string RoomName => roomName;
    public Room[] Neighbors => neighbors;
    public bool MonsterPresent { get; private set; }

    public event Action Changed;

    public virtual Sprite GetCurrentSprite() => MonsterPresent ? monsterSprite : emptySprite;

    private int lastNoiseIndex = -1;

    public void PlayNoise()
    {
        if (noiseSource == null) return;

        if (noiseClips == null || noiseClips.Length == 0)
        {
            noiseSource.Play();
            return;
        }

        int i = UnityEngine.Random.Range(0, noiseClips.Length);
        if (noiseClips.Length > 1)
            while (i == lastNoiseIndex) i = UnityEngine.Random.Range(0, noiseClips.Length);
        lastNoiseIndex = i;

        if (noiseClips[i] != null) noiseSource.PlayOneShot(noiseClips[i]);
    }

    public void MonsterEntered()
    {
        MonsterPresent = true;
        OnMonsterEntered();
        Changed?.Invoke();
    }

    public void MonsterExited()
    {
        MonsterPresent = false;
        OnMonsterExited();
        Changed?.Invoke();
    }
    protected void NotifyChanged() => Changed?.Invoke();

    protected virtual void OnMonsterEntered() { }
    protected virtual void OnMonsterExited() { }
}