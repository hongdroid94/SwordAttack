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

		playerDamaged.Defect += PlayerDefect;
	}

	void OnDestroy()
	{
		playerDamaged.Defect -= PlayerDefect;

	}

	void Update()
	{
		if (isRunningStopwatch)
		{
			elapsedTime += Time.deltaTime;
			int min = (int)elapsedTime % 3600 / 60; 
			int sec = (int)elapsedTime % 3600 % 60;
			int milliSecond = (int)((elapsedTime % 1) * 100);
			stopwatchText.text = stopwatchResultText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", min, sec, milliSecond);
		}

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			PausePanel();
		}
	}

	public void PausePanel()
	{
		print("¸ØÃã");
		UIManager.Inst.ShowPanel("PausePanel");
		Time.timeScale = 0;
	}
	public void PauseResumePanel()
	{
		print("Àç°³");
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

	void PlayerDefect(bool isDie, int health, int damageDir) 
	{
		for (int i = 0; i < lifeView.childCount; i++)
		{
			if (i >= health)
			{
				Destroy(lifeView.GetChild(i).gameObject);
			}
		}
	}
}
