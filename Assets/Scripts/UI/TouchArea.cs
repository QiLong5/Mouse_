using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TouchArea : MonoBehaviour,IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public Vector2 mOutPos;
    public RectTransform mJoystickBG;
    public YangJoystick mJoystickHandle;
    public CanvasGroup mCanvasGp;
    public bool IsTouching;

    public float mTime=0;

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
        }
        else
        {
            mJoystickBG.gameObject.SetActive(false);
            mCanvasGp.alpha = 0;
        }
    }
}
