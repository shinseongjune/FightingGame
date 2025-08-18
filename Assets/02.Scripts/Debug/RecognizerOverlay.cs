using UnityEngine;

public class RecognizerOverlay : MonoBehaviour
{
    public Vector2 anchor = new Vector2(20, 380);
    public float lineHeight = 18;

    void OnGUI()
    {
        var frame = RecognizerTrace.GetDisplayFrame();
        var list = frame.attempts;
        if (list == null || list.Count == 0) return;

        float y = anchor.y;
        GUI.color = Color.white;
        GUI.Label(new Rect(anchor.x, y, 700, lineHeight),
            $"<b>Recognize Attempts (Frame {frame.frame})</b>"); y += lineHeight;

        foreach (var a in list)
        {
            GUI.color = a.success ? Color.green : Color.red;
            string reason = a.success ? "SUCCESS" : ("FAIL: " + (string.IsNullOrEmpty(a.failReason) ? "(no-reason)" : a.failReason));
            GUI.Label(new Rect(anchor.x, y, 950, lineHeight),
                $"{a.skillName}  {reason}  atkIdx:{a.attackIdx}  gaps:{a.gapsUsed}/{a.maxGap}  sameFrm:{a.sameFrameDirsAllowed}");
            y += lineHeight;

            if (a.matchedIdx != null && a.matchedIdx.Count > 0)
            {
                GUI.color = Color.white;
                GUI.Label(new Rect(anchor.x + 20, y, 950, lineHeight),
                    $"matched idx: [{string.Join(", ", a.matchedIdx)}]");
                y += lineHeight;
            }
        }
            GUI.color = Color.white;
        }
    }
