// SelectSceneBootstrap.cs
using System.Collections.Generic;
using UnityEngine;

public class SelectSceneBootstrap : MonoBehaviour
{
    [SerializeField] UGUISelectSceneView view;
    [SerializeField] SelectSceneController controller;

    void Start()
    {
        // 1) 임시 데이터 구성
        var chars = new List<SelectableItemViewData>
        {
            new("TestMan",   "Ryu"),
            new("TestMan2",   "Ken"),
        };
        var stages = new List<SelectableItemViewData>
        {
            new("stagedojo", "Dojo"),
        };

        // 2) 잠금/히든 예시
        var locked = new[] { "char_blanka" };
        var hidden = new[] { "char_cammy" };

        // 3) 모델 생성
        var model = new SimpleSelectSceneModel(chars, stages, locked, hidden);

        // 4) 컨트롤러 초기화
        controller.Initialize(view, model);
    }
}
