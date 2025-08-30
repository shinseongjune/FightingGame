using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerId { P1, P2 }

/// <summary>
/// View�� �Ѱ��ִ� ������Ʈ�� ������(���� ������)
/// </summary>
[Serializable]
public struct NodeGeom
{
    public int id;
    public Vector2 center;
    public Vector2 size;
    public bool locked;
    public bool hidden;
    public bool interactable;
}

/// <summary>
/// �׸��忡 ���ε��� ǥ�� ������(�����/�̸� ��)
/// - Unity ����ȭ�� ����� public �ʵ� + �Ķ���� ���� ������ ����
/// </summary>
[Serializable]
public class SelectableItemViewData
{
    public string Id;
    public string DisplayName;
    public string AddressableKeyThumb; // null ����

    public SelectableItemViewData() { } // Unity/����ȭ��

    public SelectableItemViewData(string id, string displayName, string addressableKeyThumb = null)
    {
        Id = id;
        DisplayName = displayName;
        AddressableKeyThumb = addressableKeyThumb;
    }
}

/// <summary>
/// ������ �г� � �� �ΰ� ����
/// </summary>
[Serializable]
public class DetailViewData
{
    public string Title;
    public string Desc;    // null ����
    public object Extra;   // �ʿ�� ���� ��ü

    public DetailViewData() { }

    public DetailViewData(string title, string desc = null, object extra = null)
    {
        Title = title;
        Desc = desc;
        Extra = extra;
    }
}

/// <summary>
/// ���� ��� DTO (���� ������ �ѱ� ��)
/// </summary>
[Serializable]
public class SelectionResult
{
    public string P1CharId;
    public string P2CharId;
    public string StageId;

    public SelectionResult() { }

    public SelectionResult(string p1CharId, string p2CharId, string stageId)
    {
        P1CharId = p1CharId;
        P2CharId = p2CharId;
        StageId = stageId;
    }
}