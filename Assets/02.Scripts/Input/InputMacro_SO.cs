using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Input/Macro")]
public class InputMacro_SO : ScriptableObject
{
    [System.Serializable]
    public struct Step
    {
        public int frameOffset;     // 시작 기준 몇 프레임 후에
        public Direction direction; // 방향
        public AttackKey attack;    // 키 비트마스크
        public int holdFrames;      // 같은 입력 유지 프레임 수 (0이면 1프레임)
    }

    public List<Step> steps = new();
    public bool loop = true;
}