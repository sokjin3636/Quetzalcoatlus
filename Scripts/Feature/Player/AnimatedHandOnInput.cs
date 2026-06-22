using UnityEngine;
using UnityEngine.InputSystem;

public class AnimatedHandOnInput : MonoBehaviour
{
    [Header("--- 입력 설정 ---")]
    public InputActionProperty gripValue;

    [Header("--- 애니메이터 ---")]
    public Animator handAnimator;

    void Start()
    {
    }

    void Update()
    {
        // 컨트롤러의 그립(Grip) 입력값을 읽어와 애니메이터 파라미터에 전달
        float grip = gripValue.action.ReadValue<float>();
        handAnimator.SetFloat("Grip", grip);
    }
}