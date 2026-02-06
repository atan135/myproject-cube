using UnityEngine;

public class SpaceCameraControl : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 10f;
    public float zoomSpeed = 50f;       // 滚轮灵敏度
    public float smoothTime = 0.2f;     // 缓冲时间（越小越灵敏，越大越丝滑）

    [Header("旋转设置")]
    public float lookSpeed = 2f;
    
    private float rotationX = 0f;
    private float rotationY = 0f;
    
    // 平滑辅助变量
    private Vector3 targetPosition;
    private Vector3 currentVelocity = Vector3.zero;

    void Start()
    {
        Vector3 rot = transform.localRotation.eulerAngles;
        rotationX = rot.y;
        rotationY = rot.x;
        targetPosition = transform.position; // 初始化目标位置
    }

    void Update()
    {
        // 1. 旋转逻辑（保持不变，右键控制）
        if (Input.GetMouseButton(1))
        {
            rotationX += Input.GetAxis("Mouse X") * lookSpeed;
            rotationY -= Input.GetAxis("Mouse Y") * lookSpeed;
            rotationY = Mathf.Clamp(rotationY, -90f, 90f);
            transform.localRotation = Quaternion.Euler(rotationY, rotationX, 0);
        }

        // 2. 移动逻辑（WASD）
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 moveInput = (transform.right * h + transform.up * v).normalized;
        targetPosition += moveInput * moveSpeed * Time.deltaTime;

        // 3. 丝滑滚动核心：累加目标位置，而非直接位移
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            targetPosition += transform.forward * scroll * zoomSpeed;
        }

        // 4. 使用 SmoothDamp 消除顿挫感
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref currentVelocity, smoothTime);
    }
}
