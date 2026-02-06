# CameraController 相机控制器使用说明

## 🚨 常见问题解决

### WASD按键无效问题

如果发现WASD按键无法控制相机移动，请按以下步骤排查：

1. **检查输入焦点**
   - 确保Game视窗已获得焦点（点击Game视窗）
   - 检查是否有其他应用程序占用了输入

2. **使用输入测试工具**
   - 将`InputTester.cs`脚本添加到任意GameObject上
   - 运行游戏，在屏幕左上角会显示实时输入状态
   - 按键时观察控制台输出确认输入是否被检测到

3. **检查Unity输入管理器**
   - 打开 Edit → Project Settings → Input Manager
   - 确认Horizontal轴配置正确：
     - Negative Button: left
     - Positive Button: right
     - Alt Negative Button: a
     - Alt Positive Button: d
   - 确认Vertical轴配置正确：
     - Negative Button: down
     - Positive Button: up
     - Alt Negative Button: s
     - Alt Positive Button: w

4. **脚本已内置备用方案**
   - 新版本的CameraController已添加直接按键检测作为备选
   - 如果轴输入无效，会自动使用KeyCode检测
   - 控制台会显示详细的输入调试信息

## 功能特性

这是一个功能完整的相机控制器脚本，支持以下操作：

### 🎮 控制方式

**移动控制：**
- **W/A/S/D** 或 **方向键**：前后左右移动
- **Shift键**：加速移动（2倍速）
- **垂直移动**：可通过设置启用上下移动

**视角控制：**
- **鼠标右键 + 拖拽**：自由旋转视角
- **鼠标滚轮**：缩放远近

### ⚙️ 参数配置

在Inspector面板中可以调整以下参数：

#### 移动设置
- `Move Speed`：基础移动速度（默认10）
- `Shift Multiplier`：Shift加速倍数（默认2）
- `Enable Vertical Movement`：是否允许垂直移动

#### 旋转设置
- `Rotation Speed`：鼠标旋转灵敏度（默认2）
- `Enable Rotation`：是否启用鼠标旋转功能

#### 缩放设置
- `Zoom Speed`：滚轮缩放速度（默认20）
- `Min Zoom`：最近缩放距离（默认5）
- `Max Zoom`：最远缩放距离（默认50）

#### 边界限制（可选）
- `Enable Bounds`：是否启用移动边界
- `Min Bounds` / `Max Bounds`：移动范围限制

### 🔧 使用方法

1. **挂载脚本**：
   - 将`CameraController.cs`脚本拖拽到场景中的Camera对象上
   - 或在Inspector中点击"Add Component"搜索"Camera Controller"

2. **基本设置**：
   - 确保相机有`AudioListener`组件（脚本会自动添加）
   - 根据需要调整各项参数

3. **运行测试**：
   - 进入Play模式即可使用WASD移动和鼠标控制

### 📋 公共接口

脚本提供以下公共方法供其他脚本调用：

```csharp
// 设置相机位置
cameraController.SetPosition(new Vector3(0, 5, -10));

// 设置缩放级别
cameraController.SetZoom(15f);

// 重置相机到初始状态
cameraController.ResetCamera();
```

### 💡 使用技巧

1. **性能优化**：在复杂场景中可适当降低移动和旋转速度
2. **边界设置**：大型场景建议启用边界限制防止相机移出有效区域
3. **多人游戏**：网络游戏中应只在本地玩家端启用此控制器
4. **UI交互**：使用时会锁定鼠标指针，注意UI元素的交互优先级

### ⚠️ 注意事项

- 脚本会自动为相机添加AudioListener组件
- 右键旋转时鼠标会被锁定，按右键释放可解锁
- 垂直移动默认关闭，如需启用请勾选相应选项
- 缩放是基于朝向世界原点(0,0,0)进行的

### 🎯 适用场景

- 编辑器场景预览
- RTS游戏视角控制
- 3D查看器应用
- 开发调试工具
- 策略游戏相机系统

---
*该脚本遵循Unity最佳实践，代码结构清晰，易于扩展和定制*