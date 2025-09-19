// InputTraceHUD.cs (공격키 토큰 표시 확장)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputTraceHUD : MonoBehaviour, ITicker
{
    private TickMaster _tm;

    [Header("Source")]
    public InputBuffer source;                 // 비워두면 GetComponent로 자동
    public int maxItems = 14;                  // 표시 라인 수

    [Header("Display")]
    public bool visible = true;
    public bool hideNeutral = false;           // 중립 ● 숨김(단, 공격이 있으면 표시)
    public Vector2 anchor = new Vector2(12, 12);
    public float lineHeight = 20f;
    public float arrowWidth = 28f;
    public float frameWidth = 40f;
    public float tokenWidth = 52f;             // [LP], [LP+MP] 폭
    public int fontSize = 16;

    [Header("Buttons")]
    public bool persistButtons = true;
    public bool showButtons = true;            // 공격키 토큰 켜기/끄기
    public int attackFlashFrames = 12;         // 토큰 표시 수명(프레임)
    public int maxTokensPerLine = 8;           // 라인별 최대 토큰 수(오른쪽으로 쌓임)

    [Header("Toggle (New Input System, Play Mode only)")]
    public Key toggleKey = Key.F9;             // F2/F1은 에디터 기본과 충돌 → F9 권장
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

    // 고정 틱: 입력 스냅샷을 압축 기록(방향 라인 + 공격 토큰)
    public void Tick()
    {
        if (source == null) return;

        var dir = source.LastInput.direction;
        var atk = source.LastInput.attack; // Flags: LP/MP/HP/LK/MK/HK

        // 1) 라인 유지/추가(중립 숨김 옵션: 공격이 없을 때만 숨김)
        bool hasAttack = atk != AttackKey.None;
        if (hideNeutral && dir == Direction.Neutral && !hasAttack)
        {
            // 직전이 Neutral이면 프레임만 축적
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

        // 2) 공격키가 눌린 프레임이면 토큰 추가
        if (showButtons && hasAttack)
        {
            var label = AttackLabel(atk);
            var line = items[0];

            if (persistButtons)
            {
                // 지속 모드: 같은 라벨이면 스택 증가, 아니면 새 토큰 추가
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
                // 기존 깜빡 모드(수명형)
                var tok = new Token { label = label, count = 1 }; // count=1은 의미상 placeholder
                line.tokens.Add(tok);
                if (line.tokens.Count > maxTokensPerLine)
                    line.tokens.RemoveAt(0);
                // TTL 감소는 아래 3)에서 처리(keep)
            }

            items[0] = line;
        }

        // 3) 토큰 수명 감소/정리
        if (!persistButtons)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].tokens == null || items[i].tokens.Count == 0) continue;
                // 필요하다면 각 토큰에 대한 별도 ttl 리스트를 두거나,
                // 간단히 "최근 몇 틱만 유지"하는 큐로 관리했을 수도 있음.
                // 현재 구조를 간단히 유지하려면 토큰에 별도 ttl 필드를 임시로 두고 관리하면 됨.
                // (아래는 예시용으로 전체를 조금씩 지우는 단순 로직 제거/유지 선택)
            }
        }
    }

    void Update()
    {
        if (!Application.isPlaying) return; // 에디터 단축키 충돌 방지
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

            // 화살표
            GUI.Label(new Rect(6f, y, arrowWidth, lineHeight), ArrowGlyph(it.dir), arrowStyle);
            // 프레임
            GUI.Label(new Rect(6f + arrowWidth, y, frameWidth, lineHeight), $"{Mathf.Min(it.frames, 999)}", textStyle);

            // 토큰 라벨 표시: persist면 "라벨×카운트" 형태
            if (showButtons && it.tokens != null && it.tokens.Count > 0)
            {
                float x = 6f + arrowWidth + frameWidth;
                for (int t = 0; t < it.tokens.Count; t++)
                {
                    var tok = it.tokens[t];
                    string text = tok.count > 1 ? $"{tok.label}×{Mathf.Min(tok.count, 999)}" : tok.label;

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

    // InputTraceHUD.cs 내부(같은 클래스 안)
    int MaxTokenColumns(int shown)
    {
        int m = 0;
        for (int i = 0; i < shown && i < items.Count; i++)
        {
            var toks = items[i].tokens;
            if (toks != null && toks.Count > m) m = toks.Count;
        }
        // 최소 1칸은 확보(프레임만 있어도 박스 너비가 0이면 모양이 깨짐)
        return Mathf.Clamp(m, 1, maxTokensPerLine);
    }

    void BumpFrame(int idx, int cap = 999)
    {
        if (idx < 0 || idx >= items.Count) return;
        var it = items[idx];
        it.frames = Mathf.Min(it.frames + 1, cap);
        items[idx] = it; // struct 재대입
    }

    // 편의: AttackKey 플래그 → "LP", "LP+MP" 같은 라벨
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

    // 펀치/킥에 살짝 색 차이(선택)
    static Color ColorForToken(string label)
        => label.Contains("K") ? new Color(0.2f, 0.7f, 1f, 0.9f) : new Color(1f, 0.5f, 0.3f, 0.9f);

    static string ArrowGlyph(Direction d) => d switch
    {
        Direction.Up => "↑",
        Direction.Down => "↓",
        Direction.Forward => "→",
        Direction.Back => "←",
        Direction.UpForward => "↗",
        Direction.UpBack => "↖",
        Direction.DownForward => "↘",
        Direction.DownBack => "↙",
        Direction.Neutral => "●",
        _ => "?"
    };
}
