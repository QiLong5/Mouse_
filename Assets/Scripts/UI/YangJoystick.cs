using UnityEngine;
using UnityEngine.UI;

public class YangJoystick : MonoBehaviour
{
    public float Horizontal { get { return input.x; } }
    public float Vertical { get { return input.y; } }
    public Vector2 Direction { get { return input; } }
    public Vector2 Delta { get { return delta.normalized; } }
    public RectTransform rectTransform { get { return GetComponent<RectTransform>(); } }

    public bool dragging { get { return rectTransform.localPosition.magnitude > radius * 0.01f; } }

    [SerializeField]
    private Image BG;

    private Vector2 input = Vector2.zero;

    private Vector2 delta = Vector2.zero;

    [SerializeField]
    private float radius;
    private RectTransform rect;

    private void OnEnable()
    {
        rect = GetComponent<RectTransform>();
        if(radius==0)
            radius = BG.rectTransform.sizeDelta.x / 2f;
        rectTransform.anchoredPosition = new Vector2(0, 0);
    }

    void Update()
    {
        if (rect.localPosition.magnitude > radius)
        {
            rect.localPosition = rect.localPosition.normalized * radius;
        }
        input = rect.localPosition.normalized;
    }

    public void SetPos(Vector3 pos)
    {
        this.transform.localPosition = pos;

        if (rect.localPosition.magnitude > radius)
        {
            rect.localPosition = rect.localPosition.normalized * radius;
        }
        input = rect.localPosition.normalized;
    }
}
