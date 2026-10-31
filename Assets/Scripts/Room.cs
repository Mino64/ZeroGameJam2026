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

    public virtual Sprite GetCurrentSprite()=> MonsterPresent ? monsterSprite : emptySprite;

    public void PlayNoise()
    {
        if (noiseSource == null || noiseClips == null || noiseClips.Length == 0) return;
        noiseSource.PlayOneShot(noiseClips[UnityEngine.Random.Range(0, noiseClips.Length)]);
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