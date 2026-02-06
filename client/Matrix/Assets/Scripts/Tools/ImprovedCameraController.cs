using UnityEngine;

namespace Cube.Tools
{
    /// <summary>
    /// 改进版相机控制器 - 解决移动和缩放冲突问题
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class ImprovedCameraController : MonoBehaviour
    {
        [Header("移动设置")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float shiftMultiplier = 2f;
        [SerializeField] private bool enableVerticalMovement = true;
        
        [Header("旋转设置")]
        [SerializeField] private float rotationSpeed = 2f;
        [SerializeField] private bool enableRotation = true;
        
        [Header("缩放设置")]
        [SerializeField] private float zoomSpeed = 20f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 50f;
        [SerializeField] private ZoomMode zoomBehavior = ZoomMode.Orbit; // 缩放行为模式
        
        [Header("边界限制")]
        [SerializeField] private bool enableBounds = false;
        [SerializeField] private Vector3 minBounds = new Vector3(-50, -10, -50);
        [SerializeField] private Vector3 maxBounds = new Vector3(50, 50, 50);

        public enum ZoomMode
        {
            Orbit,      // 围绕中心点缩放（默认）
            Dolly,      // 直接推拉相机（不改变目标点）
            Combined    // 结合两种模式
        }

        private Camera mainCamera;
        private Vector3 targetPosition;
        private Vector3 orbitCenter = Vector3.zero; // 缩放围绕的中心点
        private float targetZoom;
        private float currentZoom;
        
        // 输入相关
        private Vector3 moveInput;
        private Vector2 rotateInput;
        private float zoomInput;
        private bool isRotating = false;

        private void Awake()
        {
            mainCamera = GetComponent<Camera>();
            targetPosition = transform.position;
            currentZoom = Vector3.Distance(transform.position, orbitCenter);
            targetZoom = currentZoom;
            
            if (GetComponent<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
        }

        private void Update()
        {
            HandleInput();
            ProcessMovement();
            ProcessRotation();
            ProcessZoom();
            ApplyBounds();
        }

        private void HandleInput()
        {
            // 移动输入
            moveInput = new Vector3(
                Input.GetAxisRaw("Horizontal"),
                enableVerticalMovement ? Input.GetAxisRaw("Vertical") : 0f,
                0f
            );

            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                moveInput *= shiftMultiplier;
            }

            // 旋转输入
            if (enableRotation)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    isRotating = true;
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                
                if (Input.GetMouseButtonUp(1))
                {
                    isRotating = false;
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }

                if (isRotating)
                {
                    rotateInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * rotationSpeed;
                }
                else
                {
                    rotateInput = Vector2.zero;
                }
            }

            // 缩放输入
            zoomInput = Input.GetAxis("Mouse ScrollWheel");
        }

        private void ProcessMovement()
        {
            if (moveInput != Vector3.zero)
            {
                Vector3 forward = transform.forward;
                Vector3 right = transform.right;
                
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
                Vector3 moveVelocity = moveDirection * moveSpeed * Time.deltaTime;
                
                targetPosition += moveVelocity;
                
                // 更新轨道中心点，使缩放围绕新的位置
                orbitCenter = targetPosition;
                
                Debug.Log($"移动 - 方向:{moveDirection:F2}, 速度:{moveVelocity:F3}, 新位置:{targetPosition:F2}");
            }
        }

        private void ProcessRotation()
        {
            if (rotateInput != Vector2.zero)
            {
                // 围绕轨道中心旋转
                Vector3 offset = targetPosition - orbitCenter;
                transform.RotateAround(orbitCenter, Vector3.up, rotateInput.x);
                transform.RotateAround(orbitCenter, transform.right, -rotateInput.y);
                
                // 更新目标位置
                targetPosition = orbitCenter + (transform.position - orbitCenter);
                
                Debug.Log($"旋转 - 偏移:{offset:F2}, 新位置:{targetPosition:F2}");
            }
        }

        private void ProcessZoom()
        {
            if (Mathf.Abs(zoomInput) > 0.01f)
            {
                targetZoom -= zoomInput * zoomSpeed;
                targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
                
                Debug.Log($"缩放 - 输入:{zoomInput:F3}, 目标:{targetZoom:F2}");
            }

            currentZoom = Mathf.Lerp(currentZoom, targetZoom, Time.deltaTime * 5f);
            
            // 根据缩放模式处理相机位置
            switch (zoomBehavior)
            {
                case ZoomMode.Orbit:
                    // 围绕轨道中心缩放
                    if (currentZoom > 0.1f)
                    {
                        Vector3 direction = (orbitCenter - transform.position).normalized;
                        targetPosition = orbitCenter - direction * currentZoom;
                    }
                    break;
                    
                case ZoomMode.Dolly:
                    // 直接推拉相机（保持相对位置）
                    if (currentZoom > 0.1f && Mathf.Abs(zoomInput) > 0.01f)
                    {
                        Vector3 forward = transform.forward;
                        float deltaZoom = zoomInput * zoomSpeed;
                        targetPosition += forward * deltaZoom;
                    }
                    break;
                    
                case ZoomMode.Combined:
                    // 结合模式：移动时推拉，静止时围绕中心
                    if (moveInput != Vector3.zero)
                    {
                        // 有移动输入时使用推拉模式
                        if (currentZoom > 0.1f && Mathf.Abs(zoomInput) > 0.01f)
                        {
                            Vector3 forward = transform.forward;
                            float deltaZoom = zoomInput * zoomSpeed;
                            targetPosition += forward * deltaZoom;
                        }
                    }
                    else
                    {
                        // 无移动输入时使用轨道模式
                        if (currentZoom > 0.1f)
                        {
                            Vector3 direction = (orbitCenter - transform.position).normalized;
                            targetPosition = orbitCenter - direction * currentZoom;
                        }
                    }
                    break;
            }
        }

        private void ApplyBounds()
        {
            if (enableBounds)
            {
                targetPosition = new Vector3(
                    Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x),
                    Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y),
                    Mathf.Clamp(targetPosition.z, minBounds.z, maxBounds.z)
                );
            }
            
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
        }

        // 公共接口
        public void SetPosition(Vector3 position)
        {
            targetPosition = position;
            orbitCenter = position; // 同时更新轨道中心
            transform.position = position;
        }

        public void SetZoom(float zoom)
        {
            targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
            currentZoom = targetZoom;
        }

        public void SetOrbitCenter(Vector3 center)
        {
            orbitCenter = center;
        }

        public void ResetCamera()
        {
            targetPosition = new Vector3(0, 10, -20);
            orbitCenter = Vector3.zero;
            targetZoom = 20f;
            currentZoom = targetZoom;
            transform.position = targetPosition;
            transform.rotation = Quaternion.Euler(30, 0, 0);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 绘制轨道中心
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(orbitCenter, 0.5f);
            
            // 绘制边界
            if (enableBounds)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(
                    (minBounds + maxBounds) * 0.5f,
                    maxBounds - minBounds
                );
            }
            
            // 绘制相机朝向
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 5f);
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, transform.position + transform.right * 2f);
        }
#endif
    }
}