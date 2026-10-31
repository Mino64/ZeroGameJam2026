using System.Collections.Generic;
using UnityEngine;

public class Monster : MonoBehaviour
{
    private enum State { Wandering, Investigating }

    [SerializeField] private Room startRoom;
    [SerializeField] private Room playerRoom;
    [SerializeField] private Vector2 wanderInterval = new Vector2(6f, 12f);
    [SerializeField] private float moveInterval = 4f;
    [SerializeField] private float attackDelay = 3f;
    [SerializeField] private float noiseReactionTime = 0.5f;
    private Room current;
    private State state = State.Wandering;
    private Queue<Room> path = new Queue<Room>();
    private float timer;
    private float attackTimer;
    private bool attacking;

    public static event System.Action OnMonsterMoved;
    public static event System.Action OnPlayerAttacked;

    private void OnEnable() => NoiseSystem.OnNoiseMade += HandleNoise;
    private void OnDisable() => NoiseSystem.OnNoiseMade -= HandleNoise;

    private void Start()
    {
        MoveTo(startRoom);
        ResetTimer(wanderInterval.x, wanderInterval.y);
    }

    private void Update()
    {
        if (attacking)
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attacking = false;
                OnPlayerAttacked?.Invoke();
                return;
            }
        }

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        if (state == State.Wandering)
        {
            Room[] neighbors = current.Neighbors;
            if (neighbors != null && neighbors.Length > 0)
                MoveTo(neighbors[Random.Range(0, neighbors.Length)]);

            ResetTimer(wanderInterval.x, wanderInterval.y);
        }
        else
        {
            if (path.Count > 0)
            {
                MoveTo(path.Dequeue());
                ResetTimer(moveInterval, moveInterval);
            }
            else
            {
                state = State.Wandering;
                ResetTimer(wanderInterval.x, wanderInterval.y);
            }
        }
    }

    private void HandleNoise(Room noiseRoom)
    {
        if (current == null || noiseRoom == current) return;
        path = new Queue<Room>(FindPath(current, noiseRoom));
        state = State.Investigating;

        float reaction = current == playerRoom ? noiseReactionTime : moveInterval;
        ResetTimer(reaction, reaction);
    }

    private void MoveTo(Room room)
    {
        bool hadRoom = current != null;
        if (hadRoom) current.MonsterExited();

        current = room;
        current.MonsterEntered();

        if (hadRoom) OnMonsterMoved?.Invoke();

        attacking = room == playerRoom;
        if (attacking) attackTimer = attackDelay;
    }

    private void ResetTimer(float min, float max) => timer = Random.Range(min, max);
    private List<Room> FindPath(Room from, Room to)
    {
        Dictionary<Room, Room> prev = new Dictionary<Room, Room>();
        Queue<Room> q = new Queue<Room>();
        q.Enqueue(from);
        prev[from] = null;

        while (q.Count > 0)
        {
            Room r = q.Dequeue();
            if (r == to) break;
            foreach (Room n in r.Neighbors)
            {
                if (n != null && !prev.ContainsKey(n))
                {
                    prev[n] = r;
                    q.Enqueue(n);
                }
            }
        }

        List<Room> result = new List<Room>();
        if (!prev.ContainsKey(to)) return result;
        for (Room r = to; r != from; r = prev[r]) result.Insert(0, r);
        return result;
    }
}