using UnityEngine;
[DefaultExecutionOrder(100)]
public class MapCameraFollow : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;           // 拖入你的主相机 (Main Camera)

    [Header("高度设置")]
    public float height = 100f;        // 俯视高度
    
    [Header("平滑设置")]
    public bool useSmoothing = true;   // 是否开启平滑跟随
    public float smoothSpeed = 10f;    // 跟随速度

    void LateUpdate()
    {
        if (target == null) return;

        // 计算目标位置：保持主相机的 X,Z 坐标，高度固定
        Vector3 targetPos = new Vector3(target.position.x, height, target.position.z);

        if (useSmoothing)
        {
            // 使用 Lerp 平滑移动，消除抖动
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);
        }
        else
        {
            transform.position = targetPos;
        }

        // 确保始终垂直向下看，且旋转不受主相机旋转影响
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }
}
