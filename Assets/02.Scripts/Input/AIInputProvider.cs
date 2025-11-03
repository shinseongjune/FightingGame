using UnityEngine;

public sealed class AIInputProvider : IInputProvider
{
    readonly CharacterProperty me;
    readonly CharacterProperty enemy;

    public AIInputProvider(CharacterProperty owner, CharacterProperty other)
    {
        me = owner;
        enemy = other;
    }

    public InputData GetSnapshot()
    {
        InputData input = new();
        // ai 로직
        // 1. 의도를 만든다.
        // 2. 의도에 맞는 input을 입력.
        // 3. 의도 재평가를 랜덤 타이머로 한다.
        //
        // 
        return input;
    }
}