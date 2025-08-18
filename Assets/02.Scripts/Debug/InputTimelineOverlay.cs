using UnityEngine;
using UnityEngine.InputSystem;

public class InputTimelineOverlay : MonoBehaviour
{
    public int maxFramesShown = 30;
    public Vector2 anchor = new Vector2(20, 20);
    public float lineHeight = 18;

    //void Update()
    //{
    //    var kb = Keyboard.current;
    //    if (kb != null)
    //    {
    //        if (kb.f1Key.wasPressedThisFrame) RecognizerTrace.ToggleFreeze();
    //        if (kb.leftArrowKey.wasPressedThisFrame) RecognizerTrace.Step(-1);
    //        if (kb.rightArrowKey.wasPressedThisFrame) RecognizerTrace.Step(+1);
    //    }
    //
    //    var pad = Gamepad.current;
    //    if (pad != null)
    //    {
    //        if (pad.startButton.wasPressedThisFrame) RecognizerTrace.ToggleFreeze();
    //        if (pad.dpad.left.wasPressedThisFrame) RecognizerTrace.Step(-1);
    //        if (pad.dpad.right.wasPressedThisFrame) RecognizerTrace.Step(+1);
    //    }
    //}

    string DirToShort(Direction d)
    {
        return d switch
        {
            Direction.Neutral => "5",
            Direction.Forward => "6",
            Direction.Back => "4",
            Direction.Up => "8",
            Direction.Down => "2",
            Direction.UpForward => "9",
            Direction.UpBack => "7",
            Direction.DownForward => "3",
            Direction.DownBack => "1",
            _ => "?"
        };
    }

    string AttackToShort(AttackKey a)
    {
        if (a == AttackKey.None) return "";
        System.Text.StringBuilder sb = new();
        if (a.HasFlag(AttackKey.LP)) sb.Append("LP ");
        if (a.HasFlag(AttackKey.MP)) sb.Append("MP ");
        if (a.HasFlag(AttackKey.HP)) sb.Append("HP ");
        if (a.HasFlag(AttackKey.LK)) sb.Append("LK ");
        if (a.HasFlag(AttackKey.MK)) sb.Append("MK ");
        if (a.HasFlag(AttackKey.HK)) sb.Append("HK ");
        return sb.ToString().TrimEnd();
    }

    void OnGUI()
    {
        var frame = RecognizerTrace.GetDisplayFrame();
        var buf = frame.buffer;
        if (buf == null || buf.Length == 0) return;

        // 상단 상태 라벨
        GUI.color = Color.white;
        string mode = RecognizerTrace.Frozen ? $"FREEZE [{RecognizerTrace.ViewIndex + 1} / ?]" : "LIVE";
        GUI.Label(new Rect(anchor.x, anchor.y - lineHeight, 600, lineHeight),
            $"{mode}   Frame:{frame.frame}   Time:{frame.time:0.000}   (F1:Freeze  ←/→:Step)");

        int start = Mathf.Max(0, buf.Length - maxFramesShown);
        int row = 0;

        // 최근 시도(이 프레임의 Attempt들)
        var atts = frame.attempts;

        for (int i = start; i < buf.Length; i++)
        {
            Color c = Color.white;
            bool isAttack = false;
            bool matched = false;

            foreach (var at in atts)
            {
                if (at.attackIdx == i) isAttack = true;
                if (at.success && at.matchedIdx.Contains(i)) matched = true;
            }

            if (matched) c = Color.green;
            else if (isAttack) c = Color.yellow;
            else if (buf[i].isUsed) c = new Color(0.6f, 0.8f, 1f, 1f);

            GUI.color = c;
            GUI.Label(new Rect(anchor.x, anchor.y + row * lineHeight, 520, lineHeight),
                $"[{i}] dir:{DirToShort(buf[i].direction)}  atk:{AttackToShort(buf[i].attack)}  bc:{buf[i].backCharge} dc:{buf[i].downCharge}{(buf[i].isUsed ? " (used)" : "")}");
            row++;
        }
        GUI.color = Color.white;
    }
}
