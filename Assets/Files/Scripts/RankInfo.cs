using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RankInfo : MonoBehaviour
{
   
    [SerializeField] TMP_Text TxtNumber, TxtUserName, TxtRankTime;

    public void setText(int _number,string _userName, string _rankTime)
    {
        TxtNumber.text = _number.ToString();
        TxtUserName.text = _userName.ToString();
        TxtRankTime.text = _rankTime.ToString();
    }
}
