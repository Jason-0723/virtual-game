using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

// Roll a Ball 玩家控制（含投影片後半段的「無限生成」版本）
public class PlayerController : MonoBehaviour
{
    public float speed = 5f;                 // 推力大小（投影片建議先給個值，例如 3）
    public TextMeshProUGUI countText;        // 左上角分數文字
    public GameObject winTextObject;         // "You WIN!" 文字物件
    public GameObject spawner;               // PickUpGen 物件（取代耗效能的 GameObject.Find）
    public int startCount = 7;               // 開局一次生成幾個 pickup

    private Rigidbody rb;
    private int count;
    private float movementX;
    private float movementY;

    // 如果物件上沒有掛 Player Input，就用程式自己建立一組 WASD / 方向鍵 / 搖桿 的輸入
    private InputAction moveAction;

    void Awake()
    {
        if (GetComponent<PlayerInput>() == null)
        {
            moveAction = new InputAction("Move");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");
        }
    }

    void OnEnable()  { moveAction?.Enable(); }
    void OnDisable() { moveAction?.Disable(); }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        count = 0;
        SetCountText();
        if (winTextObject != null) winTextObject.SetActive(false);
        if (spawner != null) spawner.GetComponent<RandomGen>().spawnNow(startCount);
    }

    // 掛了 Player Input（Send Messages 模式）時，Unity 會自動呼叫這個函數
    private void OnMove(InputValue movementValue)
    {
        Vector2 movementVector = movementValue.Get<Vector2>();
        movementX = movementVector.x;
        movementY = movementVector.y;
    }

    // 物理相關的動作放在 FixedUpdate（固定時間間隔執行）
    private void FixedUpdate()
    {
        if (moveAction != null)
        {
            Vector2 m = moveAction.ReadValue<Vector2>();
            movementX = m.x;
            movementY = m.y;
        }
        Vector3 movement = new Vector3(movementX, 0.0f, movementY);
        rb.AddForce(movement * speed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("PickUp"))
        {
            count += 1;
            // 先脫離 PickUpGen 再 Destroy，childCount 立刻就會少 1
            // （Destroy 要到這一格畫面的最後才真正執行，見投影片補充說明）
            other.transform.SetParent(null);
            Destroy(other.gameObject);
            if (spawner != null) spawner.GetComponent<RandomGen>().spawnNew();
        }

        SetCountText();
        if (spawner != null && spawner.transform.childCount == 0 && winTextObject != null)
        {
            winTextObject.SetActive(true);
        }
    }

    void SetCountText()
    {
        if (countText != null) countText.text = "Count: " + count.ToString();
    }
}
