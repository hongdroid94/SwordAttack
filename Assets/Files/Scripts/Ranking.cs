using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase;
using Firebase.Database;
using Firebase.Unity;
using System;
using Debug = UnityEngine.Debug;
using System.Diagnostics;
using Random = UnityEngine.Random;

using System.Threading;

public class Ranking : MonoBehaviour
{    
    public GameObject contents;
    public GameObject RankInfo;   // ·©Å· Á¤º¸
    public GameObject RankingPanel;
    private bool isPanelOpen;

    SynchronizationContext context;

    public class RankingData
    {
        public string userName;        
        public string rankTime;
        public RankingData(string _userName, string _rankTime)
        {
            this.userName = _userName;            
            this.rankTime = _rankTime;
        }
        
        public string getUserName()
        {
            return userName;
        }

        public string getRankTime()
        {
            return rankTime;
        }
    }

    private DatabaseReference databaseRef;
    
    void Start()
    {
        isPanelOpen = false;
        context = SynchronizationContext.Current;
        databaseRef = FirebaseDatabase.DefaultInstance.RootReference;
        ReadCurrentRanking();        
    }

    public void OpenRankingPanel()
    {
        if (!isPanelOpen)
        {
            isPanelOpen = true;
            RankingPanel.SetActive(true);
        }
        else
        {
            isPanelOpen = false;
            RankingPanel.SetActive(false);
        }

    }

    public void WriteNewRanking(string userName, string rankTime)
    {
        // write new ranking data
        RankingData rankingData = new RankingData(userName, rankTime);
        string json = JsonUtility.ToJson(rankingData);
        databaseRef = FirebaseDatabase.DefaultInstance.RootReference;
        databaseRef.Push().SetRawJsonValueAsync(json);
        print($"{userName} firebase upload {rankTime}");
    }    

    private void ReadCurrentRanking()
    {
        // load all ranking data
        databaseRef.OrderByChild("rankTime").GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                // handle error
                Debug.LogError("Error !!");
            }
            else if (task.IsCompleted)
            {
                DataSnapshot snapshot = task.Result;
                Debug.Log(snapshot.ChildrenCount);

                int i = 1;
                foreach(DataSnapshot data in snapshot.Children)
                {
                    IDictionary rankData = (IDictionary)data.Value;
                                                 
                    context.Post(x => {
                        Instantiate(this.RankInfo, contents.transform).GetComponent<RankInfo>().setText(i++, rankData["userName"].ToString(), rankData["rankTime"].ToString());
                    }, null);                    

                    Debug.Log(" [ " + i + " ] " + "userName : " + rankData["userName"] + ", rankTime : " + rankData["rankTime"]);
                    
                }               
            }
        });
    }


}
