using UnityEngine;

public sealed class AIInputProvider : IInputProvider
{
    private readonly InputMacro_SO macro;
    private readonly CharacterProperty owner;

    private long startTick = -1;
    private int macroLength; // 총 프레임 길이(holdFrames 포함 확장)

    public AIInputProvider(InputMacro_SO macro, CharacterProperty owner)
    {
        this.macro = macro;
        this.owner = owner;
        macroLength = CalcMacroLength(macro);
    }

    public InputData GetSnapshot()
    {
        long tick = Tick();
        if (macro == null || macro.steps.Count == 0)
            return Stamp(default, tick);

        int t = (int)(tick - startTick);
        if (t < 0) return Stamp(default, tick);

        if (!macro.loop && t >= macroLength)
            return Stamp(default, tick);

        int local = macro.loop ? (t % macroLength) : Mathf.Min(t, macroLength - 1);

        // local 시각에 해당하는 Step을 찾아서 반환
        int acc = 0;
        foreach (var s in macro.steps)
        {
            int span = Mathf.Max(1, s.holdFrames);
            int begin = s.frameOffset + acc;
            int end = begin + span; // [begin, end)

            if (local >= begin && local < end)
            {
                var d = new InputData { direction = s.direction, attack = s.attack };
                return Stamp(d, tick);
            }
            acc += span;
        }
        return Stamp(default, tick);
    }

    private long Tick()
    {
        long curr = TickMaster.Instance != null ? TickMaster.Instance.CurrentTick : 0;
        if (startTick < 0) startTick = curr;
        return curr;
    }

    private static int CalcMacroLength(InputMacro_SO m)
    {
        if (m == null || m.steps.Count == 0) return 0;
        int last = 0;
        int acc = 0;
        foreach (var s in m.steps)
        {
            int span = Mathf.Max(1, s.holdFrames);
            last = Mathf.Max(last, s.frameOffset + acc + span);
            acc += span;
        }
        return Mathf.Max(1, last);
    }

    private static InputData Stamp(InputData d, long tick)
    {
        d.tick = tick;
        return d;
    }
}