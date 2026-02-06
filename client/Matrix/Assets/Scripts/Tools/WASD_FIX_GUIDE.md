# WASD按键问题修复指南

## 问题现象
WASD按键无法控制相机移动

## 解决方案

### 方案一：使用增强版相机控制器（推荐）
增强版CameraController.cs已包含双重输入检测机制：
1. 首先尝试使用Unity标准轴输入
2. 如果无效，则使用直接按键检测(KeyCode)
3. 添加了详细的调试输出帮助诊断

### 方案二：手动检查输入管理器配置

1. 打开Unity编辑器
2. 菜单栏选择：Edit → Project Settings → Input Manager
3. 检查前两个轴配置：

**第一个轴 (Horizontal):**
- Name: Horizontal
- Negative Button: left
- Positive Button: right
- Alt Negative Button: a
- Alt Positive Button: d
- Type: Key or Button (0)

**第二个轴 (Vertical):**
- Name: Vertical
- Negative Button: down
- Positive Button: up
- Alt Negative Button: s
- Alt Positive Button: w
- Type: Key or Button (0)

### 方案三：使用输入测试工具

1. 将`InputTester.cs`脚本添加到场景中的任意GameObject
2. 运行游戏
3. 观察：
   - 屏幕左上角显示实时输入状态
   - 按键时控制台输出详细信息
   - 确认哪种输入方式有效

### 方案四：临时解决方案

如果以上方法都不行，可以手动修改CameraController.cs中的HandleInput方法：

```csharp
private void HandleInput()
{
    // 强制使用直接按键检测
    float horizontal = 0f;
    float vertical = 0f;
    
    if (Input.GetKey(KeyCode.A)) horizontal = -1f;
    if (Input.GetKey(KeyCode.D)) horizontal = 1f;
    if (Input.GetKey(KeyCode.W)) vertical = 1f;
    if (Input.GetKey(KeyCode.S)) vertical = -1f;
    
    moveInput = new Vector3(horizontal, enableVerticalMovement ? vertical : 0f, 0f);
    
    // 其他输入处理...
}
```

## 调试步骤

1. **确认焦点**：确保Game视窗获得焦点
2. **测试简单输入**：先测试空格键等简单按键
3. **检查冲突**：确认没有其他脚本占用相同按键
4. **查看控制台**：观察是否有输入相关的调试输出
5. **逐步排除**：使用InputTester逐个测试不同的输入方式

## 常见原因

- Game视窗未获得焦点
- 输入管理器配置错误
- 其他脚本拦截了输入
- 键盘布局问题（非QWERTY键盘）
- Unity编辑器Bug

## 验证方法

运行游戏后，应该能在控制台看到类似输出：
```
移动输入: (1.0, 0.0, 0.0), 水平: 1.00, 垂直: 0.00
```

如果没有输出，说明输入完全没有被检测到。