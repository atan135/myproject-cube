-- 清理脚本 - 删除现有数据库和用户
-- 文件路径: server/sql/cleanup_database.sql

-- 警告：此脚本会删除所有数据！
-- 仅在开发环境中使用

-- 删除外键约束检查（临时禁用）
SET FOREIGN_KEY_CHECKS = 0;

-- 删除数据库
DROP DATABASE IF EXISTS cube_game;

-- 删除用户
DROP USER IF EXISTS 'cube_user'@'localhost';
DROP USER IF EXISTS 'cube_user'@'%';

-- 重新启用外键约束检查
SET FOREIGN_KEY_CHECKS = 1;

-- 刷新权限
FLUSH PRIVILEGES;

-- 验证清理结果
SELECT 'Database and user cleaned successfully!' AS result;
SHOW DATABASES LIKE 'cube_game';
SELECT User, Host FROM mysql.user WHERE User = 'cube_user';