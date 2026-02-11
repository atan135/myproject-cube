using UnityEngine;

public class SmoothCameraLoader : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 20f;        // 基础移动速度
    public float shiftMultiplier = 2.5f; // 按住Shift时的加速倍数
    public float smoothness = 10f;      // 平滑度

    [Header("旋转设置")]
    public float rotateSpeed = 2f;      // 旋转灵敏度
    public float minVerticalAngle = -80f;
    public float maxVerticalAngle = 80f;

    [Header("缩放设置")]
    public float zoomSpeed = 5f;

    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float rotationX;
    private float rotationY;

    void Start()
    {
        targetPosition = transform.position;
        targetRotation = transform.rotation;
        
        // 初始化旋转角度，避免突变
        Vector3 angles = transform.eulerAngles;
        rotationX = angles.y;
        rotationY = angles.x;
    }

    void LateUpdate()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();

        // 执行平滑插值
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothness);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothness);
    }

    void HandleMovement()
    {
        float speed = moveSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.LeftShift)) speed *= shiftMultiplier;

        float h = Input.GetAxisRaw("Horizontal"); // A, D
        float v = Input.GetAxisRaw("Vertical");   // W, S

        Vector3 moveDir = transform.right * h + transform.forward * v;
        targetPosition += moveDir * speed;
    }

    void HandleRotation()
    {
        // 只有点击鼠标右键时才旋转
        if (Input.GetMouseButton(1))
        {
            rotationX += Input.GetAxis("Mouse X") * rotateSpeed;
            rotationY -= Input.GetAxis("Mouse Y") * rotateSpeed;
            rotationY = Mathf.Clamp(rotationY, minVerticalAngle, maxVerticalAngle);

            targetRotation = Quaternion.Euler(rotationY, rotationX, 0);
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // 向前/向后推相机实现缩放效果
            targetPosition += transform.forward * scroll * zoomSpeed * 10f;
        }
    }
}
