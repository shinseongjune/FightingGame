using System.Collections.Generic;

public static class InputRecognizer
{
    private const int SAME_FRAME_DIRS_ALLOWED_DEFAULT = 1;

    public static Skill_SO Recognize(Queue<InputData> buffer, List<Skill_SO> skills)
    {
        // ������(���纻)
        var inputs = buffer.ToArray();

        foreach (var skill in skills)
        {
            if (TryMatchAndMark(inputs, skill.command, out var usedMask))
            {
                // �ڿ��� Queue ����: usedMask�� true�� �ε����� isUsed=true ����
                buffer.Clear();
                for (int i = 0; i < inputs.Length; i++)
                {
                    if (usedMask[i])
                    {
                        var t = inputs[i];
                        t.isUsed = true;
                        inputs[i] = t; // ����(struct)�̶� ����� �ʿ�
                    }
                    buffer.Enqueue(inputs[i]);
                }
                return skill;
            }
        }
        return null;
    }

    // ��Ī + � �ε����� �Һ��ߴ��� ����ũ�� ����
    private static bool TryMatchAndMark(InputData[] inputs, SkillInputData cmd, out bool[] used)
    {
        used = null;
        if (cmd.inputData.Length == 0) { return false; }
        if (inputs.Length == 0) { return false; }

        // 1) ���� ��Ŀ ã�� (���� ���� ������)
        var attackCmd = cmd.inputData[^1];
        int attackIdx = FindAttackIndex(inputs, attackCmd);
        if (attackIdx < 0) { return false; }

        // (����) ������ �ʹ� �����Ǹ� ��
        const int ATTACK_WINDOW = 8;
        if (inputs.Length - 1 - attackIdx > ATTACK_WINDOW) { return false; }

        int cmdIndex = cmd.inputData.Length - 2;
        int bufferIndex = attackIdx;
        int gap = 0;
        var matched = new List<int> { attackIdx }; // ���� ������ �Һ�

        while (cmdIndex >= 0 && bufferIndex >= 0)
        {
            var actual = inputs[bufferIndex];
            var expected = cmd.inputData[cmdIndex];
            bool isSameFrame = (bufferIndex == attackIdx);

            if (!actual.isUsed && MatchInput(actual, expected, cmd.isStrict))
            {
                if (isSameFrame)
                {
                    bufferIndex--; // ���� �������� �� ������ ���� ���������� �̵�
                    continue;
                }
                matched.Add(bufferIndex);
                cmdIndex--;
                gap = 0; // ��Ī������ gap ����
            }
            else if (++gap > cmd.maxFrameGap) { return false; }

            bufferIndex--;
        }
        if (cmdIndex >= 0) { return false; }

        // 3) �Һ� ����ũ ����
        used = new bool[inputs.Length];
        for (int i = 0; i < matched.Count; i++)
            used[matched[i]] = true;

        return true;
    }

    private static int FindAttackIndex(InputData[] inputs, InputData attackCmd)
    {
        for (int i = inputs.Length - 1; i >= 0; --i)
            if (!inputs[i].isUsed && (inputs[i].attack & attackCmd.attack) == attackCmd.attack) return i;
        return -1;
    }

    private static bool MatchInput(InputData actual, InputData expected, bool strict)
    {
        // ���� ����
        if (expected.backCharge > 0 || expected.downCharge > 0)
            return actual.backCharge >= expected.backCharge && actual.downCharge >= expected.downCharge;

        return strict
            ? actual.direction == expected.direction
            : DirectionMatches(actual.direction, expected.direction);
    }

    private static bool DirectionMatches(Direction actual, Direction expected)
    {
        // expected ������ ���� �������� loose ��Ī
        return expected switch
        {
            Direction.Forward => actual is Direction.Forward or Direction.UpForward or Direction.DownForward,
            Direction.Back => actual is Direction.Back or Direction.UpBack or Direction.DownBack,
            Direction.Up => actual is Direction.Up or Direction.UpForward or Direction.UpBack,
            Direction.Down => actual is Direction.Down or Direction.DownForward or Direction.DownBack,

            // �밢���� ö���ϰ� ��Ī�� (216 �� �߸��� Ŀ�ǵ� �Է� ����)
            Direction.UpForward => actual is Direction.UpForward,
            Direction.UpBack => actual is Direction.UpBack,
            Direction.DownForward => actual is Direction.DownForward,
            Direction.DownBack => actual is Direction.DownBack,

            Direction.Neutral => actual is Direction.Neutral,

            _ => false,
        };
    }
}
