using UnityEngine;

public class RecognizeSuccessToastOverlay : MonoBehaviour
{
    [Header("Position")]
    public Vector2 anchor = new Vector2(20f, 20f);  // �»�� ����
    public bool topRight = true;                    // ���� ��ġ����
    public float lineHeight = 22f;
    public float width = 520f;

    [Header("Style")]
    public bool richText = true;
    public int fontSize = 16;

    void OnGUI()
    {
        var list = RecognizerTrace.successToasts;
        if (list == null || list.Count == 0) return;

        // �⺻ ��Ÿ��
        var style = new GUIStyle(GUI.skin.label);
        style.richText = richText;
        style.fontSize = fontSize;

        float now = Time.unscaledTime;
        float x = topRight ? (Screen.width - anchor.x - width) : anchor.x;
        float y = anchor.y;

        // ������ ���� ���⼭ �������� �ʰ�, �׸��鼭 ���� 0 ���ϸ� ����
        for (int i = list.Count - 1, row = 0; i >= 0; --i)
        {
            float age = now - list[i].t0Unscaled;
            float life = RecognizerTrace.SuccessToastDuration;
            if (age > life)
            {
                // �Ⱓ ���� �佺Ʈ�� ����
                RecognizerTrace.successToasts.RemoveAt(i);
                continue;
            }

            // ���̵� �ƿ�
            float a = Mathf.Clamp01(1f - (age / life));
            var old = GUI.color;
            GUI.color = new Color(0.1f, 1f, 0.3f, a); // �����, ���� ����

            // ���� ǥ���ϰ� ������ <b> �±� (richText=true �� ��)
            string text = $"<b>{list[i].text}</b>";
            GUI.Label(new Rect(x, y + row * lineHeight, width, lineHeight), text, style);

            GUI.color = old;
            row++;
        }
    }
}
