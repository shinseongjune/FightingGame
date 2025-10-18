using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Input/Macro")]
public class InputMacro_SO : ScriptableObject
{
    [System.Serializable]
    public struct Step
    {
        public int frameOffset;     // ���� ���� �� ������ �Ŀ�
        public Direction direction; // ����
        public AttackKey attack;    // Ű ��Ʈ����ũ
        public int holdFrames;      // ���� �Է� ���� ������ �� (0�̸� 1������)
    }

    public List<Step> steps = new();
    public bool loop = true;
}