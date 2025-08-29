// SelectSceneBootstrap.cs
using System.Collections.Generic;
using UnityEngine;

public class SelectSceneBootstrap : MonoBehaviour
{
    [SerializeField] UGUISelectSceneView view;
    [SerializeField] SelectSceneController controller;

    void Start()
    {
        // 1) �ӽ� ������ ����
        var chars = new List<SelectableItemViewData>
        {
            new("char_ryu",   "Ryu"),
            new("char_ken",   "Ken"),
            new("char_chun",  "Chun"),
            new("char_guile", "Guile"),
            new("char_blanka","Blanka"),
            new("char_cammy", "Cammy"),
        };
        var stages = new List<SelectableItemViewData>
        {
            new("stagedojo", "Dojo"),
            new("stagecity", "Downtown"),
            new("stagebay",  "Harbor")
        };

        // 2) ���/���� ����
        var locked = new[] { "char_blanka" };
        var hidden = new[] { "char_cammy" };

        // 3) �� ����
        var model = new SimpleSelectSceneModel(chars, stages, locked, hidden);

        // 4) ��Ʈ�ѷ� �ʱ�ȭ
        controller.Initialize(view, model);
    }
}
