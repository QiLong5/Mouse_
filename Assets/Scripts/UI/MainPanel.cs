using System.Collections;
using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;
using UnityEngine.UI;

public class MainPanel : MonoBehaviour
{
    private CanvasGroup Tip;
    [SerializeField]private CanvasGroup Tip1;
    [SerializeField]private CanvasGroup Tip2;

    [Header("Progress")]
    [SerializeField] Transform player;    

    Animator joystickTip;
    private Tweener sequence;
    public float mTime = 5;
    float mCurrentTime;
    bool isPress = false;
    bool isFirstGuild=true;
    private void Start()
    {
        mCurrentTime = mTime;
        Tip = LunaManager.IsGoogle() ? Tip2 : Tip1;
        joystickTip = Tip.GetComponent<Animator>();
        StartCenterSpin();
    }
    private void StartCenterSpin()
    {
        if (Tip == null) return;
        if (!LunaManager.IsGoogle()) return;

        sequence?.Kill();
        Tip.transform.localRotation = Quaternion.identity;
        sequence = Tip.transform.GetChild(0)
            .DOLocalRotate(new Vector3(0f, 0f, -360f), 1f, RotateMode.FastBeyond360)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Restart);
        transform.DOLocalMove(Vector3.zero,0);
    }
    void Update()
    {
        if (LunaManager.instance.isGameOver||Player.instance.isDie)
        {
            Tip.alpha = 0;
            return;
        }
        if (!isPress)
        {
            mCurrentTime += Time.deltaTime;
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            mCurrentTime = 0;
            Tip.alpha = 0f;
            isPress = true;
            if (joystickTip != null)
                joystickTip.enabled = false;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            mCurrentTime = 0;
            Tip.alpha = 0f;
            isPress = false;
            if(joystickTip!=null)
                joystickTip.enabled = false;
        }
        if (mCurrentTime >= mTime)
        {
            Tip.alpha = 1f;
            if (isFirstGuild)
                isFirstGuild = false;
            if (joystickTip != null)
                joystickTip.enabled = true;
        }        
    }

}
