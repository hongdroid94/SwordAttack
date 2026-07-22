using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UniRx;

public class GamePanel : MonoBehaviour
{
    [SerializeField] Transform lifeView;
    [SerializeField] TMP_Text stopwatchText;
	[SerializeField] TMP_Text stopwatchResultText;

	Damaged playerDamaged;
	double elapsedTime;
	bool isRunningStopwatch;
	public string stopWatch;

	[ContextMenu("StartStopWatch")]
	public void StartStopWatch() 
	{
		isRunningStopwatch = true;
	}

	[ContextMenu("StopStopWatch")]
	public void StopStopWatch()
	{
		isRunningStopwatch = false;
	}

	void Start()
	{
		playerDamaged = FindObjectOfType<PlayerController>().GetComponent<Damaged>();
		for (int i = 0; i < playerDamaged.Health - 1; i++)
		{
			Instantiate(lifeView.GetChild(0).gameObject, lifeView).name = "LifeImage";
		}

		// 피격·회복 모두 HealthChanged로 통일한다.
		playerDamaged.HealthChanged += RefreshHearts;
	}

	void OnDestroy()
	{
		playerDamaged.HealthChanged -= RefreshHearts;
	}

	void Update()
	{
		if (isRunningStopwatch)
		{
			elapsedTime += Time.deltaTime;
			int min = (int)elapsedTime % 3600 / 60; 
			int sec = (int)elapsedTime % 3600 % 60;
			int milliSecond = (int)((elapsedTime % 1) * 100);
			stopwatchText.text = stopwatchResultText.text = stopWatch = string.Format("{0:D2}:{1:D2}:{2:D2}", min, sec, milliSecond);
		}

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			PausePanel();
		}
	}

	public void PausePanel()
	{
		print("����");
		UIManager.Inst.ShowPanel("PausePanel");
		Time.timeScale = 0;
	}
	public void PauseResumePanel()
	{
		print("�簳");
		UIManager.Inst.ShowPanel("GamePanel");
		Time.timeScale = 1;
	}

	public void GiveUpPanel() 
	{
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}

	public void QuitPanel() 
	{
		Application.Quit();
	}

	// 하트를 Destroy하지 않고 켜고 끈다. 회복하면 다시 채워야 하기 때문이다.
	void RefreshHearts(int health)
	{
		for (int i = 0; i < lifeView.childCount; i++)
		{
			lifeView.GetChild(i).gameObject.SetActive(i < health);
		}
	}
}
