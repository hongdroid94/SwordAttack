using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour
{
	public static Subject<Vector2> JoyStick = new();
	public static Subject<int> Jump = new();
	public static Subject<int> Attack = new();
	// 대시는 누르는 동안 지속되므로 누름/뗌을 나눠서 보낸다.
	public static Subject<int> Dash = new();
	public static Subject<int> DashRelease = new();

	[SerializeField] FixedJoystick fixedJoystick;

	[Header("Virtual Buttons")]
	// 비워두면 씬에서 이름으로 찾는다.
	[SerializeField] Button jumpButton;
	[SerializeField] Button attackButton;
	// 점프 버튼 기준 상대 위치. 기본값은 점프 버튼 바로 위.
	[SerializeField] Vector2 dashButtonOffset = new Vector2(0f, 220f);


	public void InputJump()
	{
		Jump.OnNext(0);
	}

	public void InputAttack()
	{
		Attack.OnNext(0);
	}

	public void InputDash()
	{
		Dash.OnNext(0);
	}

	public void InputDashRelease()
	{
		DashRelease.OnNext(0);
	}

	void Awake()
	{
		Button jump = jumpButton != null ? jumpButton : FindSceneButton("JumpBtn");
		Button attack = attackButton != null ? attackButton : FindSceneButton("AttackBtn");
		Button dash = CreateDashButton(jump);

		// 세 버튼 모두 ScratchButton으로 통일해야 서로 긁어서 넘어갈 수 있다.
		Bind(jump, InputJump, null);
		Bind(attack, InputAttack, null);
		Bind(dash, InputDash, InputDashRelease);
	}

	/// <summary>
	/// 기존 EventTrigger 입력을 걷어내고 ScratchButton으로 갈아끼운다.
	/// </summary>
	static void Bind(Button button, Action onPress, Action onRelease)
	{
		if (button == null) return;

		// 이 프로젝트의 가상 버튼은 Button.onClick이 아니라 EventTrigger의
		// PointerDown으로 입력을 보낸다. 남겨두면 입력이 두 번 들어간다.
		EventTrigger trigger = button.GetComponent<EventTrigger>();
		if (trigger != null) trigger.triggers.Clear();
		button.onClick = new Button.ButtonClickedEvent();

		ScratchButton scratch = button.GetComponent<ScratchButton>();
		if (scratch == null) scratch = button.gameObject.AddComponent<ScratchButton>();
		if (onPress != null) scratch.Pressed += onPress;
		if (onRelease != null) scratch.Released += onRelease;
	}

	/// <summary>
	/// 비활성 오브젝트까지 훑어서 씬 안의 버튼을 이름으로 찾는다.
	/// GamePanel은 씬 시작 시 비활성이라 GameObject.Find로는 찾을 수 없다.
	/// scene.IsValid()로 프리팹 에셋을 걸러낸다.
	/// </summary>
	static Button FindSceneButton(string name)
	{
		Button[] all = Resources.FindObjectsOfTypeAll<Button>();
		for (int i = 0; i < all.Length; i++)
		{
			if (all[i].gameObject.name == name && all[i].gameObject.scene.IsValid()) return all[i];
		}
		Debug.LogWarning($"[InputManager] {name}을(를) 찾지 못했습니다.");
		return null;
	}

	/// <summary>
	/// 점프 버튼을 복제해서 대시 버튼을 만든다.
	/// 씬 YAML을 건드리지 않으므로 GUID·fileID 참조가 깨질 위험이 없고,
	/// 스프라이트·크기·트랜지션이 점프 버튼과 자동으로 일치한다.
	/// </summary>
	Button CreateDashButton(Button source)
	{
		if (source == null)
		{
			Debug.LogWarning("[InputManager] JumpBtn이 없어 대시 버튼을 만들지 못했습니다.");
			return null;
		}

		GameObject clone = Instantiate(source.gameObject, source.transform.parent);
		clone.name = "DashBtn";

		RectTransform src = (RectTransform)source.transform;
		RectTransform rt = (RectTransform)clone.transform;
		rt.anchorMin = src.anchorMin;
		rt.anchorMax = src.anchorMax;
		rt.pivot = src.pivot;
		rt.sizeDelta = src.sizeDelta;
		rt.localScale = src.localScale;
		rt.anchoredPosition = src.anchoredPosition + dashButtonOffset;

		// 아이콘만 대시용(겹화살표)으로 교체. 버튼 배경은 그대로 둔다.
		Sprite icon = Resources.Load<Sprite>("UI/Icon_Dash");
		if (icon != null)
		{
			Image[] images = clone.GetComponentsInChildren<Image>(true);
			for (int i = 0; i < images.Length; i++)
			{
				if (images[i].gameObject != clone) images[i].sprite = icon;
			}
		}

		return clone.GetComponent<Button>();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.X)) InputJump();
		else if (Input.GetKeyDown(KeyCode.C)) InputAttack();

		// 대시는 눌림/뗌을 따로 봐야 하므로 else-if 사슬에서 분리한다.
		if (Input.GetKeyDown(KeyCode.Z)) InputDash();
		else if (Input.GetKeyUp(KeyCode.Z)) InputDashRelease();
	}

	void FixedUpdate()
	{
		JoyStick.OnNext(new Vector2(fixedJoystick.Horizontal + Input.GetAxisRaw("Horizontal"), fixedJoystick.Vertical + +Input.GetAxisRaw("Vertical")));
		
	}
}
