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

    private static bool Match(Queue<InputData> buffer, SkillInputData command)
    {
        if (command.inputData.Length == 0 || buffer.Count == 0)
            return false;

        InputData[] inputs = buffer.ToArray();
        int bufferIndex = inputs.Length - 1;
        int cmdIndex = command.inputData.Length - 1;
        int gapCount = 0;

        // 공격 입력 먼저 검사
        InputData? attackCmd = null;

        attackCmd = command.inputData[cmdIndex];
        if (!MatchAttack(inputs, attackCmd.Value))
            return false;

        cmdIndex--; // 나머지는 방향 매칭용

        // 방향 입력 역순으로 검사
        while (cmdIndex >= 0 && bufferIndex >= 0)
        {
            InputData input = inputs[bufferIndex];
            InputData expected = command.inputData[cmdIndex];

            if (MatchInput(input, expected, command.isStrict))
            {
                cmdIndex--;
                gapCount = 0;
            }
            else
            {
                gapCount++;
                if (gapCount > command.maxFrameGap)
                    return false;
            }

            bufferIndex--;
        }

        return cmdIndex < 0;
    }

    private static bool MatchInput(InputData actual, InputData expected, bool isStrict)
    {
        // 차지 조건만 충족되면 방향 무시하고 성공
        if ((expected.backCharge != 0 && actual.backCharge >= expected.backCharge) &&
            (expected.downCharge != 0 && actual.downCharge >= expected.downCharge))
        {
            return true;
        }

        if (isStrict)
        {
            return actual.direction == expected.direction;
        }
        else
        {
            return DirectionMatches(actual.direction, expected.direction);
        }
    }

    private static bool MatchAttack(InputData[] inputs, InputData attackCmd)
    {
        for (int i = inputs.Length - 1; i >= 0; i--)
        {
            if ((inputs[i].attack & attackCmd.attack) != 0)
            {
                return true; // 공격 키만 확인
            }
        }
        return false;
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

            // 대각선: 구성 요소를 포함하면 허용
            Direction.UpForward => actual is Direction.Up or Direction.Forward or Direction.UpForward,
            Direction.UpBack => actual is Direction.Up or Direction.Back or Direction.UpBack,
            Direction.DownForward => actual is Direction.Down or Direction.Forward or Direction.DownForward,
            Direction.DownBack => actual is Direction.Down or Direction.Back or Direction.DownBack,

            Direction.Neutral => actual is Direction.Neutral,

            _ => false,
        };
    }
}
