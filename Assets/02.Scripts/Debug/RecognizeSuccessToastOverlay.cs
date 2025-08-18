using UnityEngine;

public class RecognizeSuccessToastOverlay : MonoBehaviour
{
    [Header("Position")]
    public Vector2 anchor = new Vector2(20f, 20f);  // 좌상단 기준
    public bool topRight = true;                    // 우상단 배치할지
    public float lineHeight = 22f;
    public float width = 520f;

    [Header("Style")]
    public bool richText = true;
    public int fontSize = 16;

    void OnGUI()
    {
        var list = RecognizerTrace.successToasts;
        if (list == null || list.Count == 0) return;

        // 기본 스타일
        var style = new GUIStyle(GUI.skin.label);
        style.richText = richText;
        style.fontSize = fontSize;

        float now = Time.unscaledTime;
        float x = topRight ? (Screen.width - anchor.x - width) : anchor.x;
        float y = anchor.y;

        // 오래된 것은 여기서 제거하지 않고, 그리면서 알파 0 이하면 제거
        for (int i = list.Count - 1, row = 0; i >= 0; --i)
        {
            float age = now - list[i].t0Unscaled;
            float life = RecognizerTrace.SuccessToastDuration;
            if (age > life)
            {
                // 기간 지난 토스트는 제거
                RecognizerTrace.successToasts.RemoveAt(i);
                continue;
            }

            // 페이드 아웃
            float a = Mathf.Clamp01(1f - (age / life));
            var old = GUI.color;
            GUI.color = new Color(0.1f, 1f, 0.3f, a); // 연녹색, 알파 적용

            // 굵게 표시하고 싶으면 <b> 태그 (richText=true 일 때)
            string text = $"<b>{list[i].text}</b>";
            GUI.Label(new Rect(x, y + row * lineHeight, width, lineHeight), text, style);

            GUI.color = old;
            row++;
        }
    }
}
