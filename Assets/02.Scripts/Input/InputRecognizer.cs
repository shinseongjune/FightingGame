using System.Collections.Generic;

public static class InputRecognizer
{
    private const int SAME_FRAME_DIRS_ALLOWED_DEFAULT = 1;

    public static Skill_SO Recognize(Queue<InputData> buffer, List<Skill_SO> skills)
    {
        // 스냅샷(복사본)
        var inputs = buffer.ToArray();
        RecognizerTrace.BeginFrame(inputs);

        foreach (var skill in skills)
        {
            var attempt = RecognizerTrace.BeginAttempt(
                skillName: skill.name,
                maxGap: skill.command.maxFrameGap,
                sameFrameDirsAllowed: SAME_FRAME_DIRS_ALLOWED_DEFAULT
            );

            if (TryMatchAndMark(inputs, skill.command, out var usedMask, attempt))
            {
                RecognizerTrace.Success(attempt);

                // ★원본 Queue 갱신: usedMask가 true인 인덱스에 isUsed=true 설정
                buffer.Clear();
                for (int i = 0; i < inputs.Length; i++)
                {
                    if (usedMask[i])
                    {
                        var t = inputs[i];
                        t.isUsed = true;
                        inputs[i] = t; // 값형(struct)이라 재대입 필요
                    }
                    buffer.Enqueue(inputs[i]);
                }
                return skill;
            }
        }
        return null;
    }

    // 매칭 + 어떤 인덱스를 소비했는지 마스크로 리턴
    private static bool TryMatchAndMark(InputData[] inputs, SkillInputData cmd, out bool[] used, RecognizerTrace.Attempt trace)
    {
        used = null;
        if (cmd.inputData.Length == 0) { RecognizerTrace.Fail(trace, "Empty cmd"); return false; }
        if (inputs.Length == 0) { RecognizerTrace.Fail(trace, "Empty buffer"); return false; }

        // 1) 공격 앵커 찾기 (가장 나중 프레임)
        var attackCmd = cmd.inputData[^1];
        int attackIdx = FindAttackIndex(inputs, attackCmd);
        if (attackIdx < 0) { RecognizerTrace.Fail(trace, "No attack"); return false; }
        RecognizerTrace.MarkAttack(trace, attackIdx);

        // (선택) 공격이 너무 오래되면 컷
        const int ATTACK_WINDOW = 8;
        if (inputs.Length - 1 - attackIdx > ATTACK_WINDOW) { RecognizerTrace.Fail(trace, "Attack too old"); return false; }

        int cmdIndex = cmd.inputData.Length - 2;
        int bufferIndex = attackIdx;
        int gap = 0;
        int sameFrameDirsLeft = trace.sameFrameDirsAllowed;
        var matched = new List<int> { attackIdx }; // 공격 프레임 소비

        while (cmdIndex >= 0 && bufferIndex >= 0)
        {
            var actual = inputs[bufferIndex];
            var expected = cmd.inputData[cmdIndex];
            bool isSameFrame = (bufferIndex == attackIdx);

            if (!actual.isUsed && MatchInput(actual, expected, cmd.isStrict))
            {
                if (isSameFrame && sameFrameDirsLeft <= 0)
                {
                    bufferIndex--; // 같은 프레임은 다 썼으니 이전 프레임으로 이동
                    continue;
                }
                matched.Add(bufferIndex);
                RecognizerTrace.MarkMatch(trace, bufferIndex);
                cmdIndex--;
                         if (isSameFrame) sameFrameDirsLeft--;
                gap = 0; // ★ 매칭했으면 gap 리셋
            }
            else if (++gap > cmd.maxFrameGap) { RecognizerTrace.Fail(trace, "Gap limit"); return false; }
            else RecognizerTrace.MarkGap(trace, gap);

            bufferIndex--;
        }
        if (cmdIndex >= 0) { RecognizerTrace.Fail(trace, "Ran out"); return false; }

        // 3) 소비 마스크 구성
        used = new bool[inputs.Length];
        for (int i = 0; i < matched.Count; i++)
            used[matched[i]] = true;

        return true;
    }

    private static int FindAttackIndex(InputData[] inputs, InputData attackCmd)
    {
        for (int i = inputs.Length - 1; i >= 0; --i)
            if (!inputs[i].isUsed && (inputs[i].attack & attackCmd.attack) != 0) return i;
        return -1;
    }

    private static bool MatchInput(InputData actual, InputData expected, bool strict)
    {
        // 차지 먼저
        if (expected.backCharge > 0 || expected.downCharge > 0)
            return actual.backCharge >= expected.backCharge && actual.downCharge >= expected.downCharge;

        return strict
            ? actual.direction == expected.direction
            : DirectionMatches(actual.direction, expected.direction);
    }

    private static bool DirectionMatches(Direction actual, Direction expected)
    {
        // expected 방향이 포함 관계인지 loose 매칭
        return expected switch
        {
            Direction.Forward => actual is Direction.Forward or Direction.UpForward or Direction.DownForward,
            Direction.Back => actual is Direction.Back or Direction.UpBack or Direction.DownBack,
            Direction.Up => actual is Direction.Up or Direction.UpForward or Direction.UpBack,
            Direction.Down => actual is Direction.Down or Direction.DownForward or Direction.DownBack,

            // 대각선은 철저하게 매칭함 (216 등 잘못된 커맨드 입력 방지)
            Direction.UpForward => actual is Direction.UpForward,
            Direction.UpBack => actual is Direction.UpBack,
            Direction.DownForward => actual is Direction.DownForward,
            Direction.DownBack => actual is Direction.DownBack,

            Direction.Neutral => actual is Direction.Neutral,

            _ => false,
        };
    }
}
