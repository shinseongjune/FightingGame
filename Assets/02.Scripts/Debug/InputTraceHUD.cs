// InputTraceHUD.cs (����Ű ��ū ǥ�� Ȯ��)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputTraceHUD : MonoBehaviour, ITicker
{
    private TickMaster _tm;

    [Header("Source")]
    public InputBuffer source;                 // ����θ� GetComponent�� �ڵ�
    public int maxItems = 14;                  // ǥ�� ���� ��

    [Header("Display")]
    public bool visible = true;
    public bool hideNeutral = false;           // �߸� �� ����(��, ������ ������ ǥ��)
    public Vector2 anchor = new Vector2(12, 12);
    public float lineHeight = 20f;
    public float arrowWidth = 28f;
    public float frameWidth = 40f;
    public float tokenWidth = 52f;             // [LP], [LP+MP] ��
    public int fontSize = 16;

    [Header("Buttons")]
    public bool persistButtons = true;
    public bool showButtons = true;            // ����Ű ��ū �ѱ�/����
    public int attackFlashFrames = 12;         // ��ū ǥ�� ����(������)
    public int maxTokensPerLine = 8;           // ���κ� �ִ� ��ū ��(���������� ����)

    [Header("Toggle (New Input System, Play Mode only)")]
    public Key toggleKey = Key.F9;             // F2/F1�� ������ �⺻�� �浹 �� F9 ����
    private InputAction toggleAction;

    struct Token { public string label; public int count; }
    struct Item
    {
        public Direction dir;
        public int frames;
        public List<Token> tokens;
    }

    readonly List<Item> items = new();

    void Awake()
    {
        if (source == null) source = GetComponent<InputBuffer>();
    }

    void OnEnable()
    {
        _tm = TickMaster.Instance;
        _tm?.Register(this);

        if (Application.isPlaying)
        {
            toggleAction = new InputAction(type: InputActionType.Button, binding: $"<Keyboard>/{toggleKey.ToString().ToLower()}");
            toggleAction.Enable();
        }
    }
    void OnDisable()
    {
        _tm?.Unregister(this);
        toggleAction?.Disable();
        toggleAction?.Dispose();
        toggleAction = null;
    }

    // ���� ƽ: �Է� �������� ���� ���(���� ���� + ���� ��ū)
    public void Tick()
    {
        if (source == null) return;

        var dir = source.LastInput.direction;
        var atk = source.LastInput.attack; // Flags: LP/MP/HP/LK/MK/HK

        // 1) ���� ����/�߰�(�߸� ���� �ɼ�: ������ ���� ���� ����)
        bool hasAttack = atk != AttackKey.None;
        if (hideNeutral && dir == Direction.Neutral && !hasAttack)
        {
            // ������ Neutral�̸� �����Ӹ� ����
            if (items.Count > 0 && items[0].dir == Direction.Neutral)
                BumpFrame(0);
            return;
        }

        bool sameAsLast = items.Count > 0 && items[0].dir == dir;
        if (!sameAsLast)
        {
            items.Insert(0, new Item { dir = dir, frames = 1, tokens = new List<Token>(4) });
            if (items.Count > maxItems) items.RemoveAt(items.Count - 1);
        }
        else
        {
            BumpFrame(0);
        }

        // 2) ����Ű�� ���� �������̸� ��ū �߰�
        if (showButtons && hasAttack)
        {
            var label = AttackLabel(atk);
            var line = items[0];

            if (persistButtons)
            {
                // ���� ���: ���� ���̸� ���� ����, �ƴϸ� �� ��ū �߰�
                if (line.tokens.Count > 0 && line.tokens[^1].label == label)
                {
                    var last = line.tokens[^1];
                    last.count = Mathf.Min(last.count + 1, 999);
                    line.tokens[^1] = last;
                }
                else
                {
                    line.tokens.Add(new Token { label = label, count = 1 });
                    if (line.tokens.Count > maxTokensPerLine)
                        line.tokens.RemoveAt(0);
                }
            }
            else
            {
                // ���� ���� ���(������)
                var tok = new Token { label = label, count = 1 }; // count=1�� �ǹ̻� placeholder
                line.tokens.Add(tok);
                if (line.tokens.Count > maxTokensPerLine)
                    line.tokens.RemoveAt(0);
                // TTL ���Ҵ� �Ʒ� 3)���� ó��(keep)
            }

            items[0] = line;
        }

        // 3) ��ū ���� ����/����
        if (!persistButtons)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].tokens == null || items[i].tokens.Count == 0) continue;
                // �ʿ��ϴٸ� �� ��ū�� ���� ���� ttl ����Ʈ�� �ΰų�,
                // ������ "�ֱ� �� ƽ�� ����"�ϴ� ť�� �������� ���� ����.
                // ���� ������ ������ �����Ϸ��� ��ū�� ���� ttl �ʵ带 �ӽ÷� �ΰ� �����ϸ� ��.
                // (�Ʒ��� ���ÿ����� ��ü�� ���ݾ� ����� �ܼ� ���� ����/���� ����)
            }
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return; // ������ ����Ű �浹 ����
        if (toggleAction != null && toggleAction.WasPerformedThisFrame())
            visible = !visible;
    }

    void OnGUI()
    {
        if (!visible) return;

        var textStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize, alignment = TextAnchor.MiddleLeft };
        var arrowStyle = new GUIStyle(GUI.skin.label) { fontSize = fontSize + 2, alignment = TextAnchor.MiddleCenter };
        var tokStyle = new GUIStyle(GUI.skin.box) { fontSize = fontSize - 2, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(6, 6, 2, 2) };

        int shown = Mathf.Min(items.Count, maxItems);
        float w = arrowWidth + frameWidth + tokenWidth * Mathf.Max(1, MaxTokenColumns(shown));
        float h = shown * lineHeight + 10f;
        GUILayout.BeginArea(new Rect(anchor.x, anchor.y, w, h), GUI.skin.box);

        for (int i = 0; i < shown; i++)
        {
            var it = items[i];
            float y = 5f + i * lineHeight;

            // ȭ��ǥ
            GUI.Label(new Rect(6f, y, arrowWidth, lineHeight), ArrowGlyph(it.dir), arrowStyle);
            // ������
            GUI.Label(new Rect(6f + arrowWidth, y, frameWidth, lineHeight), $"{Mathf.Min(it.frames, 999)}", textStyle);

            // ��ū �� ǥ��: persist�� "�󺧡�ī��Ʈ" ����
            if (showButtons && it.tokens != null && it.tokens.Count > 0)
            {
                float x = 6f + arrowWidth + frameWidth;
                for (int t = 0; t < it.tokens.Count; t++)
                {
                    var tok = it.tokens[t];
                    string text = tok.count > 1 ? $"{tok.label}��{Mathf.Min(tok.count, 999)}" : tok.label;

                    var r = new Rect(x, y + 2f, tokenWidth - 4f, lineHeight - 4f);
                    GUI.color = ColorForToken(text);
                    GUI.Box(r, text, tokStyle);
                    GUI.color = Color.white;

                    x += tokenWidth;
                }
            }
        }
        GUILayout.EndArea();
    }

    // InputTraceHUD.cs ����(���� Ŭ���� ��)
    int MaxTokenColumns(int shown)
    {
        int m = 0;
        for (int i = 0; i < shown && i < items.Count; i++)
        {
            var toks = items[i].tokens;
            if (toks != null && toks.Count > m) m = toks.Count;
        }
        // �ּ� 1ĭ�� Ȯ��(�����Ӹ� �־ �ڽ� �ʺ� 0�̸� ����� ����)
        return Mathf.Clamp(m, 1, maxTokensPerLine);
    }

    void BumpFrame(int idx, int cap = 999)
    {
        if (idx < 0 || idx >= items.Count) return;
        var it = items[idx];
        it.frames = Mathf.Min(it.frames + 1, cap);
        items[idx] = it; // struct �����
    }

    // ����: AttackKey �÷��� �� "LP", "LP+MP" ���� ��
    static string AttackLabel(AttackKey a)
    {
        if (a == AttackKey.None) return "";
        var list = new List<string>(3);
        if ((a & AttackKey.LP) != 0) list.Add("LP");
        if ((a & AttackKey.MP) != 0) list.Add("MP");
        if ((a & AttackKey.HP) != 0) list.Add("HP");
        if ((a & AttackKey.LK) != 0) list.Add("LK");
        if ((a & AttackKey.MK) != 0) list.Add("MK");
        if ((a & AttackKey.HK) != 0) list.Add("HK");
        return string.Join("+", list);
    }

    // ��ġ/ű�� ��¦ �� ����(����)
    static Color ColorForToken(string label)
        => label.Contains("K") ? new Color(0.2f, 0.7f, 1f, 0.9f) : new Color(1f, 0.5f, 0.3f, 0.9f);

    static string ArrowGlyph(Direction d) => d switch
    {
        Direction.Up => "��",
        Direction.Down => "��",
        Direction.Forward => "��",
        Direction.Back => "��",
        Direction.UpForward => "��",
        Direction.UpBack => "��",
        Direction.DownForward => "��",
        Direction.DownBack => "��",
        Direction.Neutral => "��",
        _ => "?"
    };
}
