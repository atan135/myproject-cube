-- 清空数据库表数据脚本（保留表结构）
-- 文件路径: server/sql/truncate_tables.sql

-- 警告：此脚本会删除所有表数据！
-- 仅在开发环境中使用

USE cube_game;

-- 禁用外键约束检查
SET FOREIGN_KEY_CHECKS = 0;

-- 清空所有表数据
TRUNCATE TABLE user_role_relations;
TRUNCATE TABLE login_records;
TRUNCATE TABLE user_roles;
TRUNCATE TABLE game_users;

-- 重新启用外键约束检查
SET FOREIGN_KEY_CHECKS = 1;

-- 重新插入默认角色数据
INSERT IGNORE INTO user_roles (role_name, role_description, permissions) VALUES 
('player', '普通玩家', '{"can_play": true, "can_chat": true}'),
('vip', 'VIP玩家', '{"can_play": true, "can_chat": true, "special_rewards": true}'),
('moderator', '管理员', '{"can_play": true, "can_chat": true, "can_moderate": true}'),
('admin', '超级管理员', '{"all_permissions": true}');

-- 验证清空结果
SELECT 'Tables truncated successfully!' AS result;
SELECT 'game_users count:' AS table_name, COUNT(*) AS record_count FROM game_users;
SELECT 'login_records count:' AS table_name, COUNT(*) AS record_count FROM login_records;
SELECT 'user_roles count:' AS table_name, COUNT(*) AS record_count FROM user_roles;
SELECT 'user_role_relations count:' AS table_name, COUNT(*) AS record_count FROM user_role_relations;