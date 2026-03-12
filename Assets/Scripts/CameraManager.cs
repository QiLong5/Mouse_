using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public enum CamreaModel
{
    FollowPlayer,
    GoToArrow,
    Stay,
    BackToPlayer
}

/// <summary>
/// 相机管理器
/// </summary>
public class CameraManager : MonoSingleton<CameraManager>
{
    public Camera mCamera;

    [Header("视野范围控制")]
    [SerializeField]
    float mCameraCressNum;
    [SerializeField]
    float mCameraVerNum;
    [SerializeField]
    float mCameraCressNumFar;
    [SerializeField]
    float mCameraVerNumFar;

    float mCurrentCameraNum;
    bool iszoom;


    Vector3 mOffset;


    [Header("相机引导")]
    [SerializeField]
    private float switchTimer = 1f;//切换时长
    [SerializeField]
    private float stayTimer = 1f;//ͣ停留时长
    [SerializeField]
    private List<Transform> forceCameras;//引导的位置

    Vector3 oripos;

    CamreaModel camreaModel = CamreaModel.FollowPlayer;
    bool isForcedLook;//是否强制引导中

    public bool IsCanMove { get { return (!(isForcedLook || iszoom)); } }

    public event Action CameraMoveAction;
    public event Action CameraZoomAction;
    public bool isCress;
    void Start()
    {
        if (Screen.width > Screen.height)
        {
            mCurrentCameraNum = mCameraCressNumFar;
            isCress = true;
        }
        else
        {
            mCurrentCameraNum = mCameraVerNumFar;
            isCress = false;
        }
        mOffset =  transform.position- Player.instance.transform.position ;
        CamerZoom(false, 1);
    }

    // Update is called once per frame
    void Update()
    {
        if (!iszoom)
        {
            if (Screen.width > Screen.height)
            {
                mCurrentCameraNum = mCameraCressNum;
                isCress = true;
            }
            else
            {
                mCurrentCameraNum = mCameraVerNum;
                isCress = false;
            }
        }
        mCamera.orthographicSize = mCurrentCameraNum;

        if (Input.GetKeyDown(KeyCode.L))
        {
            CamerZoom(true, 1,true,1);
        }
    }

    private void LateUpdate()
    {
        if (!LunaManager.instance.isGameOver && !isForcedLook)
        {
            if (transform.position != Player.instance.transform.position + mOffset)
            {
                transform.position = Player.instance.transform.position + mOffset;
               
            }
        }
        CameraMoveAction?.Invoke();
    }

    /// <summary>
    /// 强制引导看index
    /// </summary>
    /// <param name="index"></param>
    public void ForceLookByIndex(int index)
    {
        if (!isForcedLook)
        {
            isForcedLook = true;
            oripos = transform.position;
            StartCoroutine(ForceLookIE(forceCameras[index]));
        }
    }
        
    IEnumerator ForceLookIE(Transform targetPos)
    {
        transform.DOMove(targetPos.position, switchTimer).SetEase(Ease.Linear);
        yield return new WaitForSeconds(switchTimer+stayTimer);
        transform.DOMove(oripos, switchTimer).SetEase(Ease.Linear);
        yield return new WaitForSeconds(switchTimer);
        isForcedLook = false;
    }

    /// <summary>
    /// 视野控制拉远拉近
    /// </summary>
    /// <param name="isfar"></param>
    public void CamerZoom(bool isfar,float zoomtimes=1f,bool isback=false,float staytimes=0f)
    {
        if (!iszoom)
        {
            iszoom = true;
            StartCoroutine(CamerZoomIE(isfar, zoomtimes,isback, staytimes));
        }
    }

    IEnumerator CamerZoomIE(bool isfar,float zoomtimes,bool isback,float staytimes)
    {
        float mzoomtimes = zoomtimes;
        float disV= mCameraVerNumFar - mCameraVerNum;
        float disC= mCameraCressNumFar - mCameraCressNum;
        while (mzoomtimes>0)
        {
            if (Screen.width > Screen.height)
            {
                if (isfar)
                {
                    mCurrentCameraNum = mCameraCressNumFar - (disC * (mzoomtimes / zoomtimes));
                }
                else
                {
                    mCurrentCameraNum = mCameraCressNum + (disC * (mzoomtimes / zoomtimes));
                }
            }
            else
            {
                if (isfar)
                {
                    mCurrentCameraNum = mCameraVerNumFar - (disV * (mzoomtimes / zoomtimes));
                }
                else
                {
                    mCurrentCameraNum = mCameraVerNum + (disV * (mzoomtimes / zoomtimes));
                }
            }
            mzoomtimes -= Time.deltaTime;
            CameraZoomAction?.Invoke();
            yield return null;
        }
        if (Screen.width > Screen.height)
        {
            if (isfar)
            {
                mCurrentCameraNum = mCameraCressNumFar;
            }
            else
            {
                mCurrentCameraNum = mCameraCressNum;
            }
            
        }
        else
        {
            if (isfar)
            {
                mCurrentCameraNum = mCameraVerNumFar;
            }
            else
            {
                mCurrentCameraNum = mCameraVerNum;
            }
        }

        if (isback)
        {
            yield return new WaitForSeconds(staytimes);
            StartCoroutine(CamerZoomIE(!isfar, zoomtimes,false,0));
        }
        else
        {
            iszoom = false;
        }
        CameraZoomAction?.Invoke();
    }
}
