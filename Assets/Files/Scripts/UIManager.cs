using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;

public class UIManager : MonoBehaviour
{
	public static UIManager Inst { get; private set; }
	void Awake()
	{
		Inst = this;
	}

	[SerializeField] GameObject[] panels;
	[SerializeField] GamePanel gamePanel;
	[SerializeField] Ranking ranking;
	[SerializeField] GameObject player;
	[SerializeField] TMP_InputField nickInput;
	[SerializeField] Toggle soundToggle;
	[SerializeField] Button startBtn;
	[SerializeField] TMP_Text endTitleText;
	[SerializeField] AdmobManager admobManager;

	string _nickname;
	public string Nickname 
	{
		get 
		{
			_nickname = PlayerPrefs.GetString("Nickname", "");
			return _nickname;
		}
		set 
		{
			PlayerPrefs.SetString("Nickname", value);
			_nickname = value;
		}
	}

	bool _onSound;
	public bool OnSound 
	{
		get
		{
			_onSound = PlayerPrefs.GetInt("OnSound", 1) == 1;
			return _onSound;
		}
		set
		{
			PlayerPrefs.SetInt("OnSound", value ? 1 : 0);
			_onSound = value;
		}
	}


	public void ShowPanel(string panelName) 
	{
		for (int i = 0; i < panels.Length; i++)
		{
			bool isEqual = panels[i].name.ToLower() == panelName.ToLower();
			panels[i].SetActive(isEqual);
		}
	}

	public void ShowEndPanel(bool isDie) 
	{
		ShowPanel("EndPanel");
		if (isDie)
		{
			endTitleText.text = "<color=red>YOU DIED</color>";
		}
		else 
		{
			endTitleText.text = "FINISH !";
			ranking.WriteNewRanking(Nickname, gamePanel.stopWatch);

		}

		// ¾Öµå¸÷ ±¤°í 40%
		if (Random.Range(0, 101) > 60) 
		{
			admobManager.ShowRewardAd();
		}
	}

	public void ShowRankPanel() 
	{
		ShowPanel("RankPanel");
	}

	public void ToggleSound(bool isOn) 
	{
		OnSound = isOn;
		foreach (var audioSource in FindObjectsOfType<AudioSource>())
		{
			audioSource.mute = !isOn;
		}
	}

	public void StartClick() 
	{
		ShowPanel("GamePanel");
		gamePanel.StartStopWatch();
		player.SetActive(true);
		SoundManager.Instance.PlayBGMSound();
	}

	public void QuitClick()
	{
		Application.Quit();
	}

	public void NickInputEndEdit() 
	{
		Nickname = nickInput.text;
	}

	public void MenuCancelSound() 
	{
		SoundManager.Instance.PlaySFXSound("Menu_Cancel");
	}

	public void MenuConfirmSound()
	{
		SoundManager.Instance.PlaySFXSound("Menu_Confirm");
	}

	void Start()
	{
		Application.targetFrameRate = 60;
		nickInput.text = Nickname;
		soundToggle.isOn = OnSound;
		Time.timeScale = 1;
		ShowPanel("MainPanel");
	}

	void Update()
	{
		startBtn.interactable = !string.IsNullOrWhiteSpace(nickInput.text);
	}
}
