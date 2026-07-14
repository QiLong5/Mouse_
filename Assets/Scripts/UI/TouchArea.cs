using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TouchArea : MonoSingleton<TouchArea>,IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public Vector2 mOutPos;
    [SerializeField]private RectTransform mJoystickBG1;
    [SerializeField]private RectTransform mJoystickBG2;
    [HideInInspector]public RectTransform mJoystickBG;
    [SerializeField]private YangJoystick mJoystickHandle1;
    [SerializeField]private YangJoystick mJoystickHandle2;
    [HideInInspector]public YangJoystick mJoystickHandle;
    public CanvasGroup mCanvasGp;
    public bool IsTouching;

    public float mTime = 0;

    [Header("方向指示边")]
    [SerializeField] private RectTransform mEdge = null;
    [Tooltip("贴图默认朝向偏移：贴图朝上填 -90，朝右填 0，朝下填 90")]
    [SerializeField] private float mEdgeAngleOffset = -90f;
    [Tooltip("平滑跟随速度，越大越快贴近目标方向")]
    [SerializeField] private float mEdgeRotateLerp = 15f;

    private void Start()
    {
        mJoystickBG = LunaManager.IsGoogle() ? mJoystickBG2 : mJoystickBG1;
        mJoystickHandle = LunaManager.IsGoogle() ? mJoystickHandle2 : mJoystickHandle1;
        ShowEdge(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        mCanvasGp.alpha = 1f;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform, eventData.pressPosition, eventData.enterEventCamera, out mOutPos
            );

        mJoystickBG.localPosition = mOutPos;
        IsTouching = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        mCanvasGp.alpha = 0f;
        //JoystickHandle.localPosition = Vector3.zero;
        IsTouching = false;
    }

    private void Update()
    {
        if(LunaManager.instance.isGameOver)
        {
            mJoystickBG.gameObject.SetActive(false);
            mCanvasGp.alpha = 0;
            ShowEdge(false);
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            this.transform.position = Input.mousePosition;
            mCanvasGp.alpha = 1;
        }
        if (Input.GetMouseButton(0))
        {
            mJoystickBG.gameObject.SetActive(true);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mJoystickBG, Input.mousePosition, null, out mOutPos
            );

            //JoystickHandle.localPosition = outPos;
            mJoystickHandle.SetPos(mOutPos);
            // 触摸摇杆：显示方向指示 Edge，并让其朝向摇杆方向
            ShowEdge(true);
            UpdateEdgeRotation();
        }
        else
        {
            mJoystickBG.gameObject.SetActive(false);
            mCanvasGp.alpha = 0;
            // 松开摇杆：隐藏 Edge
            ShowEdge(false);
        }
    }

    /// <summary>控制方向指示 Edge 的显隐。</summary>
    private void ShowEdge(bool show)
    {
        if (mEdge == null) return;
        if (mEdge.gameObject.activeSelf != show)
            mEdge.gameObject.SetActive(show);
    }

    /// <summary>将 Edge 的 z 轴旋转平滑指向当前摇杆方向。</summary>
    private void UpdateEdgeRotation()
    {
        if (mEdge == null) return;
        Vector2 dir = mJoystickHandle.Direction;
        if (dir.sqrMagnitude < 0.0001f) return;   // 回中/空闲：保持当前角度，避免 Atan2(0,0) 抖动
        float target = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + mEdgeAngleOffset;
        float z = Mathf.LerpAngle(mEdge.localEulerAngles.z, target, mEdgeRotateLerp * Time.deltaTime);
        mEdge.localEulerAngles = new Vector3(0f, 0f, z);
    }
}
