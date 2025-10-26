using System.Collections.Generic;
using UnityEngine;

public class WinMarks : MonoBehaviour
{
    private List<GameObject> p1Backgrounds;
    private List<GameObject> p2Backgrounds;

    private List<GameObject> p1WinMarks;
    private List<GameObject> p2WinMarks;

    private void Awake()
    {
        p1Backgrounds = new List<GameObject>();
        p2Backgrounds = new List<GameObject>();

        p1WinMarks = new List<GameObject>();
        p2WinMarks = new List<GameObject>();

        var p1 = transform.GetChild(0);
        var p2 = transform.GetChild(1);

        for (int i = 0; i < p1.childCount; i++)
        {
            p1Backgrounds.Add(p1.GetChild(i).gameObject);
            p2Backgrounds.Add(p2.GetChild(i).gameObject);
        }

        foreach (var back in p1Backgrounds)
        {
            var mark = back.transform.GetChild(0).gameObject;
            p1WinMarks.Add(mark);
            mark.SetActive(false);
            back.SetActive(false);
        }

        foreach (var back in p2Backgrounds)
        {
            var mark = back.transform.GetChild(0).gameObject;
            p2WinMarks.Add(mark);
            mark.SetActive(false);
            back.SetActive(false);
        }

    }

    public void Init(int roundsToWin)
    {
        for (int i = 0; i < roundsToWin; i++)
        {
            p1Backgrounds[i].SetActive(true);
            p2Backgrounds[i].SetActive(true);
        }
    }

    public void UpdateWinCount(int p1Wins, int p2Wins)
    {
        for (int i = 0; i < p1WinMarks.Count; i++)
        {
            p1WinMarks[i].SetActive(i < p1Wins);
        }

        for (int i = 0; i < p2WinMarks.Count; i++)
        {
            p2WinMarks[i].SetActive(i < p2Wins);
        }
    }
}
