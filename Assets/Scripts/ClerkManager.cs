using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClerkInfo//临时配置(后续改为配置表)
{
    public int id;//解锁id
    public int getTotalCoin;//累计获得金币数后
    public List<int> unlockIds;//需解锁id前提
    public int price;//需投入金币数
    public ClerkInfo(int id,int getCoin,int price, List<int> unlockIds)
    {
        this.id=id;
        this.getTotalCoin=getCoin;
        this.price=price;
        this.unlockIds=unlockIds;
    }
}
/// <summary>
/// 待解锁图标控制
/// </summary>
public class ClerkManager : MonoSingleton<ClerkManager>
{
    private int totalMoney;//玩家累计获得的金币数
    private List<int> displayIds=new List<int>();//已显示图标id
    private List<ClerkInfo> infos=new List<ClerkInfo>();
    public List<PurchaseZone_Clerk> clerks=new List<PurchaseZone_Clerk>();

    void Start()
    {
        //初始化配置（后续改为配置表）
        infos.Add(new ClerkInfo(1,100,150,new List<int>()));
        infos.Add(new ClerkInfo(2,200,200,new List<int>(){1}));
        infos.Add(new ClerkInfo(3,300,250,new List<int>(){2}));
        infos.Add(new ClerkInfo(4,400,250,new List<int>(){3}));
        infos.Add(new ClerkInfo(5,500,250,new List<int>(){4}));
        infos.Add(new ClerkInfo(6,0,1,new List<int>(){5}));

        for (int i = 0; i < clerks.Count; i++)
        {
            clerks[i].id=infos[i].id;
            clerks[i].InitPrice(infos[i].price);
            clerks[i].gameObject.SetActive(false);
        }

        Check();
    }

    public void AddTotalMoney(int num)//玩家累积金币增加
    {
        if(num<=0) return;//仅计算增加的金币

        totalMoney+=num;
        Check();
    }

    public void Check()//显示条件检测   
    {
        if (clerks.Count == 0) return;
        for (int i = 0; i < infos.Count; i++)
        {
            var info=infos[i];
            if (!displayIds.Contains(info.id)&&info.getTotalCoin <= totalMoney)
            {
                bool isFull=true;
                foreach (var item in info.unlockIds)
                {
                    if(clerks.Exists(t=>t.id==item&&!t.hasCompletedPurchase))//存在没满足条件的id
                    {
                        isFull=false;
                        break;
                    }
                }

                if (isFull)
                {
                    displayIds.Add(info.id);
                    var clerk = clerks.Find(t => t.id == info.id);
                    clerk.gameObject.SetActive(true);
                    AudioManager.instance.Play(SK.图标出现);

                    //引导检测
                    //GuildManager.instance.CheckGuild(GuildTriggerType.UnlockIcon,0,clerk.name);
                }
            }
        }
        
    }
}
