using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class ObjStartButton : MonoBehaviour
{
    public Processor processingMachine;
    public Transform box;

    public bool isenter;

    public bool isAlwayEnter;
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
         
            if (!isenter&&!isAlwayEnter)
            {
                processingMachine.ispprocess = true;
                isenter =true;
                box.DOKill();
                box.DOLocalMoveY(-0.9f,0.3f);
            }
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
            if (!isAlwayEnter)
            {
                processingMachine.ispprocess = true;
            }
          
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        if (other.tag.Equals("Player"))
        {
           
            if (isenter && !isAlwayEnter)
            {
                processingMachine.ispprocess = false;
                isenter =false;
                box.DOKill();
                box.DOLocalMoveY(-0.5f, 0.3f);
            }
        }

    }

    public void SetAlwayEnter()
    {
        isAlwayEnter =true;
        processingMachine.ispprocess = true;
        box.DOKill();
        box.DOLocalMoveY(-0.9f, 0.3f);
    }
}
