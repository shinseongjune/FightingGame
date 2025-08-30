using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerId { P1, P2 }

/// <summary>
/// View가 넘겨주는 지오메트리 스냅샷(순수 데이터)
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
/// 그리드에 바인딩할 표시 데이터(썸네일/이름 등)
/// - Unity 직렬화를 고려해 public 필드 + 파라미터 없는 생성자 유지
/// </summary>
[Serializable]
public class SelectableItemViewData
{
    public string Id;
    public string DisplayName;
    public string AddressableKeyThumb; // null 가능

    public SelectableItemViewData() { } // Unity/직렬화용

    public SelectableItemViewData(string id, string displayName, string addressableKeyThumb = null)
    {
        Id = id;
        DisplayName = displayName;
        AddressableKeyThumb = addressableKeyThumb;
    }
}

/// <summary>
/// 디테일 패널 등에 쓸 부가 정보
/// </summary>
[Serializable]
public class DetailViewData
{
    public string Title;
    public string Desc;    // null 가능
    public object Extra;   // 필요시 임의 객체

    public DetailViewData() { }

    public DetailViewData(string title, string desc = null, object extra = null)
    {
        Title = title;
        Desc = desc;
        Extra = extra;
    }
}

/// <summary>
/// 최종 결과 DTO (다음 씬으로 넘길 값)
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