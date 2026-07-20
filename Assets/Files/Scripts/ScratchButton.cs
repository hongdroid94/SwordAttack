using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 손가락을 뗐다 다시 누르지 않아도, 누른 채로 다른 버튼 위로 미끄러지면
/// 앞 버튼은 떼진 것으로, 뒤 버튼은 눌린 것으로 처리되는 가상 버튼.
///
/// Unity 기본 EventSystem은 PointerDown을 받은 오브젝트가 포인터를 독점한다.
/// (PointerUp도 원래 눌렸던 오브젝트에게만 간다.)
/// 그래서 드래그 중에 들어온 PointerEnter를 "누름"으로,
/// PointerExit를 "뗌"으로 직접 해석하고,
/// 손가락이 실제로 떨어졌는지는 Update에서 직접 확인한다.
/// </summary>
[DisallowMultipleComponent]
public class ScratchButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
	const int NoPointer = int.MinValue;

	public event Action Pressed;
	public event Action Released;

	Selectable selectable;
	int pointerId = NoPointer;
	bool isPressed;


	void Awake()
	{
		selectable = GetComponent<Selectable>();
	}

	void OnDisable()
	{
		// 패널이 닫히는 등으로 비활성화되면 눌린 상태가 남지 않도록 정리한다.
		if (isPressed) Release();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		Press(eventData);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (isPressed && eventData.pointerId == pointerId) Release();
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		// 이미 누르고 있는 손가락이 미끄러져 들어온 경우에만 누름으로 친다.
		// 그냥 마우스를 올려놓은 것(호버)은 무시된다.
		if (IsPointerDown(eventData.pointerId)) Press(eventData);
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		if (isPressed && eventData.pointerId == pointerId) Release();
	}

	void Update()
	{
		// PointerUp은 처음 눌린 오브젝트에게만 전달된다.
		// 긁어서 넘어온 버튼은 그 이벤트를 못 받으므로 직접 확인해야 한다.
		if (isPressed && !IsPointerDown(pointerId)) Release();
	}

	void Press(PointerEventData eventData)
	{
		if (isPressed) return;

		isPressed = true;
		pointerId = eventData.pointerId;

		// 눌림 연출. Selectable은 자기가 받은 PointerDown에만 반응하므로
		// 긁어서 들어온 경우에는 직접 상태를 만들어 줘야 한다.
		if (selectable != null) selectable.OnPointerDown(eventData);

		Pressed?.Invoke();
	}

	void Release()
	{
		isPressed = false;
		pointerId = NoPointer;

		if (selectable != null)
		{
			// OnPointerUp은 onClick을 발생시키지 않는다. (그건 OnPointerClick의 몫)
			selectable.OnPointerUp(new PointerEventData(EventSystem.current));
		}

		Released?.Invoke();
	}

	/// <summary>해당 포인터가 지금도 화면에 닿아 있는지.</summary>
	static bool IsPointerDown(int id)
	{
		if (id == NoPointer) return false;

		// 마우스 포인터 id는 음수(-1 좌클릭). 에디터 플레이모드용.
		if (id < 0) return Input.GetMouseButton(0);

		// 터치 포인터 id는 fingerId와 같다.
		for (int i = 0; i < Input.touchCount; i++)
		{
			Touch touch = Input.GetTouch(i);
			if (touch.fingerId != id) continue;
			return touch.phase != TouchPhase.Ended && touch.phase != TouchPhase.Canceled;
		}
		return false;
	}
}
