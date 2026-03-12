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

    float radius;

    private void OnEnable()
    {
        rectTransform.anchoredPosition = new Vector2(0, 0);
    }

    void Update()
    {
        radius = BG.rectTransform.sizeDelta.x / 2f;

        if (GetComponent<RectTransform>().localPosition.magnitude > radius)
        {
            GetComponent<RectTransform>().localPosition = GetComponent<RectTransform>().localPosition.normalized * radius;
        }
        input = GetComponent<RectTransform>().localPosition.normalized;
    }

    public void SetPos(Vector3 pos)
    {
        this.transform.localPosition = pos;
        radius = BG.rectTransform.sizeDelta.x / 2f;

        if (GetComponent<RectTransform>().localPosition.magnitude > radius)
        {
            GetComponent<RectTransform>().localPosition = GetComponent<RectTransform>().localPosition.normalized * radius;
        }
        input = GetComponent<RectTransform>().localPosition.normalized;
    }
}
