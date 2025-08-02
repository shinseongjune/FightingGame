using System.Collections.Generic;

public static class InputRecognizer
{
    public static Skill_SO Recognize(Queue<InputData> buffer, List<Skill_SO> skills)
    {
        foreach (Skill_SO skill in skills)
        {
            if (Match(buffer, skill.command))
                return skill;
        }
        return null;
    }

    private static bool Match(Queue<InputData> buffer, SkillInputData[] command)
    {
        if (command.Length == 0 || buffer.Count == 0)
            return false;

        InputData[] inputs = buffer.ToArray();
        int bufferIndex = inputs.Length - 1;
        int cmdIndex = command.Length - 1;
        int gapCount = 0;

        // ���� �Է� ���� �˻�
        SkillInputData? attackCmd = null;
        if (command[cmdIndex].inputData.attack != AttackKey.None)
        {
            attackCmd = command[cmdIndex];
            if (!MatchAttack(inputs, attackCmd.Value))
                return false;

            cmdIndex--; // �������� ���� ��Ī��
        }

        // ���� �Է� �������� �˻�
        while (cmdIndex >= 0 && bufferIndex >= 0)
        {
            InputData input = inputs[bufferIndex];
            SkillInputData expected = command[cmdIndex];

            if (MatchInput(input, expected))
            {
                cmdIndex--;
                gapCount = 0;
            }
            else
            {
                gapCount++;
                if (gapCount > expected.maxFrameGap)
                    return false;
            }

            bufferIndex--;
        }

        return cmdIndex < 0;
    }

    private static bool MatchInput(InputData actual, SkillInputData expected)
    {
        // ���� ���Ǹ� �����Ǹ� ���� �����ϰ� ����
        if ((expected.inputData.backCharge != 0 && actual.backCharge >= expected.inputData.backCharge) &&
            (expected.inputData.downCharge != 0 && actual.downCharge >= expected.inputData.downCharge))
        {
            return true;
        }

        if (expected.isStrict)
        {
            return actual.direction == expected.inputData.direction;
        }
        else
        {
            return DirectionMatches(actual.direction, expected.inputData.direction);
        }
    }

    private static bool MatchAttack(InputData[] inputs, SkillInputData attackCmd)
    {
        for (int i = inputs.Length - 1; i >= 0; i--)
        {
            if ((inputs[i].attack & attackCmd.inputData.attack) != 0)
            {
                return true; // ���� Ű�� Ȯ��
            }
        }
        return false;
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

            // �밢��: ���� ��Ҹ� �����ϸ� ���
            Direction.UpForward => actual is Direction.Up or Direction.Forward or Direction.UpForward,
            Direction.UpBack => actual is Direction.Up or Direction.Back or Direction.UpBack,
            Direction.DownForward => actual is Direction.Down or Direction.Forward or Direction.DownForward,
            Direction.DownBack => actual is Direction.Down or Direction.Back or Direction.DownBack,

            Direction.Neutral => actual is Direction.Neutral,

            _ => false,
        };
    }
}
