using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainPanel : MonoBehaviour
{
    [SerializeField] CanvasGroup Tip;

    [Header("Progress")]
    [SerializeField] Transform player;    

    [Header("JoystickTip")]
    [SerializeField] Animator joystickTip;

    public float mTime = 2;
    float mCurrentTime;
    bool isPress = false;
    private void Start()
    {
        mCurrentTime=mTime;
    }
    void Update()
    {
        if (!isPress)
        {
            mCurrentTime += Time.deltaTime;
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            mCurrentTime = 0;
            joystickTip.enabled = false;
            Tip.alpha = 0f;
            isPress = true;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            mCurrentTime = 0;
            joystickTip.enabled = false;
            Tip.alpha = 0f;
            isPress = false;
        }
        if (mCurrentTime >= mTime)
        {
            joystickTip.enabled=true;
            Tip.alpha = 1f;
        }        
    }

}
