# MySQL 数据格式定义规范

## 1. 命名规范

### 1.1 表名规范
- 表名使用小写字母，单词之间用下划线分隔（snake_case）
- 表名应具有描述性，能够清晰表达表的用途
- 表名使用单数形式，如 `user` 而不是 `users`
- 避免使用 MySQL 保留字作为表名
- 示例：`user_info`, `game_config`, `item_data`

### 1.2 字段名规范
- 字段名使用小写字母，单词之间用下划线分隔（snake_case）
- 字段名应具有明确的含义，避免使用缩写（除非是通用缩写）
- 布尔类型字段建议使用 `is_` 或 `has_` 前缀
- 时间字段建议使用 `_time` 或 `_at` 后缀
- 避免使用 MySQL 保留字作为字段名
- 示例：`user_id`, `user_name`, `is_active`, `created_at`, `update_time`

### 1.3 索引命名规范
- 主键索引：默认使用 `PRIMARY KEY`
- 唯一索引：`uk_字段名` 或 `uk_表名_字段名`
- 普通索引：`idx_字段名` 或 `idx_表名_字段名`
- 联合索引：`idx_字段1_字段2` 或 `idx_表名_字段1_字段2`
- 示例：`uk_user_name`, `idx_created_at`, `idx_user_level_exp`

## 2. 字段类型规范

### 2.1 整数类型
- `TINYINT`：范围 -128 ~ 127（有符号）或 0 ~ 255（无符号），适用于状态、类型等小范围值
- `SMALLINT`：范围 -32768 ~ 32767，适用于较小的计数
- `INT`：范围 -2147483648 ~ 2147483647，适用于一般的 ID 和计数
- `BIGINT`：范围更大，适用于大数值或雪花 ID
- 建议：能用小类型就不用大类型，节省存储空间
- 金额类型不要使用浮点数，使用 `DECIMAL` 或存储为整数（分为单位）

### 2.2 字符串类型
- `CHAR(n)`：定长字符串，适用于长度固定的数据（如手机号、身份证号）
- `VARCHAR(n)`：变长字符串，适用于长度不固定的数据
  - 一般字符串字段：`VARCHAR(255)` 或更小
  - 较长文本：`VARCHAR(1000)` 或 `VARCHAR(2000)`
- `TEXT`：存储大文本，但不建议频繁使用，会影响性能
  - `TINYTEXT`：最大 255 字节
  - `TEXT`：最大 65535 字节
  - `MEDIUMTEXT`：最大 16MB
  - `LONGTEXT`：最大 4GB
- 建议：优先使用 `VARCHAR`，明确长度限制

### 2.3 时间类型
- `DATETIME`：范围 1000-01-01 00:00:00 ~ 9999-12-31 23:59:59，占用 8 字节
  - 不受时区影响，存储什么就是什么
  - 适用于需要精确时间的场景
- `TIMESTAMP`：范围 1970-01-01 00:00:01 ~ 2038-01-19 03:14:07，占用 4 字节
  - 受时区影响，会自动转换
  - 适用于记录操作时间
- `DATE`：仅存储日期，占用 3 字节
- `TIME`：仅存储时间，占用 3 字节
- 建议：一般使用 `DATETIME` 或 `BIGINT`（存储时间戳）

### 2.4 其他类型
- `DECIMAL(M, D)`：精确的小数，M 为总位数，D 为小数位数
  - 适用于金额、比率等需要精确计算的场景
  - 示例：`DECIMAL(10, 2)` 表示最多 8 位整数，2 位小数
- `ENUM`：枚举类型，不建议使用，扩展性差
- `JSON`：MySQL 5.7+ 支持，适用于存储结构化但不需要频繁查询的数据

## 3. 表设计规范

### 3.1 必备字段
每个表建议包含以下字段：
- `id`：主键，建议使用 `BIGINT UNSIGNED AUTO_INCREMENT` 或雪花 ID
- `created_at`：创建时间，`DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP`
- `updated_at`：更新时间，`DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP`
- 根据业务需求选择性添加：
  - `created_by`：创建人 ID
  - `updated_by`：更新人 ID
  - `is_deleted`：逻辑删除标识，`TINYINT NOT NULL DEFAULT 0`
  - `version`：乐观锁版本号

### 3.2 字段属性
- **NOT NULL**：尽量为字段设置 NOT NULL 约束，避免 NULL 值带来的问题
- **DEFAULT**：为字段设置合理的默认值
- **COMMENT**：每个字段都必须添加注释，说明字段含义

### 3.3 字符集和排序规则
- 表字符集：`utf8mb4`（支持 emoji 和特殊字符）
- 排序规则：`utf8mb4_general_ci`（不区分大小写）或 `utf8mb4_bin`（区分大小写）
- 建议在建表时统一指定

