using UnityEngine;

namespace Cube.Tools
{
    /// <summary>
    /// 输入测试脚本 - 用于诊断WASD按键问题
    /// </summary>
    public class InputTester : MonoBehaviour
    {
        [Header("测试结果显示")]
        [SerializeField] private bool showDebugInfo = true;
        
        private void Update()
        {
            // 测试各种输入方式
            TestInputMethods();
            
            // 显示调试信息
            if (showDebugInfo)
            {
                DisplayDebugInfo();
            }
        }

        private void TestInputMethods()
        {
            // 测试1: 标准轴输入
            float horizontalAxis = Input.GetAxisRaw("Horizontal");
            float verticalAxis = Input.GetAxisRaw("Vertical");
            
            // 测试2: 直接按键输入
            bool keyA = Input.GetKey(KeyCode.A);
            bool keyD = Input.GetKey(KeyCode.D);
            bool keyW = Input.GetKey(KeyCode.W);
            bool keyS = Input.GetKey(KeyCode.S);
            
            // 测试3: 方向键输入
            bool arrowLeft = Input.GetKey(KeyCode.LeftArrow);
            bool arrowRight = Input.GetKey(KeyCode.RightArrow);
            bool arrowUp = Input.GetKey(KeyCode.UpArrow);
            bool arrowDown = Input.GetKey(KeyCode.DownArrow);
            
            // 如果检测到任何输入，输出到控制台
            if (Mathf.Abs(horizontalAxis) > 0.1f || Mathf.Abs(verticalAxis) > 0.1f ||
                keyA || keyD || keyW || keyS || arrowLeft || arrowRight || arrowUp || arrowDown)
            {
                Debug.Log($"=== 输入检测 ===\n" +
                         $"轴输入 - 水平: {horizontalAxis:F2}, 垂直: {verticalAxis:F2}\n" +
                         $"WASD键 - A:{keyA} D:{keyD} W:{keyW} S:{keyS}\n" +
                         $"方向键 - ←:{arrowLeft} →:{arrowRight} ↑:{arrowUp} ↓:{arrowDown}");
            }
        }

        private void DisplayDebugInfo()
        {
            // 在屏幕上显示当前输入状态
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;
            style.normal.textColor = Color.white;
            
            string info = "输入测试信息:\n" +
                         $"Horizontal轴: {Input.GetAxisRaw("Horizontal"):F2}\n" +
                         $"Vertical轴: {Input.GetAxisRaw("Vertical"):F2}\n" +
                         $"A键: {Input.GetKey(KeyCode.A)}\n" +
                         $"D键: {Input.GetKey(KeyCode.D)}\n" +
                         $"W键: {Input.GetKey(KeyCode.W)}\n" +
                         $"S键: {Input.GetKey(KeyCode.S)}";
            
            // 在Game视图左上角显示
            GUI.Label(new Rect(10, 10, 300, 200), info, style);
        }

        private void OnGUI()
        {
            if (showDebugInfo)
            {
                DisplayDebugInfo();
            }
        }
    }
}