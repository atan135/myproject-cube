# 数据库初始化说明

## 解决中文乱码问题

### 执行步骤：

1. **清理现有数据库**（如果已有数据）：
```bash
mysql -u root -p < server/sql/cleanup_database.sql
```

2. **执行修正后的初始化脚本**：
```bash
mysql -u root -p < server/sql/init_database.sql
```

### 脚本说明：

- **init_database.sql** - 数据库初始化脚本（包含UTF8MB4字符集设置）
- **cleanup_database.sql** - 完全清理脚本（删除数据库和用户）
- **truncate_tables.sql** - 清空数据脚本（保留表结构，删除所有数据）

### 关键改进：

1. **明确设置字符集**：
   - `SET NAMES utf8mb4` - 设置客户端字符集
   - `CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci` - 表级字符集
   - 列级字符集明确指定

2. **包含测试数据**：
   - 自动插入中文测试数据
   - 验证字符集设置是否正确

3. **验证机制**：
   - 显示字符集配置信息
   - 验证中文数据存储和检索

### 预期结果：

执行后应该能看到：
- 数据库和表正确创建
- 中文数据正常存储和显示
- 字符集显示为 utf8mb4