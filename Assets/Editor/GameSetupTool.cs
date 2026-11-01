// GameSetupTool.cs  ->  colocar em Assets/Editor/
// Menu: Tools > FNaF Setup
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSetupWindow : EditorWindow
{
    private const string SpritesDir = "Assets/FnafSetup/Sprites";
    private const string PrefabsDir = "Assets/FnafSetup/Prefabs";

    // =====================================================================
    //  MAPA DE CÂMARAS  (edita aqui para mudar o mapa)
    //  - Posição: 0..1 dentro do painel do mapa (0,0 = canto inferior esquerdo)
    //  - O escritório é sempre o último nó (índice = número de salas)
    // =====================================================================

    private struct MapEntry
    {
        public string label;
        public Vector2 pos;
        public MapEntry(string label, float x, float y) { this.label = label; pos = new Vector2(x, y); }
    }

    private static readonly MapEntry[] MapRooms =
    {
        new MapEntry("1A", 0.50f, 1.00f), // 0  palco (o monstro começa aqui)
        new MapEntry("1B", 0.40f, 0.72f), // 1  sala de jantar
        new MapEntry("1C", 0.00f, 0.60f), // 2
        new MapEntry("2A", 0.25f, 0.38f), // 3  corredor esquerdo
        new MapEntry("2B", 0.25f, 0.08f), // 4  esquina esquerda
        new MapEntry("3",  0.00f, 0.30f), // 5  arrumos
        new MapEntry("4A", 0.75f, 0.38f), // 6  corredor direito
        new MapEntry("4B", 0.75f, 0.08f), // 7  esquina direita
        new MapEntry("5",  0.00f, 0.88f), // 8  bastidores
        new MapEntry("7",  1.00f, 0.62f), // 9  casas de banho
    };

    private static readonly Vector2 OfficeMapPos = new Vector2(0.50f, 0.00f);

    // Ligações entre salas (índices de MapRooms; MapRooms.Length = escritório)
    private static readonly (int a, int b)[] Links =
    {
        (0, 1), (0, 8), (1, 8), (1, 2), (1, 3), (1, 6), (1, 9),
        (2, 3), (3, 4), (3, 5), (5, 4), (6, 7), (6, 9),
        (4, 10), (7, 10), // 10 = escritório
    };

    // Salas que ficam "alteráveis" primeiro (as perto do escritório)
    private static readonly int[] AlterablePriority = { 4, 7, 3, 6, 5, 9, 8, 2, 1, 0 };

    private static readonly Vector2 MapSize = new Vector2(440f, 440f);
    private static readonly Vector2 MapButtonSize = new Vector2(80f, 44f);

    // =====================================================================

    // ---- Sons (guardados em EditorPrefs; campo vazio = sem som / não altera)
    private const string SoundPrefKey = "FnafSetup.Sounds";

    [SerializeField] private AudioClip[] noiseClips = new AudioClip[0];
    [SerializeField] private AudioClip cameraSwitchClip;
    [SerializeField] private AudioClip interferenceClip;
    [SerializeField] private AudioClip monitorOpenClip;
    [SerializeField] private AudioClip monitorCloseClip;
    [SerializeField] private AudioClip qteSuccessClip;
    [SerializeField] private AudioClip qteFailClip;
    [SerializeField] private AudioClip qteStepClip;
    [SerializeField] private AudioClip jumpscareClip;
    [SerializeField] private AudioClip knockClip;
    [SerializeField] private AudioClip doorOpenClip;
    [SerializeField] private AudioClip doorCloseClip;
    [SerializeField] private AudioClip backSafeClip;
    [SerializeField] private AudioClip backDangerousClip;
    [SerializeField] private AudioClip backKillClip;

    private class SoundSet
    {
        public AudioClip[] noise = new AudioClip[0];
        public AudioClip cameraSwitch;
        public AudioClip interference;
        public AudioClip monitorOpen, monitorClose, qteSuccess, qteFail, jumpscare, knock;
        public AudioClip qteStep;
        public AudioClip doorOpen, doorClose, backSafe, backDangerous, backKill;
    }

    [System.Serializable]
    private class SoundPaths
    {
        public string[] noise = new string[0];
        public string cameraSwitch = "";
        public string interference = "";
        public string monitorOpen = "", monitorClose = "", qteSuccess = "", qteFail = "", jumpscare = "", knock = "";
        public string qteStep = "";
        public string doorOpen = "", doorClose = "", backSafe = "", backDangerous = "", backKill = "";
    }

    private static string PathOf(Object o) => o != null ? AssetDatabase.GetAssetPath(o) : "";

    private static AudioClip LoadClip(string path) =>
        string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(path);

    private SoundSet CurrentSounds()
    {
        var list = new List<AudioClip>();
        if (noiseClips != null)
            foreach (var c in noiseClips) if (c != null) list.Add(c);

        return new SoundSet
        {
            noise = list.ToArray(),
            cameraSwitch = cameraSwitchClip,
            interference = interferenceClip,
            monitorOpen = monitorOpenClip,
            monitorClose = monitorCloseClip,
            qteSuccess = qteSuccessClip,
            qteFail = qteFailClip,
            qteStep = qteStepClip,
            jumpscare = jumpscareClip,
            knock = knockClip,
            doorOpen = doorOpenClip,
            doorClose = doorCloseClip,
            backSafe = backSafeClip,
            backDangerous = backDangerousClip,
            backKill = backKillClip,
        };
    }

    private void SaveSounds()
    {
        var s = CurrentSounds();
        var paths = new SoundPaths
        {
            noise = new string[s.noise.Length],
            cameraSwitch = PathOf(s.cameraSwitch),
            interference = PathOf(s.interference),
            monitorOpen = PathOf(s.monitorOpen),
            monitorClose = PathOf(s.monitorClose),
            qteSuccess = PathOf(s.qteSuccess),
            qteFail = PathOf(s.qteFail),
            qteStep = PathOf(s.qteStep),
            jumpscare = PathOf(s.jumpscare),
            knock = PathOf(s.knock),
            doorOpen = PathOf(s.doorOpen),
            doorClose = PathOf(s.doorClose),
            backSafe = PathOf(s.backSafe),
            backDangerous = PathOf(s.backDangerous),
            backKill = PathOf(s.backKill),
        };
        for (int i = 0; i < s.noise.Length; i++) paths.noise[i] = PathOf(s.noise[i]);
        EditorPrefs.SetString(SoundPrefKey, JsonUtility.ToJson(paths));
    }

    private static SoundSet LoadSavedSounds()
    {
        var set = new SoundSet();
        string json = EditorPrefs.GetString(SoundPrefKey, "");
        if (string.IsNullOrEmpty(json)) return set;

        var paths = JsonUtility.FromJson<SoundPaths>(json);
        if (paths == null) return set;

        var list = new List<AudioClip>();
        if (paths.noise != null)
            foreach (var p in paths.noise) { var c = LoadClip(p); if (c != null) list.Add(c); }

        set.noise = list.ToArray();
        set.cameraSwitch = LoadClip(paths.cameraSwitch);
        set.interference = LoadClip(paths.interference);
        set.monitorOpen = LoadClip(paths.monitorOpen);
        set.monitorClose = LoadClip(paths.monitorClose);
        set.qteSuccess = LoadClip(paths.qteSuccess);
        set.qteFail = LoadClip(paths.qteFail);
        set.qteStep = LoadClip(paths.qteStep);
        set.jumpscare = LoadClip(paths.jumpscare);
        set.knock = LoadClip(paths.knock);
        set.doorOpen = LoadClip(paths.doorOpen);
        set.doorClose = LoadClip(paths.doorClose);
        set.backSafe = LoadClip(paths.backSafe);
        set.backDangerous = LoadClip(paths.backDangerous);
        set.backKill = LoadClip(paths.backKill);
        return set;
    }

    private void OnEnable()
    {
        var saved = LoadSavedSounds();
        noiseClips = saved.noise;
        cameraSwitchClip = saved.cameraSwitch;
        interferenceClip = saved.interference;
        monitorOpenClip = saved.monitorOpen;
        monitorCloseClip = saved.monitorClose;
        qteSuccessClip = saved.qteSuccess;
        qteFailClip = saved.qteFail;
        qteStepClip = saved.qteStep;
        jumpscareClip = saved.jumpscare;
        knockClip = saved.knock;
        doorOpenClip = saved.doorOpen;
        doorCloseClip = saved.doorClose;
        backSafeClip = saved.backSafe;
        backDangerousClip = saved.backDangerous;
        backKillClip = saved.backKill;

        spriteSlots = LoadSavedSprites();
    }

    private const int MinGeneratedRooms = 2;
    private const int MaxGeneratedRooms = 20;
    private const int MaxPerRow = 4;   // salas por linha no mapa gerado

    private bool useCustomMap = false;
    private int totalRooms = 10;
    private int alterableRooms = 2;

    private SpriteSlots spriteSlots = new SpriteSlots();
    private Vector2 scroll;
    private bool showOfficeSprites = true;
    private bool showRoomSprites = true;
    private readonly bool[] roomFoldout = new bool[MaxGeneratedRooms];

    // Mapa usado pelo Build: o desenhado à mão (tabelas acima) ou um gerado para N salas
    private class MapLayout
    {
        public MapEntry[] rooms;
        public (int a, int b)[] links;
        public int[] alterablePriority;
        public Vector2 officePos;
    }

    private static MapLayout CustomLayout() => new MapLayout
    {
        rooms = MapRooms,
        links = Links,
        alterablePriority = AlterablePriority,
        officePos = OfficeMapPos,
    };

    // Gera um mapa para n salas com câmara (sem contar o escritório):
    //  - camada 0: palco (sala 0, onde o monstro começa)
    //  - camadas do meio: até MaxPerRow salas cada, com ligações laterais (alternativas/loops)
    //  - última camada: 2 salas que dão para o escritório (as duas entradas)
    //  - cada sala liga à sala mais próxima da camada seguinte (e vice-versa), por isso
    //    o mapa fica sempre ligado do palco ao escritório
    private static MapLayout GenerateLayout(int n)
    {
        n = Mathf.Clamp(n, 1, MaxGeneratedRooms);

        var layers = new List<List<int>> { new List<int> { 0 } };
        int next = 1, remaining = n - 1;
        if (remaining > 0)
        {
            int entrances = Mathf.Min(2, remaining);
            int middle = remaining - entrances;
            if (middle > 0)
            {
                int rows = Mathf.CeilToInt(middle / (float)MaxPerRow);
                for (int r = 0; r < rows; r++)
                {
                    int count = middle / rows + (r < middle % rows ? 1 : 0);
                    var row = new List<int>();
                    for (int k = 0; k < count; k++) row.Add(next++);
                    layers.Add(row);
                }
            }

            var last = new List<int>();
            for (int k = 0; k < entrances; k++) last.Add(next++);
            layers.Add(last);
        }
        layers.Add(new List<int> { n });   // escritório (índice = n)

        int layerCount = layers.Count;     // >= 2
        var rooms = new MapEntry[n];
        var xs = new float[n + 1];
        for (int j = 0; j < layerCount; j++)
        {
            var row = layers[j];
            int k = row.Count;
            float spacing = k > 1 ? Mathf.Min(0.34f, 0.9f / (k - 1)) : 0f;
            float y = 1f - j / (float)(layerCount - 1);
            for (int i = 0; i < k; i++)
            {
                int idx = row[i];
                float x = 0.5f + (i - (k - 1) * 0.5f) * spacing;
                xs[idx] = x;
                if (idx < n) rooms[idx] = new MapEntry((idx + 1).ToString(), x, y);
            }
        }

        var linkSet = new HashSet<(int a, int b)>();
        void Link(int a, int b) => linkSet.Add(a < b ? (a, b) : (b, a));
        int Nearest(int origin, List<int> candidates)
        {
            int best = candidates[0];
            foreach (int c in candidates)
                if (Mathf.Abs(xs[c] - xs[origin]) < Mathf.Abs(xs[best] - xs[origin])) best = c;
            return best;
        }

        for (int j = 0; j < layerCount - 1; j++)
        {
            foreach (int a in layers[j]) Link(a, Nearest(a, layers[j + 1]));
            foreach (int b in layers[j + 1]) Link(b, Nearest(b, layers[j]));
        }
        for (int j = 1; j < layerCount - 2; j++)          // só camadas do meio
            for (int i = 0; i + 1 < layers[j].Count; i++)
                Link(layers[j][i], layers[j][i + 1]);

        var links = new List<(int a, int b)>(linkSet);
        links.Sort();

        // Salas perto do escritório ficam alteráveis primeiro
        var priority = new List<int>();
        for (int j = layerCount - 2; j >= 0; j--) priority.AddRange(layers[j]);

        return new MapLayout
        {
            rooms = rooms,
            links = links.ToArray(),
            alterablePriority = priority.ToArray(),
            officePos = new Vector2(0.5f, 0f),
        };
    }

    // ---- Imagens (guardadas em EditorPrefs; campo vazio = placeholder ao criar / não altera ao aplicar)
    private const string SpritePrefKey = "FnafSetup.Sprites";

    // As imagens de uma sala: vazia, com o monstro, e as versões "alteradas" (só nas salas alteráveis)
    private class RoomSpriteSlot
    {
        public Sprite empty, monster, alteredEmpty, alteredMonster;
    }

    private class SpriteSlots
    {
        public RoomSpriteSlot[] rooms = new RoomSpriteSlot[0];

        // Escritório, vista da frente (é também a sala "Office" onde o monstro ataca)
        public Sprite officeFront, officeMonster;
        // Vistas laterais: fundo com a porta aberta e com a porta fechada
        public Sprite leftOpen, leftClosed, rightOpen, rightClosed;
        // Vista de trás: os 3 estados
        public Sprite backSafe, backDangerous, backKill;
        // Transição ao abrir o monitor: office 1, e office 2 a meio do tempo
        public Sprite monitorA, monitorB;
    }

    [System.Serializable]
    private class RoomSpritePaths
    {
        public string empty = "", monster = "", alteredEmpty = "", alteredMonster = "";
    }

    [System.Serializable]
    private class SpritePaths
    {
        public RoomSpritePaths[] rooms = new RoomSpritePaths[0];
        public string officeFront = "", officeMonster = "";
        public string leftOpen = "", leftClosed = "", rightOpen = "", rightClosed = "";
        public string backSafe = "", backDangerous = "", backKill = "";
        public string monitorA = "", monitorB = "";
    }

    // "caminho|nome": o nome distingue os sprites de uma spritesheet
    private static string SpriteKey(Sprite s) =>
        s != null ? AssetDatabase.GetAssetPath(s) + "|" + s.name : "";

    private static Sprite LoadSprite(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;

        int bar = key.LastIndexOf('|');
        string path = bar >= 0 ? key.Substring(0, bar) : key;
        string spriteName = bar >= 0 ? key.Substring(bar + 1) : "";
        if (string.IsNullOrEmpty(path)) return null;

        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite sp && sp.name == spriteName) return sp;

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void SaveSprites(SpriteSlots s)
    {
        var paths = new SpritePaths
        {
            rooms = new RoomSpritePaths[s.rooms.Length],
            officeFront = SpriteKey(s.officeFront),
            officeMonster = SpriteKey(s.officeMonster),
            leftOpen = SpriteKey(s.leftOpen),
            leftClosed = SpriteKey(s.leftClosed),
            rightOpen = SpriteKey(s.rightOpen),
            rightClosed = SpriteKey(s.rightClosed),
            backSafe = SpriteKey(s.backSafe),
            backDangerous = SpriteKey(s.backDangerous),
            backKill = SpriteKey(s.backKill),
            monitorA = SpriteKey(s.monitorA),
            monitorB = SpriteKey(s.monitorB),
        };

        for (int i = 0; i < s.rooms.Length; i++)
        {
            var r = s.rooms[i] ?? new RoomSpriteSlot();
            paths.rooms[i] = new RoomSpritePaths
            {
                empty = SpriteKey(r.empty),
                monster = SpriteKey(r.monster),
                alteredEmpty = SpriteKey(r.alteredEmpty),
                alteredMonster = SpriteKey(r.alteredMonster),
            };
        }

        EditorPrefs.SetString(SpritePrefKey, JsonUtility.ToJson(paths));
    }

    private static SpriteSlots LoadSavedSprites()
    {
        var s = new SpriteSlots();
        string json = EditorPrefs.GetString(SpritePrefKey, "");
        if (string.IsNullOrEmpty(json)) return s;

        var p = JsonUtility.FromJson<SpritePaths>(json);
        if (p == null) return s;

        if (p.rooms != null)
        {
            s.rooms = new RoomSpriteSlot[p.rooms.Length];
            for (int i = 0; i < p.rooms.Length; i++)
            {
                var r = p.rooms[i];
                s.rooms[i] = r == null
                    ? new RoomSpriteSlot()
                    : new RoomSpriteSlot
                    {
                        empty = LoadSprite(r.empty),
                        monster = LoadSprite(r.monster),
                        alteredEmpty = LoadSprite(r.alteredEmpty),
                        alteredMonster = LoadSprite(r.alteredMonster),
                    };
            }
        }

        s.officeFront = LoadSprite(p.officeFront);
        s.officeMonster = LoadSprite(p.officeMonster);
        s.leftOpen = LoadSprite(p.leftOpen);
        s.leftClosed = LoadSprite(p.leftClosed);
        s.rightOpen = LoadSprite(p.rightOpen);
        s.rightClosed = LoadSprite(p.rightClosed);
        s.backSafe = LoadSprite(p.backSafe);
        s.backDangerous = LoadSprite(p.backDangerous);
        s.backKill = LoadSprite(p.backKill);
        s.monitorA = LoadSprite(p.monitorA);
        s.monitorB = LoadSprite(p.monitorB);
        return s;
    }

    // Garante um slot por sala (nunca encolhe, para não perder o que já foi escolhido)
    private static SpriteSlots EnsureRoomSlots(SpriteSlots s, int count)
    {
        if (s.rooms.Length < count)
        {
            var arr = new RoomSpriteSlot[count];
            System.Array.Copy(s.rooms, arr, s.rooms.Length);
            s.rooms = arr;
        }

        for (int i = 0; i < s.rooms.Length; i++)
            if (s.rooms[i] == null) s.rooms[i] = new RoomSpriteSlot();

        return s;
    }

    private static RoomSpriteSlot SlotFor(SpriteSlots s, int roomIndex) =>
        s != null && s.rooms != null && roomIndex < s.rooms.Length ? s.rooms[roomIndex] : null;

    private static Sprite Or(Sprite custom, Sprite fallback) => custom != null ? custom : fallback;

    private static bool[] AlterableFlags(MapLayout layout, int alterableCount)
    {
        var flags = new bool[layout.rooms.Length];
        for (int k = 0; k < Mathf.Min(alterableCount, layout.alterablePriority.Length); k++)
            flags[layout.alterablePriority[k]] = true;
        return flags;
    }

    private static Sprite SpriteField(string label, Sprite value) =>
        (Sprite)EditorGUILayout.ObjectField(label, value, typeof(Sprite), false);

    // As imagens escolhidas na janela substituem os placeholders do escritório
    private static void ApplyOverrides(SpriteSet set, SpriteSlots s)
    {
        if (s == null) return;

        set.officeEmpty = Or(s.officeFront, set.officeEmpty);
        set.officeMonster = Or(s.officeMonster, set.officeMonster);
        set.leftOpen = Or(s.leftOpen, set.leftOpen);
        set.leftClosed = Or(s.leftClosed, set.leftClosed);
        set.rightOpen = Or(s.rightOpen, set.rightOpen);
        set.rightClosed = Or(s.rightClosed, set.rightClosed);
        set.backSafe = Or(s.backSafe, set.backSafe);
        set.backDangerous = Or(s.backDangerous, set.backDangerous);
        set.backKill = Or(s.backKill, set.backKill);
    }

    // Placeholder das vistas laterais: fundo liso com a "porta" no centro (escura se aberta,
    // castanha se fechada). A área clicável da porta fica exatamente em cima deste retângulo.
    private static Color DoorPixel(int x, int y, Color background, bool closed)
    {
        bool door = x >= 14 && x < 18 && y >= 4 && y < 13;
        if (!door) return background;
        return closed ? new Color(0.45f, 0.25f, 0.12f) : new Color(0.02f, 0.02f, 0.02f);
    }

    // ------------------------------------------------------------ Aplicar imagens a uma cena já feita

    // Campos vazios não alteram nada. As salas são encontradas pela ordem em que o setup as cria
    // (filhas de "Rooms"), por isso funciona mesmo que tenhas mudado o número de salas na janela.
    private static void ApplySpritesToScene(SpriteSlots s)
    {
        int roomCount = 0;

        var roomsRoot = GameObject.Find("Rooms");
        if (roomsRoot != null)
        {
            for (int i = 0; i < roomsRoot.transform.childCount; i++)
            {
                var room = roomsRoot.transform.GetChild(i).GetComponent<Room>();
                if (room == null) continue;

                if (room.gameObject.name == "Room_Office")
                {
                    SetSpriteIfAssigned(room, "emptySprite", s.officeFront);
                    SetSpriteIfAssigned(room, "monsterSprite", s.officeMonster);
                    continue;
                }

                var slot = SlotFor(s, i);
                if (slot == null) continue;

                SetSpriteIfAssigned(room, "emptySprite", slot.empty);
                SetSpriteIfAssigned(room, "monsterSprite", slot.monster);
                if (room is AlterableRoom)
                {
                    SetSpriteIfAssigned(room, "alteredEmptySprite", slot.alteredEmpty);
                    SetSpriteIfAssigned(room, "alteredMonsterSprite", slot.alteredMonster);
                }
                roomCount++;
            }
        }

        // Vista da frente (mostra a sala "Office")
        if (s.officeFront != null)
        {
            foreach (var m in FindAll<MonitorController>())
            {
                var front = GetRef<SpriteRenderer>(m, "officeView");
                if (front == null) continue;
                Undo.RecordObject(front, "FNaF Setup");
                front.sprite = s.officeFront;
            }
        }

        // Transição ao abrir o monitor (cria a imagem se a cena for de uma versão antiga do setup)
        if (s.monitorA != null || s.monitorB != null)
        {
            foreach (var m in FindAll<MonitorController>())
            {
                if (GetRef<Image>(m, "transitionImage") == null)
                {
                    var panelGo = GetRef<GameObject>(m, "cameraPanel");
                    if (panelGo != null && panelGo.transform.parent != null)
                        SetRef(m, "transitionImage",
                            CreateTransitionImage(panelGo.transform.parent, panelGo.transform));
                }

                SetSpriteIfAssigned(m, "transitionSpriteA", s.monitorA);
                SetSpriteIfAssigned(m, "transitionSpriteB", s.monitorB);
            }
        }

        // Vistas laterais: fundo com a porta aberta / fechada
        foreach (var side in FindAll<OfficeSideView>())
        {
            bool left = side.gameObject.name == "View_Left";
            Sprite open = left ? s.leftOpen : s.rightOpen;
            Sprite closed = left ? s.leftClosed : s.rightClosed;

            SetSpriteIfAssigned(side, "doorOpenSprite", open);
            SetSpriteIfAssigned(side, "doorClosedSprite", closed);

            var bg = GetRef<SpriteRenderer>(side, "background");
            if (bg != null && open != null)
            {
                Undo.RecordObject(bg, "FNaF Setup");
                bg.sprite = open;
            }
        }

        // Vista de trás: safe / dangerous / kill
        foreach (var threat in FindAll<BackThreat>())
        {
            SetSpriteIfAssigned(threat, "safeSprite", s.backSafe);
            SetSpriteIfAssigned(threat, "dangerousSprite", s.backDangerous);
            SetSpriteIfAssigned(threat, "killSprite", s.backKill);

            var screen = GetRef<SpriteRenderer>(threat, "screen");
            if (screen != null && s.backSafe != null)
            {
                Undo.RecordObject(screen, "FNaF Setup");
                screen.sprite = s.backSafe;
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[FNaF Setup] Imagens aplicadas: {roomCount} sala(s) com câmara, escritório, " +
                  "vistas laterais e vista de trás.");
    }

    // Imagem de ecrã inteiro (inativa) que o MonitorController mostra durante a transição
    private static Image CreateTransitionImage(Transform canvas, Transform panel)
    {
        var go = NewObj("MonitorTransition", canvas, typeof(RectTransform), typeof(Image));
        Stretch(go.GetComponent<RectTransform>());
        go.transform.SetSiblingIndex(panel.GetSiblingIndex() + 1);

        var img = go.GetComponent<Image>();
        img.preserveAspect = false;
        img.raycastTarget = false;
        go.SetActive(false);
        return img;
    }

    private static void SetSpriteIfAssigned(Object target, string field, Sprite value)
    {
        if (value != null) SetRef(target, field, value);
    }

    private static T GetRef<T>(Object target, string field) where T : Object
    {
        var p = new SerializedObject(target).FindProperty(field);
        return p != null ? p.objectReferenceValue as T : null;
    }

    private class SpriteSet
    {
        public Sprite white, circle, ring, noise;
        public Sprite empty, monster, alteredEmpty, alteredMonster;
        public Sprite officeEmpty, officeMonster;
        public Sprite leftOpen, leftClosed, rightOpen, rightClosed;
        public Sprite backSafe, backDangerous, backKill;
    }

    // ------------------------------------------------------------ Janela

    [MenuItem("Tools/FNaF Setup/Abrir janela")]
    private static void Open() => GetWindow<GameSetupWindow>("FNaF Setup");

    [MenuItem("Tools/FNaF Setup/Criar tudo (valores por defeito)")]
    private static void QuickSetup() => Build(2, CustomLayout(), LoadSavedSounds(), LoadSavedSprites());

    // Aplica as imagens guardadas na janela a um setup que já existe na cena
    [MenuItem("Tools/FNaF Setup/Aplicar imagens à cena atual")]
    private static void ApplySpritesMenu() => ApplySpritesToScene(LoadSavedSprites());

    // Cria o minijogo do pentagrama dentro do painel das câmaras (inativo até ser usado)
    private static PentagramQTE CreateQte(Transform panel, SpriteSet set, GameObject nodePrefab,
        GameObject linePrefab, AudioSource managerAudio, SoundSet sounds)
    {
        var qteGo = NewObj("PentagramQTE", panel, typeof(RectTransform), typeof(Image));
        var qteRT = qteGo.GetComponent<RectTransform>();
        Place(qteRT, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420f, 420f));
        var qteBg = qteGo.GetComponent<Image>();
        qteBg.color = new Color(0f, 0f, 0f, 0.75f);
        qteBg.raycastTarget = true;
        var qte = qteGo.AddComponent<PentagramQTE>();

        var ringGo = NewObj("Circle", qteGo.transform, typeof(RectTransform), typeof(Image));
        Place(ringGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(300f, 300f));
        var ringImg = ringGo.GetComponent<Image>();
        ringImg.sprite = set.ring;
        ringImg.color = new Color(1f, 1f, 1f, 0.7f);
        ringImg.raycastTarget = false;

        var barGo = NewObj("TimerBar", qteGo.transform, typeof(RectTransform), typeof(Image));
        Place(barGo.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -14f), new Vector2(360f, 12f));
        var bar = barGo.GetComponent<Image>();
        bar.sprite = set.white;
        bar.type = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;
        bar.fillOrigin = 0;
        bar.fillAmount = 1f;
        bar.color = new Color(0.9f, 0.15f, 0.15f);
        bar.raycastTarget = false;

        var staticGo = NewObj("StaticOverlay", qteGo.transform,
            typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        Stretch(staticGo.GetComponent<RectTransform>());
        var staticImg = staticGo.GetComponent<Image>();
        staticImg.sprite = set.noise;
        staticImg.raycastTarget = false;
        var staticGroup = staticGo.GetComponent<CanvasGroup>();
        staticGroup.alpha = 0f;
        staticGroup.blocksRaycasts = false;
        staticGroup.interactable = false;

        SetRef(qte, "area", qteRT);
        SetRef(qte, "circle", ringGo.GetComponent<RectTransform>());
        SetRef(qte, "nodePrefab", nodePrefab.GetComponent<RectTransform>());
        SetRef(qte, "linePrefab", linePrefab.GetComponent<Image>());
        SetRef(qte, "timerBar", bar);
        SetRef(qte, "staticOverlay", staticGroup);
        // AudioSource do GameManager (o objeto do QTE é desativado e cortaria o som)
        SetRef(qte, "audioSource", managerAudio);
        SetRef(qte, "successClip", sounds.qteSuccess);
        SetRef(qte, "failClip", sounds.qteFail);
        SetRef(qte, "stepClip", sounds.qteStep);

        qteGo.SetActive(false);
        return qte;
    }

    // Reconstrói só o minijogo do pentagrama numa cena já feita, sem mexer no resto.
    // Mantém os valores que já tinhas ajustados no QTE (raio, tempo, cores...) e os sons que já tinha.
    private static void UpdateQteInScene(SoundSet sounds)
    {
        var olds = FindAll<PentagramQTE>();
        if (olds.Length == 0)
        {
            EditorUtility.DisplayDialog("FNaF Setup",
                "Não encontrei nenhum PentagramQTE na cena. Usa \"Criar setup completo\" primeiro.", "OK");
            return;
        }

        var old = olds[0];
        var panel = old.transform.parent;
        if (panel == null)
        {
            Debug.LogWarning("[FNaF Setup] O PentagramQTE não está dentro de um painel; nada foi alterado.");
            return;
        }

        Directory.CreateDirectory(SpritesDir);
        Directory.CreateDirectory(PrefabsDir);

        var set = CreateSprites();

        // Reutiliza os prefabs (nós e linhas) do QTE antigo, para manter o que tenhas personalizado
        var nodeRT = GetRef<RectTransform>(old, "nodePrefab");
        var lineImg = GetRef<Image>(old, "linePrefab");
        var nodePrefab = nodeRT != null ? nodeRT.gameObject : CreatePrefab("NodePrefab", set.circle, new Vector2(40f, 40f));
        var linePrefab = lineImg != null ? lineImg.gameObject : CreatePrefab("LinePrefab", set.white, new Vector2(100f, 6f));
        var audio = GetRef<AudioSource>(old, "audioSource");

        int sibling = old.transform.GetSiblingIndex();
        var fresh = CreateQte(panel, set, nodePrefab, linePrefab, audio, sounds);

        // Copia os valores ajustados (não referências); os clips só se a janela não tiver um
        var oso = new SerializedObject(old);
        var nso = new SerializedObject(fresh);
        var it = oso.GetIterator();
        bool enter = true;
        while (it.NextVisible(enter))
        {
            enter = false;
            if (it.name == "m_Script") continue;

            var np = nso.FindProperty(it.propertyPath);
            if (np == null || np.propertyType != it.propertyType) continue;

            if (it.propertyType == SerializedPropertyType.ObjectReference)
            {
                bool clip = it.name == "successClip" || it.name == "failClip" || it.name == "stepClip";
                if (clip && np.objectReferenceValue == null) np.objectReferenceValue = it.objectReferenceValue;
                continue;
            }

            CopyPropertyValue(it, np);
        }
        nso.ApplyModifiedProperties();

        if (audio == null) LinkSharedAudio(fresh);

        foreach (var m in FindAll<MonitorController>()) SetRef(m, "qte", fresh);
        foreach (var r in FindAll<RepairController>()) SetRef(r, "qte", fresh);

        Undo.DestroyObjectImmediate(old.gameObject);
        fresh.transform.SetSiblingIndex(sibling);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[FNaF Setup] Minijogo do pentagrama atualizado (o resto da cena não foi alterado).");
    }

    // Copia o valor de uma propriedade serializada para outra do mesmo tipo (não referências)
    private static void CopyPropertyValue(SerializedProperty src, SerializedProperty dst)
    {
        switch (src.propertyType)
        {
            case SerializedPropertyType.Integer:
            case SerializedPropertyType.LayerMask:
            case SerializedPropertyType.Character:
                dst.longValue = src.longValue; break;
            case SerializedPropertyType.Boolean: dst.boolValue = src.boolValue; break;
            case SerializedPropertyType.Float: dst.floatValue = src.floatValue; break;
            case SerializedPropertyType.String: dst.stringValue = src.stringValue; break;
            case SerializedPropertyType.Color: dst.colorValue = src.colorValue; break;
            case SerializedPropertyType.Enum: dst.enumValueIndex = src.enumValueIndex; break;
            case SerializedPropertyType.Vector2: dst.vector2Value = src.vector2Value; break;
            case SerializedPropertyType.Vector3: dst.vector3Value = src.vector3Value; break;
            case SerializedPropertyType.Vector4: dst.vector4Value = src.vector4Value; break;
            case SerializedPropertyType.Vector2Int: dst.vector2IntValue = src.vector2IntValue; break;
            case SerializedPropertyType.Rect: dst.rectValue = src.rectValue; break;
            case SerializedPropertyType.AnimationCurve: dst.animationCurveValue = src.animationCurveValue; break;
        }
    }

    [MenuItem("Tools/FNaF Setup/Atualizar minijogo do pentagrama (cena atual)")]
    private static void UpdateQteMenu() => UpdateQteInScene(LoadSavedSounds());

    // Liga o botão a um método público sem parâmetros, pelo nome (não obriga o método a existir
    // ao compilar: se o script ainda não o tiver, avisa em vez de dar erro)
    private static void AddCall(Button btn, Object target, string method)
    {
        var mi = target.GetType().GetMethod(method,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
            null, System.Type.EmptyTypes, null);

        if (mi == null)
        {
            Debug.LogWarning($"[FNaF Setup] {target.GetType().Name} não tem o método {method}(). " +
                             "Atualiza esse script e volta a aplicar.");
            return;
        }

        var action = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), target, mi);
        UnityEventTools.AddPersistentListener(btn.onClick, action);
    }

    // Acrescenta o botão "MENU PRINCIPAL" aos painéis de derrota e vitória de uma cena já feita
    [MenuItem("Tools/FNaF Setup/Adicionar botão de menu principal aos painéis")]
    private static void AddMainMenuButtonsMenu()
    {
        int added = 0;

        foreach (var g in FindAll<GameOverController>())
        {
            var panel = GetRef<GameObject>(g, "_gameOverPanel");
            if (panel == null || panel.transform.Find("Btn_MainMenu") != null) continue;

            var btn = CreateButton("Btn_MainMenu", "MENU PRINCIPAL", panel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(-120f, -280f), new Vector2(240f, 60f));
            AddCall(btn, g, "GoToMainMenu");
            added++;
        }

        foreach (var w in FindAll<WinController>())
        {
            var panel = GetRef<GameObject>(w, "_winPanel");
            if (panel == null || panel.transform.Find("Btn_MainMenu") != null) continue;

            var btn = CreateButton("Btn_MainMenu", "MENU PRINCIPAL", panel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(-150f, -280f), new Vector2(300f, 60f));
            AddCall(btn, w, "GoToMainMenu");
            added++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[FNaF Setup] Botão de menu principal adicionado a {added} painel(is).");
    }

    // Aplica os sons guardados na janela a um setup que já existe na cena
    [MenuItem("Tools/FNaF Setup/Aplicar sons à cena atual")]
    private static void ApplySoundsMenu() => ApplySoundsToScene(LoadSavedSounds());

    // Move o AudioSource do som de bater para fora das portas (que desativam com o monitor)
    [MenuItem("Tools/FNaF Setup/Corrigir som das portas")]
    private static void FixKnockSourcesMenu() => FixKnockSources();

    // Corrige um EventSystem já existente na cena (troca o módulo de input antigo pelo novo)
    [MenuItem("Tools/FNaF Setup/Corrigir EventSystem (Input System)")]
    private static void FixEventSystemMenu()
    {
        EnsureEventSystem();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[FNaF Setup] EventSystem verificado/corrigido.");
    }

    // Para um setup que já existe na cena: cria só a barra de vida no Canvas, ligada ao Health
    [MenuItem("Tools/FNaF Setup/Criar barra de vida na cena atual")]
    private static void CreateHealthBarMenu()
    {
        if (FindAll<HealthBarUI>().Length > 0)
        {
            Debug.Log("[FNaF Setup] Já existe uma barra de vida na cena.");
            return;
        }

        var healths = FindAll<Health>();
        if (healths.Length == 0)
        {
            Debug.LogError("[FNaF Setup] Não há nenhum Health na cena. Adiciona um Health ao GameManager primeiro.");
            return;
        }

        Canvas canvas = null;
        foreach (var c in FindAll<Canvas>())
            if (c.isRootCanvas) { canvas = c; break; }
        if (canvas == null)
        {
            Debug.LogError("[FNaF Setup] Não encontrei nenhum Canvas na cena.");
            return;
        }

        Directory.CreateDirectory(SpritesDir);
        var white = GetOrCreateSprite("white", 4, 4, (x, y) => Color.white);
        CreateHealthBar(canvas.transform, white, healths[0]);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[FNaF Setup] Barra de vida criada no Canvas.");
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.HelpBox(
            "Cria na cena atual: escritório, mapa de câmaras estilo FNaF (salas com várias ligações), " +
            "monitor (abre/fecha ao passar o rato na zona em baixo), monstro, UI, o QTE do pentagrama, a interferência " +
            "das câmaras, o game over e as portas do escritório (clicar na porta abre/fecha), com todas as referências ligadas. " +
            "O escritório tem 4 vistas (frente, direita, trás, esquerda) fora do canvas, em sprites 2D: " +
            "vira-se para os lados com os bordos do ecrã (ou A/D, sem dar a volta); olha-se para trás com a " +
            "zona por baixo ao lado do monitor (ou S). As vistas laterais têm o fundo com a porta aberta e " +
            "com a porta fechada, e o ecrã de trás tem os estados safe/dangerous/kill.",
            MessageType.Info);

        useCustomMap = EditorGUILayout.ToggleLeft(
            $"Usar o mapa desenhado à mão ({MapRooms.Length} salas, tabelas MapRooms/Links no topo do script)",
            useCustomMap);

        int total;
        if (useCustomMap)
        {
            total = MapRooms.Length;
            EditorGUILayout.LabelField("Total de salas (câmaras)", total.ToString());
        }
        else
        {
            totalRooms = EditorGUILayout.IntSlider("Total de salas (câmaras)", totalRooms,
                MinGeneratedRooms, MaxGeneratedRooms);
            total = totalRooms;
            EditorGUILayout.HelpBox(
                "O mapa é gerado automaticamente: sala 1 = palco (o monstro começa aí), " +
                "as duas últimas dão para o escritório (não conta como sala).",
                MessageType.None);
        }

        alterableRooms = Mathf.Clamp(alterableRooms, 0, total);
        alterableRooms = EditorGUILayout.IntSlider("Salas alteráveis", alterableRooms, 0, total);

        // ---- Sons
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sons", EditorStyles.boldLabel);

        var so = new SerializedObject(this);
        so.Update();
        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(so.FindProperty(nameof(noiseClips)),
            new GUIContent("Ruído das salas", "Botão de ruído e falha no QTE. Toca um aleatório da lista."), true);
        EditorGUILayout.PropertyField(so.FindProperty(nameof(cameraSwitchClip)),
            new GUIContent("Troca de câmara"));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(interferenceClip)),
            new GUIContent("Interferência", "Estática nas câmaras quando o monstro se mexe."));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(monitorOpenClip)),
            new GUIContent("Abrir monitor"));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(monitorCloseClip)),
            new GUIContent("Fechar monitor"));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(qteSuccessClip)),
            new GUIContent("QTE: sucesso"));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(qteFailClip)),
            new GUIContent("QTE: falha"));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(qteStepClip)),
            new GUIContent("QTE: ponto ligado", "Toca a cada ponto ligado; o tom e o volume sobem a cada ponto."));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(jumpscareClip)),
            new GUIContent("Jumpscare", "Toca quando o monstro ataca o jogador."));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(knockClip)),
            new GUIContent("Bater à porta", "Toca do lado da porta (esquerda/direita) quando o monstro bate."));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(doorOpenClip)),
            new GUIContent("Porta: abrir"));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(doorCloseClip)),
            new GUIContent("Porta: fechar"));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(backSafeClip)),
            new GUIContent("Trás: safe", "Toca (vindo de trás) quando a tela de trás passa a safe."));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(backDangerousClip)),
            new GUIContent("Trás: dangerous", "Toca (vindo de trás) quando a tela de trás passa a dangerous."));
        EditorGUILayout.PropertyField(so.FindProperty(nameof(backKillClip)),
            new GUIContent("Trás: kill", "Toca (vindo de trás) quando a tela de trás passa a kill."));
        if (EditorGUI.EndChangeCheck())
        {
            so.ApplyModifiedProperties();
            SaveSounds();
        }

        // ---- Imagens
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Imagens", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Campo vazio = sprite placeholder ao criar o setup, e não altera nada ao aplicar à cena atual. " +
            "A imagem 'com o monstro' de cada sala é a que aparece quando o monstro lá está.",
            MessageType.None);

        var spriteLayout = useCustomMap ? CustomLayout() : GenerateLayout(totalRooms);
        spriteSlots = EnsureRoomSlots(spriteSlots, spriteLayout.rooms.Length);
        var alterableFlags = AlterableFlags(spriteLayout, alterableRooms);

        EditorGUI.BeginChangeCheck();

        showOfficeSprites = EditorGUILayout.Foldout(showOfficeSprites, "Escritório", true);
        if (showOfficeSprites)
        {
            EditorGUI.indentLevel++;
            spriteSlots.officeFront = SpriteField("Frente: vazio", spriteSlots.officeFront);
            spriteSlots.officeMonster = SpriteField("Frente: com o monstro", spriteSlots.officeMonster);
            spriteSlots.leftOpen = SpriteField("Esquerda: porta aberta", spriteSlots.leftOpen);
            spriteSlots.leftClosed = SpriteField("Esquerda: porta fechada", spriteSlots.leftClosed);
            spriteSlots.rightOpen = SpriteField("Direita: porta aberta", spriteSlots.rightOpen);
            spriteSlots.rightClosed = SpriteField("Direita: porta fechada", spriteSlots.rightClosed);
            spriteSlots.backSafe = SpriteField("Trás: safe", spriteSlots.backSafe);
            spriteSlots.backDangerous = SpriteField("Trás: dangerous", spriteSlots.backDangerous);
            spriteSlots.backKill = SpriteField("Trás: kill", spriteSlots.backKill);
            spriteSlots.monitorA = SpriteField("Monitor: office 1", spriteSlots.monitorA);
            spriteSlots.monitorB = SpriteField("Monitor: office 2 (a meio)", spriteSlots.monitorB);
            EditorGUI.indentLevel--;
        }

        showRoomSprites = EditorGUILayout.Foldout(showRoomSprites,
            $"Salas das câmaras ({spriteLayout.rooms.Length})", true);
        if (showRoomSprites)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < spriteLayout.rooms.Length; i++)
            {
                var slot = spriteSlots.rooms[i];
                string title = $"CAM {spriteLayout.rooms[i].label}" + (alterableFlags[i] ? "  (alterável)" : "");

                roomFoldout[i] = EditorGUILayout.Foldout(roomFoldout[i], title, true);
                if (!roomFoldout[i]) continue;

                EditorGUI.indentLevel++;
                slot.empty = SpriteField("Vazia", slot.empty);
                slot.monster = SpriteField("Com o monstro", slot.monster);
                if (alterableFlags[i])
                {
                    slot.alteredEmpty = SpriteField("Alterada: vazia", slot.alteredEmpty);
                    slot.alteredMonster = SpriteField("Alterada: com o monstro", slot.alteredMonster);
                }
                EditorGUI.indentLevel--;
            }
            EditorGUI.indentLevel--;
        }

        if (EditorGUI.EndChangeCheck()) SaveSprites(spriteSlots);

        EditorGUILayout.Space();
        if (GUILayout.Button("Criar setup completo", GUILayout.Height(32)))
            Build(alterableRooms, useCustomMap ? CustomLayout() : GenerateLayout(totalRooms),
                CurrentSounds(), spriteSlots);

        if (GUILayout.Button("Aplicar sons à cena atual (campos vazios não alteram)"))
            ApplySoundsToScene(CurrentSounds());

        if (GUILayout.Button("Aplicar imagens à cena atual (campos vazios não alteram)"))
            ApplySpritesToScene(spriteSlots);

        if (GUILayout.Button("Adicionar botão de menu principal aos painéis (cena atual)"))
            AddMainMenuButtonsMenu();

        if (GUILayout.Button("Atualizar minijogo do pentagrama (cena atual, sem refazer o resto)"))
            UpdateQteInScene(CurrentSounds());

        EditorGUILayout.EndScrollView();
    }

    // ------------------------------------------------------------ Build

    private static void Build(int alterableCount, MapLayout layout, SoundSet sounds, SpriteSlots sprites)
    {
        if (GameObject.Find("GameManager") != null &&
            !EditorUtility.DisplayDialog("FNaF Setup",
                "Já existe um 'GameManager' nesta cena. Criar outro setup à mesma?",
                "Criar", "Cancelar"))
            return;

        int roomCount = layout.rooms.Length;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        Directory.CreateDirectory(SpritesDir);
        Directory.CreateDirectory(PrefabsDir);

        var set = CreateSprites();
        ApplyOverrides(set, sprites);   // imagens escolhidas na janela substituem os placeholders
        var nodePrefab = CreatePrefab("NodePrefab", set.circle, new Vector2(40f, 40f));
        var linePrefab = CreatePrefab("LinePrefab", set.white, new Vector2(100f, 6f));

        // ---- Salas
        var isAlterable = new bool[roomCount];
        for (int k = 0; k < Mathf.Min(alterableCount, layout.alterablePriority.Length); k++)
            isAlterable[layout.alterablePriority[k]] = true;

        var roomsRoot = NewObj("Rooms", null);
        var rooms = new Room[roomCount];
        for (int i = 0; i < roomCount; i++)
        {
            var go = NewObj($"Room_CAM{layout.rooms[i].label}", roomsRoot.transform);
            go.transform.position = new Vector3(layout.rooms[i].pos.x * 10f, layout.rooms[i].pos.y * 10f, 0f);
            rooms[i] = isAlterable[i] ? go.AddComponent<AlterableRoom>() : go.AddComponent<Room>();
            var roomSlot = SlotFor(sprites, i);
            SetupRoom(rooms[i], $"CAM {layout.rooms[i].label}",
                Or(roomSlot?.empty, set.empty), Or(roomSlot?.monster, set.monster),
                Or(roomSlot?.alteredEmpty, set.alteredEmpty), Or(roomSlot?.alteredMonster, set.alteredMonster),
                sounds.noise);
        }

        var alterableList = new List<Object>();
        foreach (var r in rooms)
            if (r is AlterableRoom) alterableList.Add(r);

        // Escritório: a sala onde o jogador realmente está (não tem câmara)
        var officeGo = NewObj("Room_Office", roomsRoot.transform);
        officeGo.transform.position = new Vector3(layout.officePos.x * 10f, layout.officePos.y * 10f, 0f);
        var office = officeGo.AddComponent<Room>();
        SetupRoom(office, "Office", set.officeEmpty, set.officeMonster, null, null, sounds.noise);

        Room Node(int i) => i == roomCount ? office : rooms[i];

        // Vizinhos a partir da tabela de ligações (ligações nos dois sentidos)
        var adjacency = new List<Object>[roomCount + 1];
        for (int i = 0; i <= roomCount; i++) adjacency[i] = new List<Object>();
        foreach (var link in layout.links)
        {
            adjacency[link.a].Add(Node(link.b));
            adjacency[link.b].Add(Node(link.a));
        }
        for (int i = 0; i <= roomCount; i++)
            SetRefArray(Node(i), "neighbors", adjacency[i].ToArray());

        // ---- Sistemas
        var manager = NewObj("GameManager", null);
        var noise = manager.AddComponent<NoiseSystem>();
        var cams = manager.AddComponent<CameraController>();
        var noiseBtn = manager.AddComponent<NoiseButton>();
        var repair = manager.AddComponent<RepairController>();
        var monitor = manager.AddComponent<MonitorController>();
        var interference = manager.AddComponent<CameraInterference>();
        var gameOver = manager.AddComponent<GameOverController>();

        // Vida do jogador: drenada enquanto houver portas fechadas
        var playerHealth = manager.AddComponent<Health>();
        var doorDrain = manager.AddComponent<DoorHealthDrain>();

        // Vitória: reparar (QTE) uma vez cada sala alterável diferente
        var win = manager.AddComponent<WinController>();

        // Vistas do escritório (frente/direita/trás/esquerda) e o ecrã de trás (safe/dangerous/kill)
        var officeViews = manager.AddComponent<OfficeViewController>();
        var backThreat = manager.AddComponent<BackThreat>();

        // AudioSource partilhado do GameManager: som de troca de câmara (e estática, se quiseres)
        var managerAudio = manager.AddComponent<AudioSource>();
        managerAudio.playOnAwake = false;

        var monsterGo = NewObj("Monster", null);
        var monster = monsterGo.AddComponent<Monster>();
        SetRef(monster, "startRoom", rooms[0]);
        SetRef(monster, "playerRoom", office);
        SetRef(monster, "cams", cams);
        SetRef(monster, "monitor", monitor);

        // ---- UI
        var canvasGo = NewObj("Canvas", null, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        EnsureEventSystem();

        // Vistas do escritório: FORA do canvas, no mundo 2D. Cada vista é um objeto com a sua imagem
        // (SpriteRenderer ajustado à câmara) e com os objetos interativos dessa vista (as portas dos
        // lados) como FILHOS. O OfficeViewController mostra uma de cada vez e o monitor esconde
        // tudo (OfficeViews) quando abre.
        var worldCam = EnsureWorldCamera();
        var viewsRoot = NewObj("OfficeViews", null);
        var frontViewGo = CreateOfficeView("View_Front", viewsRoot.transform, set.officeEmpty, worldCam, out var officeView);
        var rightViewGo = CreateOfficeView("View_Right", viewsRoot.transform, set.rightOpen, worldCam, out var rightRenderer);
        var backViewGo = CreateOfficeView("View_Back", viewsRoot.transform, set.backSafe, worldCam, out var backRenderer);
        var leftViewGo = CreateOfficeView("View_Left", viewsRoot.transform, set.leftOpen, worldCam, out var leftRenderer);

        // No editor só a frente fica visível; em jogo o OfficeViewController trata disto
        rightViewGo.SetActive(false);
        backViewGo.SetActive(false);
        leftViewGo.SetActive(false);

        // Portas do escritório: uma por ligação ao escritório. As do lado esquerdo ficam na vista da
        // esquerda e as do lado direito na vista da direita, conforme a posição da sala no mapa.
        // Clicar numa porta abre/fecha-a.
        var entranceComps = new List<Object>();
        var doorComps = new List<Object>();
        int doorsLeft = 0, doorsRight = 0;
        var leftDoors = new List<Object>();
        var rightDoors = new List<Object>();
        // As portas são desativadas quando o monitor abre, e um AudioSource desativado não toca:
        // as fontes do som de bater ficam num objeto à parte, sempre ativo
        var knockRoot = NewObj("OfficeKnockSources", null);

        foreach (var link in layout.links)
        {
            int otherIdx = link.a == roomCount ? link.b : (link.b == roomCount ? link.a : -1);
            if (otherIdx < 0) continue;

            float dx = layout.rooms[otherIdx].pos.x - layout.officePos.x;
            float sideSign = dx < 0f ? -1f : 1f;
            int slot = sideSign < 0f ? doorsLeft++ : doorsRight++;
            // Várias portas do mesmo lado ficam lado a lado dentro da mesma vista
            float doorX = slot * 2.5f;
            var sideView = sideSign < 0f ? leftViewGo : rightViewGo;

            var doorGo = NewObj($"Door_CAM{layout.rooms[otherIdx].label}", sideView.transform,
                typeof(BoxCollider2D), typeof(Door), typeof(DoorButton),
                typeof(OfficeEntrance));
            doorGo.transform.localPosition = new Vector3(doorX, 0f, -1f);

            // A porta é só a área clicável: o desenho da porta aberta/fechada está no fundo da vista
            // (OfficeSideView troca entre as duas imagens). Ajusta o tamanho a gosto no BoxCollider2D.
            var doorBox = doorGo.GetComponent<BoxCollider2D>();
            doorBox.size = new Vector2(2.2f, 5f);
            doorBox.offset = Vector2.zero;

            var doorAudio = CreateKnockSource(knockRoot.transform, doorGo.name);

            var doorComp = doorGo.GetComponent<Door>();
            // Usa o AudioSource do GameManager (sempre ativo): o da própria porta cortava o som
            // ao virar para outra vista
            SetRef(doorComp, "SoundPlayer", managerAudio);
            SetRef(doorComp, "OpenSound", sounds.doorOpen);
            SetRef(doorComp, "CloseSound", sounds.doorClose);

            SetRef(doorGo.GetComponent<DoorButton>(), "_door", doorComp);

            var entrance = doorGo.GetComponent<OfficeEntrance>();
            SetRef(entrance, "_fromRoom", rooms[otherIdx]);
            SetRef(entrance, "_door", doorComp);
            SetRef(entrance, "_knockSource", doorAudio);
            SetFloat(entrance, "_pan", sideSign);
            if (sounds.knock != null) SetRefArray(entrance, "_knockClips", new Object[] { sounds.knock });
            entranceComps.Add(entrance);
            doorComps.Add(doorComp);
            (sideSign < 0f ? leftDoors : rightDoors).Add(doorComp);
        }

        SetRefArray(monster, "entrances", entranceComps.ToArray());
        SetRef(doorDrain, "health", playerHealth);
        SetRefArray(doorDrain, "doors", doorComps.ToArray());

        // Fundo das vistas laterais: troca entre porta aberta e porta fechada
        var leftSide = leftViewGo.AddComponent<OfficeSideView>();
        SetRef(leftSide, "background", leftRenderer);
        SetRef(leftSide, "doorOpenSprite", set.leftOpen);
        SetRef(leftSide, "doorClosedSprite", set.leftClosed);
        SetRefArray(leftSide, "doors", leftDoors.ToArray());

        var rightSide = rightViewGo.AddComponent<OfficeSideView>();
        SetRef(rightSide, "background", rightRenderer);
        SetRef(rightSide, "doorOpenSprite", set.rightOpen);
        SetRef(rightSide, "doorClosedSprite", set.rightClosed);
        SetRefArray(rightSide, "doors", rightDoors.ToArray());

        // Painel das câmaras (só visível quando o monitor está aberto)
        var panel = NewObj("CameraPanel", canvasGo.transform, typeof(RectTransform));
        Stretch(panel.GetComponent<RectTransform>());

        // Transição ao abrir o monitor: office 1 -> (a meio) office 2 -> câmaras.
        // Fica no canvas, por cima do painel (que está escondido durante a transição).
        var transitionImg = CreateTransitionImage(canvasGo.transform, panel.transform);

        // Feed da câmara
        var feedGo = NewObj("CameraFeed", panel.transform, typeof(RectTransform), typeof(Image));
        Stretch(feedGo.GetComponent<RectTransform>());
        var feed = feedGo.GetComponent<Image>();
        feed.sprite = set.empty;
        feed.preserveAspect = false;
        feed.raycastTarget = false;

        // Interferência das câmaras (ruído estático quando o monstro se mexe).
        // Fica por cima do feed e por baixo do mapa/botões. A textura de ruído é gerada
        // pelo CameraInterference em runtime; o CanvasGroup a 0 mantém-no invisível até lá.
        var interferenceGo = NewObj("CameraInterferenceOverlay", panel.transform,
            typeof(RectTransform), typeof(RawImage), typeof(CanvasGroup));
        Stretch(interferenceGo.GetComponent<RectTransform>());
        var interferenceImg = interferenceGo.GetComponent<RawImage>();
        interferenceImg.raycastTarget = false;
        var interferenceGroup = interferenceGo.GetComponent<CanvasGroup>();
        interferenceGroup.alpha = 0f;
        interferenceGroup.blocksRaycasts = false;
        interferenceGroup.interactable = false;

        // ---- Mapa de câmaras (estilo FNaF)
        var mapGo = NewObj("CameraMap", panel.transform, typeof(RectTransform), typeof(Image));
        Place(mapGo.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-20f, 100f), MapSize);
        var mapBg = mapGo.GetComponent<Image>();
        mapBg.color = new Color(0f, 0f, 0f, 0.55f);

        // Linhas de ligação (criadas primeiro, ficam por baixo dos botões)
        foreach (var link in layout.links)
        {
            Vector2 a = MapCenter(link.a == roomCount ? layout.officePos : layout.rooms[link.a].pos);
            Vector2 b = MapCenter(link.b == roomCount ? layout.officePos : layout.rooms[link.b].pos);
            CreateMapLine(mapGo.transform, a, b);
        }

        // Marcador do escritório (não clicável)
        var officeMarker = NewObj("OfficeMarker", mapGo.transform, typeof(RectTransform), typeof(Image));
        Place(officeMarker.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero,
            MapPosition(layout.officePos), MapButtonSize);
        var markerImg = officeMarker.GetComponent<Image>();
        markerImg.color = new Color(0.45f, 0.30f, 0.10f, 0.95f);
        markerImg.raycastTarget = false;
        AddLabel(officeMarker.transform, "OFFICE", 18);

        // Botões de câmara
        for (int i = 0; i < roomCount; i++)
        {
            var btn = CreateButton($"Btn_CAM{layout.rooms[i].label}", layout.rooms[i].label, mapGo.transform,
                Vector2.zero, MapPosition(layout.rooms[i].pos), MapButtonSize);
            UnityEventTools.AddObjectPersistentListener<Room>(
                btn.onClick, new UnityAction<Room>(cams.ViewRoom), rooms[i]);

            var highlight = btn.gameObject.AddComponent<CameraButtonHighlight>();
            SetRef(highlight, "cams", cams);
            SetRef(highlight, "room", rooms[i]);
            SetRef(highlight, "image", btn.GetComponent<Image>());
        }

        // Botão de ruído
        var noiseButton = CreateButton("Btn_Noise", "FAZER RUÍDO", panel.transform,
            new Vector2(1f, 0f), new Vector2(-220f, 20f), new Vector2(200f, 60f));
        UnityEventTools.AddPersistentListener(noiseButton.onClick, new UnityAction(noiseBtn.OnClick));

        // Botão de reparar
        var repairButtonObj = CreateButton("Btn_Repair", "REPARAR", panel.transform,
            new Vector2(0.5f, 0f), new Vector2(-100f, 90f), new Vector2(200f, 60f));
        UnityEventTools.AddPersistentListener(repairButtonObj.onClick, new UnityAction(repair.OnRepairClick));
        repairButtonObj.gameObject.SetActive(false);

        // ---- QTE do pentagrama (dentro do painel das câmaras)
        var qte = CreateQte(panel.transform, set, nodePrefab, linePrefab, managerAudio, sounds);

        // ---- Zona de hover do monitor (estilo FNaF): passar o rato abre/fecha o monitor.
        // Fica no fim do canvas, por cima do painel das câmaras, para funcionar nas duas vistas.
        var zoneGo = NewObj("MonitorHoverZone", canvasGo.transform,
            typeof(RectTransform), typeof(Image), typeof(MonitorHoverZone));
        Place(zoneGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            Vector2.zero, new Vector2(700f, 60f));
        var zoneImg = zoneGo.GetComponent<Image>();
        zoneImg.color = new Color(1f, 1f, 1f, 0.12f);
        zoneImg.raycastTarget = true;
        AddLabel(zoneGo.transform, "^  MONITOR  ^", 22);
        var zone = zoneGo.GetComponent<MonitorHoverZone>();

        // ---- Zonas nos bordos do ecrã para virar o escritório (esquerda/direita).
        // Escondem-se com o monitor aberto (o OfficeViewController trata disso).
        var turnZonesGo = NewObj("OfficeTurnZones", canvasGo.transform, typeof(RectTransform));
        Stretch(turnZonesGo.GetComponent<RectTransform>());
        var leftZoneGo = CreateTurnZone(turnZonesGo.transform, "TurnZone_Left", "<", false, -1, officeViews);
        var rightZoneGo = CreateTurnZone(turnZonesGo.transform, "TurnZone_Right", ">", true, 1, officeViews);
        CreateBackZone(turnZonesGo.transform, officeViews);

        // ---- Barra de vida (canto superior esquerdo, visível com o monitor aberto ou fechado)
        CreateHealthBar(canvasGo.transform, set.white, playerHealth);

        // ---- Jumpscare e game over (escondidos até o monstro atacar)
        var jumpscareGo = NewObj("JumpscareView", canvasGo.transform, typeof(RectTransform), typeof(Image));
        Stretch(jumpscareGo.GetComponent<RectTransform>());
        jumpscareGo.GetComponent<Image>().color = new Color(0.5f, 0f, 0f, 1f);
        AddLabel(jumpscareGo.transform, "!!!", 160);
        jumpscareGo.SetActive(false);

        var gameOverGo = NewObj("GameOverPanel", canvasGo.transform, typeof(RectTransform), typeof(Image));
        Stretch(gameOverGo.GetComponent<RectTransform>());
        gameOverGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);
        AddLabel(gameOverGo.transform, "GAME OVER", 90);
        var restartButton = CreateButton("Btn_Restart", "RECOMEÇAR", gameOverGo.transform,
            new Vector2(0.5f, 0.5f), new Vector2(-120f, -200f), new Vector2(240f, 60f));
        UnityEventTools.AddPersistentListener(restartButton.onClick, new UnityAction(gameOver.Restart));
        var gameOverMenuButton = CreateButton("Btn_MainMenu", "MENU PRINCIPAL", gameOverGo.transform,
            new Vector2(0.5f, 0.5f), new Vector2(-120f, -280f), new Vector2(240f, 60f));
        AddCall(gameOverMenuButton, gameOver, "GoToMainMenu");
        gameOverGo.SetActive(false);

        var winGo = NewObj("WinPanel", canvasGo.transform, typeof(RectTransform), typeof(Image));
        Stretch(winGo.GetComponent<RectTransform>());
        winGo.GetComponent<Image>().color = new Color(0f, 0.15f, 0.05f, 0.92f);
        AddLabel(winGo.transform, "VITÓRIA!", 90);
        var winRestartButton = CreateButton("Btn_Restart", "JOGAR OUTRA VEZ", winGo.transform,
            new Vector2(0.5f, 0.5f), new Vector2(-150f, -200f), new Vector2(300f, 60f));
        UnityEventTools.AddPersistentListener(winRestartButton.onClick, new UnityAction(win.Restart));
        var winMenuButton = CreateButton("Btn_MainMenu", "MENU PRINCIPAL", winGo.transform,
            new Vector2(0.5f, 0.5f), new Vector2(-150f, -280f), new Vector2(300f, 60f));
        AddCall(winMenuButton, win, "GoToMainMenu");
        winGo.SetActive(false);

        // ---- Ligações
        SetRef(cams, "feed", feed);
        SetRef(cams, "startRoom", rooms[0]);
        SetRef(cams, "audioSource", managerAudio);
        SetRef(cams, "switchClip", sounds.cameraSwitch);

        SetRef(noiseBtn, "cams", cams);
        SetRef(noiseBtn, "noise", noise);

        SetRef(repair, "cams", cams);
        SetRef(repair, "qte", qte);
        SetRef(repair, "noise", noise);
        SetRef(repair, "monitor", monitor);
        SetRef(repair, "repairButton", repairButtonObj.gameObject);

        SetRef(monitor, "cameraPanel", panel);
        SetRef(monitor, "officeRoom", office);
        SetRef(monitor, "officeView", officeView);
        SetRef(monitor, "qte", qte);
        SetRef(monitor, "officeRoot", viewsRoot);
        SetRef(monitor, "transitionImage", transitionImg);
        SetRef(monitor, "transitionSpriteA", sprites != null ? sprites.monitorA : null);
        SetRef(monitor, "transitionSpriteB", sprites != null ? sprites.monitorB : null);

        SetRef(officeViews, "frontView", frontViewGo);
        SetRef(officeViews, "rightView", rightViewGo);
        SetRef(officeViews, "backView", backViewGo);
        SetRef(officeViews, "leftView", leftViewGo);
        SetRef(officeViews, "monitor", monitor);
        SetRef(officeViews, "turnZones", turnZonesGo);
        SetRefArray(officeViews, "sideZones", new Object[] { leftZoneGo, rightZoneGo });
        SetRef(officeViews, "audioSource", managerAudio);

        SetRef(backThreat, "screen", backRenderer);
        SetRef(backThreat, "safeSprite", set.backSafe);
        SetRef(backThreat, "dangerousSprite", set.backDangerous);
        SetRef(backThreat, "killSprite", set.backKill);
        SetRef(backThreat, "office", officeViews);
        SetRef(backThreat, "gameOver", gameOver);
        // audioSource fica vazio de propósito: o BackThreat cria a sua fonte 3D (som vindo de trás)
        SetRef(backThreat, "safeClip", sounds.backSafe);
        SetRef(backThreat, "dangerousClip", sounds.backDangerous);
        SetRef(backThreat, "killClip", sounds.backKill);
        SetRef(monitor, "audioSource", managerAudio);
        SetRef(monitor, "openClip", sounds.monitorOpen);
        SetRef(monitor, "closeClip", sounds.monitorClose);

        SetRef(zone, "_monitor", monitor);

        SetRef(gameOver, "_monitor", monitor);
        SetRef(gameOver, "_jumpscareView", jumpscareGo);
        SetRef(gameOver, "_gameOverPanel", gameOverGo);
        SetRef(win, "_repair", repair);
        SetRef(win, "_monitor", monitor);
        SetRef(win, "_winPanel", winGo);
        SetRef(win, "_audioSource", managerAudio);
        SetRefArray(win, "_rooms", alterableList.ToArray());

        SetRef(gameOver, "_health", playerHealth);
        SetRef(gameOver, "_audioSource", managerAudio);
        SetRef(gameOver, "_jumpscareClip", sounds.jumpscare);

        SetRef(interference, "staticImage", interferenceImg);
        SetRef(interference, "audioSource", managerAudio);
        SetRef(interference, "staticClip", sounds.interference);


        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = manager;
        AssetDatabase.SaveAssets();

        Debug.Log($"[FNaF Setup] Criados: escritório (frente/direita/trás/esquerda) + {roomCount} salas com câmara em mapa " +
                  $"({alterableCount} alteráveis), monitor, monstro, UI, QTE e interferência das câmaras. " +
                  "Troca os sprites placeholder pelos teus.");
    }

    // ------------------------------------------------------------ Barra de vida

    private static HealthBarUI CreateHealthBar(Transform canvas, Sprite white, Health health)
    {
        var barGo = NewObj("HealthBar", canvas, typeof(RectTransform), typeof(Image), typeof(HealthBarUI));
        Place(barGo.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(30f, -30f), new Vector2(360f, 36f));
        var bg = barGo.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);
        bg.raycastTarget = false;

        var fillGo = NewObj("Fill", barGo.transform, typeof(RectTransform), typeof(Image));
        var fillRT = fillGo.GetComponent<RectTransform>();
        Stretch(fillRT);
        fillRT.offsetMin = new Vector2(4f, 4f);
        fillRT.offsetMax = new Vector2(-4f, -4f);
        var fill = fillGo.GetComponent<Image>();
        fill.sprite = white;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 1f;
        fill.color = new Color(0.2f, 0.85f, 0.2f);
        fill.raycastTarget = false;

        AddLabel(barGo.transform, "VIDA", 18);

        var bar = barGo.GetComponent<HealthBarUI>();
        SetRef(bar, "fillImage", fill);
        SetRef(bar, "player", health);
        return bar;
    }

    // ------------------------------------------------------------ Mapa

    // Posição (canto inferior esquerdo do botão) dentro do painel do mapa
    private static Vector2 MapPosition(Vector2 normalized)
    {
        return new Vector2(
            normalized.x * (MapSize.x - MapButtonSize.x),
            normalized.y * (MapSize.y - MapButtonSize.y));
    }

    // Centro do botão dentro do painel do mapa
    private static Vector2 MapCenter(Vector2 normalized)
    {
        return MapPosition(normalized) + MapButtonSize * 0.5f;
    }

    private static void CreateMapLine(Transform parent, Vector2 a, Vector2 b)
    {
        var go = NewObj("Link", parent, typeof(RectTransform), typeof(Image));
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = a;

        Vector2 d = b - a;
        rt.sizeDelta = new Vector2(d.magnitude, 4f);
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);

        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.35f);
        img.raycastTarget = false;
    }

    // ------------------------------------------------------------ Vistas do escritório

    // Uma vista: objeto-contentor (onde ficam as portas) + imagem ajustada à câmara.
    // O contentor não é escalado, por isso os filhos (portas) mantêm o tamanho.
    private static GameObject CreateOfficeView(string name, Transform parent, Sprite sprite, Camera cam,
        out SpriteRenderer renderer)
    {
        var view = NewObj(name, parent);
        var imageGo = NewObj("Image", view.transform, typeof(SpriteRenderer), typeof(FitSpriteToCamera));

        renderer = imageGo.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;

        var fit = imageGo.GetComponent<FitSpriteToCamera>();
        SetRef(fit, "targetCamera", cam);
        fit.Fit();
        return view;
    }

    // Faixa vertical num bordo do ecrã: passar o rato por cima vira o escritório
    private static GameObject CreateTurnZone(Transform parent, string name, string label, bool rightSide,
        int direction, OfficeViewController office)
    {
        var go = NewObj(name, parent, typeof(RectTransform), typeof(Image), typeof(OfficeTurnZone));
        var rt = go.GetComponent<RectTransform>();
        float x = rightSide ? 1f : 0f;
        rt.anchorMin = new Vector2(x, 0f);
        rt.anchorMax = new Vector2(x, 1f);
        rt.pivot = new Vector2(x, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(70f, 0f);

        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.08f);
        img.raycastTarget = true;
        AddLabel(go.transform, label, 40);

        var zone = go.GetComponent<OfficeTurnZone>();
        SetRef(zone, "_office", office);
        SetInt(zone, "_direction", direction);
        return go;
    }

    // Zona por baixo, ao lado da zona do monitor: passar o rato olha para trás (e volta)
    private static void CreateBackZone(Transform parent, OfficeViewController office)
    {
        var go = NewObj("BackZone", parent, typeof(RectTransform), typeof(Image), typeof(OfficeBackZone));
        Place(go.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0f, 0f),
            new Vector2(370f, 0f), new Vector2(260f, 60f));

        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.12f);
        img.raycastTarget = true;
        AddLabel(go.transform, "v  TRÁS  v", 22);

        SetRef(go.GetComponent<OfficeBackZone>(), "_office", office);
    }

    // ------------------------------------------------------------ Salas

    private static void SetupRoom(Room room, string roomName, Sprite empty, Sprite monster,
        Sprite alteredEmpty, Sprite alteredMonster, AudioClip[] noise)
    {
        var src = room.gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;

        SetString(room, "roomName", roomName);
        SetRef(room, "noiseSource", src);
        if (noise != null && noise.Length > 0) SetRefArray(room, "noiseClips", noise);
        SetRef(room, "emptySprite", empty);
        SetRef(room, "monsterSprite", monster);

        if (room is AlterableRoom)
        {
            SetRef(room, "alteredEmptySprite", alteredEmpty);
            SetRef(room, "alteredMonsterSprite", alteredMonster);
        }
    }

    // ------------------------------------------------------------ Assets

    private static SpriteSet CreateSprites()
    {
        var s = new SpriteSet();

        s.white = GetOrCreateSprite("white", 4, 4, (x, y) => Color.white);

        s.circle = GetOrCreateSprite("circle", 64, 64, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f));
            return new Color(1f, 1f, 1f, Mathf.Clamp01(32f - d));
        });

        s.ring = GetOrCreateSprite("ring", 256, 256, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(128f, 128f));
            return new Color(1f, 1f, 1f, Mathf.Clamp01(2.5f - Mathf.Abs(d - 124f)));
        });

        s.noise = GetOrCreateSprite("static_noise", 128, 128, (x, y) =>
        {
            float v = Random.value;
            return new Color(v, v, v, 1f);
        });

        s.empty = GetOrCreateSprite("room_empty", 16, 9, (x, y) => new Color(0.12f, 0.12f, 0.14f));
        s.monster = GetOrCreateSprite("room_monster", 16, 9, (x, y) => new Color(0.35f, 0.05f, 0.05f));
        s.alteredEmpty = GetOrCreateSprite("room_altered_empty", 16, 9, (x, y) => new Color(0.10f, 0.10f, 0.28f));
        s.alteredMonster = GetOrCreateSprite("room_altered_monster", 16, 9, (x, y) => new Color(0.30f, 0.05f, 0.30f));

        s.officeEmpty = GetOrCreateSprite("office_empty", 16, 9, (x, y) => new Color(0.16f, 0.13f, 0.09f));
        s.officeMonster = GetOrCreateSprite("office_monster", 16, 9, (x, y) => new Color(0.40f, 0.08f, 0.05f));

        // Vistas laterais e de trás (a de trás tem 3 estados)
        var leftBg = new Color(0.13f, 0.14f, 0.10f);
        var rightBg = new Color(0.10f, 0.12f, 0.16f);
        s.leftOpen = GetOrCreateSprite("office_left_open", 32, 18, (x, y) => DoorPixel(x, y, leftBg, false));
        s.leftClosed = GetOrCreateSprite("office_left_closed", 32, 18, (x, y) => DoorPixel(x, y, leftBg, true));
        s.rightOpen = GetOrCreateSprite("office_right_open", 32, 18, (x, y) => DoorPixel(x, y, rightBg, false));
        s.rightClosed = GetOrCreateSprite("office_right_closed", 32, 18, (x, y) => DoorPixel(x, y, rightBg, true));
        s.backSafe = GetOrCreateSprite("office_back_safe", 16, 9, (x, y) => new Color(0.10f, 0.22f, 0.12f));
        s.backDangerous = GetOrCreateSprite("office_back_dangerous", 16, 9, (x, y) => new Color(0.45f, 0.30f, 0.05f));
        s.backKill = GetOrCreateSprite("office_back_kill", 16, 9, (x, y) => new Color(0.55f, 0.02f, 0.02f));

        return s;
    }

    private static Sprite GetOrCreateSprite(string name, int w, int h, System.Func<int, int, Color> pixel)
    {
        string path = $"{SpritesDir}/{name}.png";

        if (!File.Exists(path))
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, pixel(x, y));
            tex.Apply();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static GameObject CreatePrefab(string name, Sprite sprite, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.GetComponent<RectTransform>().sizeDelta = size;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = Color.white;

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabsDir}/{name}.prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    // ------------------------------------------------------------ Helpers de UI

    private static GameObject NewObj(string name, Transform parent, params System.Type[] components)
    {
        var go = new GameObject(name, components);
        Undo.RegisterCreatedObjectUndo(go, "FNaF Setup");
        if (parent != null) go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static Text AddLabel(Transform parent, string label, int fontSize)
    {
        var textGo = NewObj("Label", parent, typeof(RectTransform), typeof(Text));
        Stretch(textGo.GetComponent<RectTransform>());
        var t = textGo.GetComponent<Text>();
        t.text = label;
        t.font = GetFont();
        t.fontSize = fontSize;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = Color.white;
        t.raycastTarget = false;
        return t;
    }

    private static Button CreateButton(string name, string label, Transform parent,
        Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = NewObj(name, parent, typeof(RectTransform), typeof(Image), typeof(Button));
        Place(go.GetComponent<RectTransform>(), anchor, new Vector2(0f, 0f), pos, size);

        var img = go.GetComponent<Image>();
        img.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);

        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        AddLabel(go.transform, label, 22);
        return btn;
    }

    private static Font GetFont()
    {
#if UNITY_2022_2_OR_NEWER
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
    }

    // Câmara ortográfica para o escritório em 2D (cria uma se a cena não tiver Main Camera).
    // O Physics2DRaycaster deixa os Interactable (Collider2D) receberem hover/clique.
    private static Camera EnsureWorldCamera()
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var go = NewObj("Main Camera", null, typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);
            cam = go.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.orthographic = true;
            cam.orthographicSize = 5f;
        }

        if (!cam.orthographic)
        {
            Undo.RecordObject(cam, "FNaF Setup");
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            Debug.Log("[FNaF Setup] A Main Camera passou a ortográfica (o escritório é um sprite 2D).");
        }

        if (cam.GetComponent<Physics2DRaycaster>() == null)
            Undo.AddComponent<Physics2DRaycaster>(cam.gameObject);

        return cam;
    }

    private static void EnsureEventSystem()
    {
#if UNITY_2023_1_OR_NEWER
        var existing = Object.FindFirstObjectByType<EventSystem>();
#else
        var existing = Object.FindObjectOfType<EventSystem>();
#endif
        if (existing != null)
        {
#if ENABLE_INPUT_SYSTEM
            // O StandaloneInputModule usa o Input antigo e dá erro com o novo Input System
            var oldModule = existing.GetComponent<StandaloneInputModule>();
            if (oldModule != null) Undo.DestroyObjectImmediate(oldModule);

            var uiModule = existing.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            if (uiModule == null)
                uiModule = Undo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(existing.gameObject);
            if (uiModule.actionsAsset == null) uiModule.AssignDefaultActions();
#endif
            return;
        }

#if ENABLE_INPUT_SYSTEM
        var go = NewObj("EventSystem", null, typeof(EventSystem));
        var module = go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (module.actionsAsset == null) module.AssignDefaultActions();
#else
        NewObj("EventSystem", null, typeof(EventSystem), typeof(StandaloneInputModule));
#endif
    }

    // ------------------------------------------------------------ Sons na cena

    // Aplica os sons a um setup existente. Campos vazios não alteram nada.
    private static void ApplySoundsToScene(SoundSet sounds)
    {
        int roomCount = 0, camCount = 0, interferenceCount = 0;

        if (sounds.noise.Length > 0)
        {
            foreach (var room in FindAll<Room>())
            {
                SetRefArray(room, "noiseClips", sounds.noise);
                roomCount++;
            }
        }

        if (sounds.cameraSwitch != null)
        {
            foreach (var cam in FindAll<CameraController>())
            {
                EnsureAudioSource(cam, "audioSource");
                SetRef(cam, "switchClip", sounds.cameraSwitch);
                camCount++;
            }
        }

        if (sounds.interference != null)
        {
            foreach (var fx in FindAll<CameraInterference>())
            {
                EnsureAudioSource(fx, "audioSource");
                SetRef(fx, "staticClip", sounds.interference);
                interferenceCount++;
            }
        }

        if (sounds.monitorOpen != null || sounds.monitorClose != null)
        {
            foreach (var m in FindAll<MonitorController>())
            {
                EnsureAudioSource(m, "audioSource");
                if (sounds.monitorOpen != null) SetRef(m, "openClip", sounds.monitorOpen);
                if (sounds.monitorClose != null) SetRef(m, "closeClip", sounds.monitorClose);
            }
        }

        if (sounds.qteSuccess != null || sounds.qteFail != null || sounds.qteStep != null)
        {
            foreach (var q in FindAll<PentagramQTE>())
            {
                LinkSharedAudio(q);
                if (sounds.qteSuccess != null) SetRef(q, "successClip", sounds.qteSuccess);
                if (sounds.qteFail != null) SetRef(q, "failClip", sounds.qteFail);
                if (sounds.qteStep != null) SetRef(q, "stepClip", sounds.qteStep);
            }
        }

        if (sounds.jumpscare != null)
        {
            foreach (var g in FindAll<GameOverController>())
            {
                EnsureAudioSource(g, "_audioSource");
                SetRef(g, "_jumpscareClip", sounds.jumpscare);
            }
        }

        FixKnockSources();
        if (sounds.knock != null)
        {
            foreach (var e in FindAll<OfficeEntrance>())
                SetRefArray(e, "_knockClips", new Object[] { sounds.knock });
        }

        if (sounds.doorOpen != null || sounds.doorClose != null)
        {
            // Fonte partilhada do GameManager (sempre ativa), se a porta ainda não tiver uma fora dela
            AudioSource shared = null;
            var cams = FindAll<CameraController>();
            if (cams.Length > 0)
            {
                EnsureAudioSource(cams[0], "audioSource");
                shared = GetRef<AudioSource>(cams[0], "audioSource");
            }

            foreach (var d in FindAll<Door>())
            {
                if (shared != null) SetRef(d, "SoundPlayer", shared);
                else EnsureAudioSource(d, "SoundPlayer");
                if (sounds.doorOpen != null) SetRef(d, "OpenSound", sounds.doorOpen);
                if (sounds.doorClose != null) SetRef(d, "CloseSound", sounds.doorClose);
            }
        }

        if (sounds.backSafe != null || sounds.backDangerous != null || sounds.backKill != null)
        {
            foreach (var b in FindAll<BackThreat>())
            {
                if (sounds.backSafe != null) SetRef(b, "safeClip", sounds.backSafe);
                if (sounds.backDangerous != null) SetRef(b, "dangerousClip", sounds.backDangerous);
                if (sounds.backKill != null) SetRef(b, "killClip", sounds.backKill);
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"[FNaF Setup] Sons aplicados: {roomCount} sala(s), {camCount} CameraController, " +
                  $"{interferenceCount} CameraInterference.");
    }

    // Se o campo do AudioSource estiver vazio, usa (ou cria) um AudioSource no mesmo objeto
    private static void EnsureAudioSource(Component owner, string field)
    {
        var so = new SerializedObject(owner);
        var p = so.FindProperty(field);
        if (p == null || p.objectReferenceValue != null) return;

        var src = owner.GetComponent<AudioSource>();
        if (src == null) src = Undo.AddComponent<AudioSource>(owner.gameObject);
        src.playOnAwake = false;

        p.objectReferenceValue = src;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static AudioSource CreateKnockSource(Transform parent, string label)
    {
        var go = NewObj($"KnockSource_{label}", parent, typeof(AudioSource));
        var src = go.GetComponent<AudioSource>();
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        return src;
    }

    // Garante que cada entrada tem uma fonte de som fora do objeto da porta (que é desativado
    // quando o monitor abre). Corrige também setups antigos com o AudioSource na própria porta.
    private static void FixKnockSources()
    {
        GameObject root = null;
        int fixedCount = 0;

        foreach (var e in FindAll<OfficeEntrance>())
        {
            var so = new SerializedObject(e);
            var p = so.FindProperty("_knockSource");
            if (p == null) continue;

            var current = p.objectReferenceValue as AudioSource;
            if (current != null && !current.transform.IsChildOf(e.transform)) continue;

            if (root == null)
            {
                root = GameObject.Find("OfficeKnockSources");
                if (root == null) root = NewObj("OfficeKnockSources", null);
            }

            p.objectReferenceValue = CreateKnockSource(root.transform, e.gameObject.name);
            so.ApplyModifiedPropertiesWithoutUndo();
            if (current != null) Undo.DestroyObjectImmediate(current);
            fixedCount++;
        }

        if (fixedCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[FNaF Setup] {fixedCount} fonte(s) de som de bater movida(s) para fora das portas.");
        }
    }

    // O QTE usa o AudioSource do monitor (GameManager): o próprio objeto do QTE é desativado
    private static void LinkSharedAudio(Component qte)
    {
        var so = new SerializedObject(qte);
        var p = so.FindProperty("audioSource");
        if (p == null || p.objectReferenceValue != null) return;

        var monitors = FindAll<MonitorController>();
        if (monitors.Length == 0) return;

        EnsureAudioSource(monitors[0], "audioSource");
        var src = new SerializedObject(monitors[0]).FindProperty("audioSource").objectReferenceValue;
        if (src == null) return;

        p.objectReferenceValue = src;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T[] FindAll<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return Object.FindObjectsOfType<T>(true);
#endif
    }

    // ------------------------------------------------------------ Ligar campos serializados

    private static void SetRef(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogError($"[FNaF Setup] Campo '{field}' não encontrado em {target.GetType().Name}.");
            return;
        }
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(Object target, string field, string value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogError($"[FNaF Setup] Campo '{field}' não encontrado em {target.GetType().Name}.");
            return;
        }
        p.stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string field, float value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogError($"[FNaF Setup] Campo '{field}' não encontrado em {target.GetType().Name}.");
            return;
        }
        p.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(Object target, string field, int value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogError($"[FNaF Setup] Campo '{field}' não encontrado em {target.GetType().Name}.");
            return;
        }
        p.intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRefArray(Object target, string field, Object[] values)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null)
        {
            Debug.LogError($"[FNaF Setup] Campo '{field}' não encontrado em {target.GetType().Name}.");
            return;
        }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