## 4. 索引规范

### 4.1 索引设计原则
- 主键索引：每个表必须有主键
- 频繁作为查询条件的字段应建立索引
- 频繁用于排序和分组的字段应建立索引
- 区分度高的字段优先建立索引
- 联合索引遵循最左前缀原则
- 避免创建过多索引，影响写入性能（一般不超过 5 个）

### 4.2 索引使用建议
- 字符串字段较长时，考虑使用前缀索引
- 避免在低区分度字段上建立索引（如性别、状态等只有 2-3 个值的字段）
- 联合索引字段顺序：区分度高的字段放前面
- 覆盖索引：查询的字段都在索引中，避免回表

### 4.3 索引失效场景（避免）
- 在索引字段上使用函数或计算
- 使用 `!=` 或 `<>` 操作符
- 使用 `OR` 连接条件（除非所有条件都有索引）
- 字符串不加引号导致隐式转换
- `LIKE` 以 `%` 开头

## 5. 建表示例

```sql
CREATE TABLE `user_info` (
  `id` BIGINT UNSIGNED NOT NULL AUTO_INCREMENT COMMENT '用户ID',
  `user_name` VARCHAR(64) NOT NULL COMMENT '用户名',
  `nick_name` VARCHAR(64) NOT NULL DEFAULT '' COMMENT '昵称',
  `email` VARCHAR(128) NOT NULL DEFAULT '' COMMENT '邮箱',
  `phone` CHAR(11) NOT NULL DEFAULT '' COMMENT '手机号',
  `avatar` VARCHAR(255) NOT NULL DEFAULT '' COMMENT '头像URL',
  `gender` TINYINT NOT NULL DEFAULT 0 COMMENT '性别：0-未知，1-男，2-女',
  `level` INT NOT NULL DEFAULT 1 COMMENT '用户等级',
  `exp` BIGINT NOT NULL DEFAULT 0 COMMENT '经验值',
  `coin` BIGINT NOT NULL DEFAULT 0 COMMENT '金币数量',
  `is_vip` TINYINT NOT NULL DEFAULT 0 COMMENT '是否VIP：0-否，1-是',
  `vip_expire_time` DATETIME NULL COMMENT 'VIP过期时间',
  `status` TINYINT NOT NULL DEFAULT 1 COMMENT '状态：0-禁用，1-正常',
  `is_deleted` TINYINT NOT NULL DEFAULT 0 COMMENT '是否删除：0-否，1-是',
  `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
  `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
  PRIMARY KEY (`id`),
  UNIQUE KEY `uk_user_name` (`user_name`),
  KEY `idx_phone` (`phone`),
  KEY `idx_level_exp` (`level`, `exp`),
  KEY `idx_created_at` (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='用户信息表';
```

## 6. 其他规范

### 6.1 存储引擎
- 默认使用 `InnoDB`，支持事务、行级锁、外键
- 不建议使用 `MyISAM`（不支持事务，已过时）

### 6.2 外键约束
- 不建议使用物理外键，影响性能和扩展性
- 使用逻辑外键，在应用层维护关联关系

### 6.3 分表策略
当单表数据量过大（如超过 1000 万），考虑分表：
- 水平分表：按某个字段（如 user_id）进行哈希或范围分表
- 垂直分表：将大字段或不常用字段拆分到另一张表

### 6.4 敏感信息处理
- 密码：使用不可逆加密算法（如 bcrypt）存储
- 身份证、银行卡：加密存储或脱敏处理
- 日志中避免记录敏感信息

## 7. 性能优化建议

### 7.1 查询优化
- 避免使用 `SELECT *`，只查询需要的字段
- 分页查询使用 `LIMIT` 限制返回数据量
- 避免在 WHERE 子句中使用函数或表达式
- 使用 `EXPLAIN` 分析查询性能

### 7.2 写入优化
- 批量插入使用 `INSERT INTO ... VALUES (...), (...), (...)`
- 大数据量更新分批执行，避免长时间锁表
- 合理设置索引，避免过多索引影响写入性能

### 7.3 表结构优化
- 控制字段数量，一般不超过 50 个
- 大字段（TEXT、BLOB）考虑单独存储
- 冷热数据分离，热数据表保持精简

## 8. 版本控制

### 8.1 数据库变更管理
- 所有表结构变更必须通过 SQL 脚本执行
- SQL 脚本需要版本管理（如使用 Flyway、Liquibase）
- 变更脚本必须包含回滚方案

### 8.2 变更注意事项
- 避免直接删除字段，先标记废弃，确认无影响后再删除
- 添加索引在业务低峰期执行
- 修改字段类型可能导致数据丢失，需谨慎操作并备份数据
