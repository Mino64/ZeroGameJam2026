using System.Collections.Generic;
using UnityEngine;

public class Monster : MonoBehaviour
{
    private enum State { Wandering, Investigating, Approaching, Knocking }

    [SerializeField] private Room startRoom;
    [SerializeField] private Room playerRoom;
    [SerializeField] private OfficeEntrance[] entrances;
    [SerializeField] private Vector2 wanderInterval = new Vector2(6f, 12f);
    [SerializeField] private float moveInterval = 4f;
    [SerializeField] private float attackDelay = 3f;
    [SerializeField] private float noiseReactionTime = 0.5f;

    [Header("Início")]
    [Tooltip("O monstro fica parado na sala inicial até o jogador olhar para essa câmara uma vez.")]
    [SerializeField] private bool waitForFirstLook = true;
    [SerializeField] private CameraController cams;
    [SerializeField] private MonitorController monitor;

    [Header("Comportamento")]
    [SerializeField, Range(0f, 1f)] private float noiseSuccessChance = 0.8f;
    [SerializeField, Range(0f, 1f)] private float approachChance = 0.3f;
    [SerializeField, Range(0f, 1f)] private float approachGiveUpChance = 0.15f;

    [Header("Porta fechada")]
    [SerializeField] private int knockCount = 3;
    [SerializeField] private float knockInterval = 0.8f;

    private Room current;
    private State state = State.Wandering;
    private Queue<Room> path = new Queue<Room>();
    private float timer;
    private float attackTimer;
    private bool attacking;
    private bool waiting;

    private OfficeEntrance knockEntrance;
    private Room knockRoom;
    private int knocksLeft;

    public static event System.Action OnMonsterMoved;
    public static event System.Action OnPlayerAttacked;

    private void OnEnable() => NoiseSystem.OnNoiseMade += HandleNoise;
    private void OnDisable() => NoiseSystem.OnNoiseMade -= HandleNoise;

    private void Start()
    {
        MoveTo(startRoom);
        ResetTimer(wanderInterval.x, wanderInterval.y);

        if (cams == null) cams = FindFirstObjectByType<CameraController>();
        if (monitor == null) monitor = FindFirstObjectByType<MonitorController>();

        // Sem câmaras ou monitor na cena não há como o jogador olhar: não espera
        waiting = waitForFirstLook && cams != null && monitor != null;
    }

    // As câmaras só estão visíveis com o monitor aberto e fora da transição de abertura
    private bool PlayerIsLookingAtMonster() =>
        current != null && monitor.IsOpen && !monitor.InTransition && cams.CurrentRoom == current;

    private void Update()
    {
        if (waiting)
        {
            if (!PlayerIsLookingAtMonster()) return;

            waiting = false;
            ResetTimer(wanderInterval.x, wanderInterval.y);
        }

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

        switch (state)
        {
            case State.Wandering: UpdateWandering(); break;
            case State.Investigating: UpdateInvestigating(); break;
            case State.Approaching: UpdateApproaching(); break;
            case State.Knocking: UpdateKnocking(); break;
        }
    }

    private void UpdateWandering()
    {
        if (playerRoom != null && Random.value < approachChance && BeginApproach()) return;

        Room[] neighbors = current.Neighbors;
        if (neighbors != null && neighbors.Length > 0)
            Step(neighbors[Random.Range(0, neighbors.Length)]);

        if (state != State.Knocking) ResetTimer(wanderInterval.x, wanderInterval.y);
    }

    private void UpdateInvestigating()
    {
        if (path.Count > 0) AdvanceAlongPath();
        else GoWander();
    }

    private void UpdateApproaching()
    {
        if (path.Count == 0 || Random.value < approachGiveUpChance)
        {
            GoWander();
            return;
        }

        AdvanceAlongPath();
    }

    private void UpdateKnocking()
    {
        if (knocksLeft > 0)
        {
            knocksLeft--;
            knockEntrance.Knock();
            ResetTimer(knockInterval, knockInterval);
            return;
        }

        EndKnock();
    }

    private bool BeginApproach()
    {
        List<Room> route = FindPath(current, playerRoom);
        if (route.Count == 0) return false;

        path = new Queue<Room>(route);
        state = State.Approaching;
        AdvanceAlongPath();
        return true;
    }

    private void AdvanceAlongPath()
    {
        Step(path.Dequeue());
        if (state != State.Knocking) ResetTimer(moveInterval, moveInterval);
    }

    private void GoWander()
    {
        state = State.Wandering;
        ResetTimer(wanderInterval.x, wanderInterval.y);
    }

    private void HandleNoise(Room noiseRoom)
    {
        if (waiting || current == null || noiseRoom == current) return;

        if (Random.value > noiseSuccessChance) return;

        path = new Queue<Room>(FindPath(current, noiseRoom));
        state = State.Investigating;

        float reaction = current == playerRoom ? noiseReactionTime : moveInterval;
        ResetTimer(reaction, reaction);
    }

    private void Step(Room next)
    {
        OfficeEntrance closed = ClosedEntrance(current, next);
        if (closed != null) StartKnock(closed);
        else MoveTo(next);
    }

    private OfficeEntrance ClosedEntrance(Room from, Room to)
    {
        if (to != playerRoom || entrances == null) return null;

        foreach (OfficeEntrance e in entrances)
            if (e != null && e.FromRoom == from && e.IsClosed) return e;

        return null;
    }

    private void StartKnock(OfficeEntrance entrance)
    {
        knockEntrance = entrance;
        knockRoom = current;
        knocksLeft = knockCount;

        attacking = false;
        path.Clear();

        current.MonsterExited();
        current = null;
        state = State.Knocking;
        ResetTimer(knockInterval, knockInterval);

        OnMonsterMoved?.Invoke();
    }

    private void EndKnock()
    {
        Room back = knockRoom;
        knockEntrance = null;
        knockRoom = null;

        MoveTo(back);
        OnMonsterMoved?.Invoke();
        GoWander();
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
                if (n == playerRoom && n != to) continue;

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