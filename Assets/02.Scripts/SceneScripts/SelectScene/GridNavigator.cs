using System.Collections.Generic;
using UnityEngine;

public struct GridNode
{
    public int id;
    public Vector2 pos;
}

public class GridNavigator
{ 
    // Ʃ�� �з�����
    private float forwardEpsilon = 0.0001f;         // ���� ���� �Ӱ�
    private float bandWidth = 160f;                 // ���� ��/���� �����ϴ� ��� ����
    private float weightForward = 1.0f;             // ���� �Ÿ� ����ġ
    private float weightSide = 0.65f;               // ���� ���� ����ġ
    private float lockedPenalty = 1e6f;             // ��Ȱ�� �ĺ� �о��

    // ������
    List<GridNode> _nodes = new List<GridNode>();

    // <summary> View�� ���̾ƿ��� ��ģ ������ ȣ�� </summary>
    void SetSnapShot(IReadOnlyList<GridNode> nodes)
    {

    }

    // <summary> ���� ��Ŀ�� id�� �Է� ���� ���ͷ� ���� ��Ŀ�� id�� ���� </summary>
    bool TryNavigate(int currentId, Vector2 inputDir, out int nextId)
    {
        nextId = currentId;
        return false;
    }

    // <summary> �� ���� + ����Ŭ ��:
    // �¿� �Է��̸� x��, ���� �Է��̸� y��
    // ���� �������� �������� ����/���� �ĺ��� ã�� ������ �ݴ� ������ ��
    // ��Ȱ���� ���� </summary>
    int WrapAxisCycle(int currentId, Vector2 inputDir)
    {
        return currentId;
    }
}
