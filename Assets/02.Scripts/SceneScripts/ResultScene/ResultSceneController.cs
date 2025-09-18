using UnityEngine;
using UnityEngine.UI;

public class ResultSceneController : MonoBehaviour
{
    [SerializeField] Text resultText;

    void Start()
    {
        var res = GameManager.Instance.lastResult;
        string who = res.winnerSlot == 1 ? "P1" : res.winnerSlot == 2 ? "P2" : "DRAW";
        resultText.text = $"{who} WINS\n{res.p1Rounds} - {res.p2Rounds}";
    }
}
