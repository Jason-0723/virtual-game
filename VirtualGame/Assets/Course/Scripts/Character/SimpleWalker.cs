using UnityEngine;
using UnityEngine.InputSystem;

// 第 9 週：按住 W 往前走、放開停下、A/D 原地轉向
// 動畫是 In Place，移動由程式負責（Animator 的 Apply Root Motion 要取消）
[RequireComponent(typeof(Animator))]
public class SimpleWalker : MonoBehaviour
{
    public float moveSpeed = 1.5f;     // 腳在地上滑（滑步）就調這個
    public float turnSpeed = 120f;     // 每秒轉幾度

    private Animator animator;
    private CharacterController cc;    // 有掛就會撞牆停下（第 10 週）；沒掛就直接平移

    void Start()
    {
        animator = GetComponent<Animator>();
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        // 走路用 isPressed（按住），點選用 wasPressedThisFrame（按下那一下）
        float move = kb.wKey.isPressed ? 1f : 0f;
        float turn = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);

        transform.Rotate(0f, turn * turnSpeed * Time.deltaTime, 0f);
        if (cc != null) cc.SimpleMove(transform.forward * move * moveSpeed);   // SimpleMove 自己乘時間、自帶重力
        else transform.Translate(Vector3.forward * move * moveSpeed * Time.deltaTime);

        // 參數名要跟 Animator 一字不差，打錯不會報錯，只會沒反應
        animator.SetBool("isWalking", move > 0f);
    }
}
