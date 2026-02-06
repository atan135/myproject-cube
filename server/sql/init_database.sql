-- 改进版数据库初始化脚本 - 解决中文乱码问题
-- 文件路径: server/sql/init_database_fixed.sql

-- 设置客户端字符集
SET NAMES utf8mb4;
SET CHARACTER SET utf8mb4;

-- 创建数据库用户
CREATE USER IF NOT EXISTS 'cube_user'@'localhost' IDENTIFIED BY 'cube_password';
CREATE USER IF NOT EXISTS 'cube_user'@'%' IDENTIFIED BY 'cube_password';

-- 创建数据库并指定字符集
CREATE DATABASE IF NOT EXISTS cube_game 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

-- 授予用户权限
GRANT ALL PRIVILEGES ON cube_game.* TO 'cube_user'@'localhost';
GRANT ALL PRIVILEGES ON cube_game.* TO 'cube_user'@'%';

-- 刷新权限
FLUSH PRIVILEGES;

-- 使用数据库
USE cube_game;

-- 设置当前连接字符集
SET NAMES utf8mb4;

-- 创建游戏用户表（明确指定字符集）
CREATE TABLE IF NOT EXISTS game_users (
    id BIGINT AUTO_INCREMENT PRIMARY KEY COMMENT '用户唯一标识',
    username VARCHAR(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL UNIQUE COMMENT '用户名',
    email VARCHAR(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL UNIQUE COMMENT '邮箱地址',
    password_hash VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL COMMENT '密码哈希值',
    nickname VARCHAR(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT '昵称',
    avatar_url VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT '头像URL',
    level INT DEFAULT 1 COMMENT '用户等级',
    experience BIGINT DEFAULT 0 COMMENT '经验值',
    coins BIGINT DEFAULT 0 COMMENT '游戏币',
    diamonds BIGINT DEFAULT 0 COMMENT '钻石',
    status TINYINT DEFAULT 1 COMMENT '账户状态: 1-正常, 2-封禁, 3-注销',
    last_login_time DATETIME COMMENT '最后登录时间',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间',
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP COMMENT '更新时间',
    
    INDEX idx_username (username),
    INDEX idx_email (email),
    INDEX idx_status (status),
    INDEX idx_created_at (created_at)
) ENGINE=InnoDB 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci 
COMMENT='游戏用户表';

-- 创建游戏登录记录表（明确指定字符集）
CREATE TABLE IF NOT EXISTS login_records (
    id BIGINT AUTO_INCREMENT PRIMARY KEY COMMENT '记录唯一标识',
    user_id BIGINT NOT NULL COMMENT '用户ID',
    login_ip VARCHAR(45) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT '登录IP地址',
    user_agent TEXT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT '用户代理信息',
    login_time DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '登录时间',
    logout_time DATETIME COMMENT '登出时间',
    session_duration INT COMMENT '会话时长(秒)',
    login_result TINYINT DEFAULT 1 COMMENT '登录结果: 1-成功, 2-失败',
    failure_reason VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT '失败原因',
    
    FOREIGN KEY (user_id) REFERENCES game_users(id) ON DELETE CASCADE,
    INDEX idx_user_id (user_id),
    INDEX idx_login_time (login_time),
    INDEX idx_login_result (login_result)
) ENGINE=InnoDB 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci 
COMMENT='游戏登录记录表';

-- 创建用户角色表（明确指定字符集）
CREATE TABLE IF NOT EXISTS user_roles (
    id INT AUTO_INCREMENT PRIMARY KEY COMMENT '角色ID',
    role_name VARCHAR(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci NOT NULL UNIQUE COMMENT '角色名称',
    role_description TEXT CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci COMMENT '角色描述',
    permissions JSON COMMENT '权限列表',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '创建时间'
) ENGINE=InnoDB 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci 
COMMENT='用户角色表';

-- 创建用户角色关联表
CREATE TABLE IF NOT EXISTS user_role_relations (
    id BIGINT AUTO_INCREMENT PRIMARY KEY COMMENT '关联ID',
    user_id BIGINT NOT NULL COMMENT '用户ID',
    role_id INT NOT NULL COMMENT '角色ID',
    assigned_at DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '分配时间',
    
    FOREIGN KEY (user_id) REFERENCES game_users(id) ON DELETE CASCADE,
    FOREIGN KEY (role_id) REFERENCES user_roles(id) ON DELETE CASCADE,
    UNIQUE KEY uk_user_role (user_id, role_id)
) ENGINE=InnoDB 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci 
COMMENT='用户角色关联表';

-- 插入默认角色（包含中文）
INSERT IGNORE INTO user_roles (role_name, role_description, permissions) VALUES 
('player', '普通玩家', '{"can_play": true, "can_chat": true}'),
('vip', 'VIP玩家', '{"can_play": true, "can_chat": true, "special_rewards": true}'),
('moderator', '管理员', '{"can_play": true, "can_chat": true, "can_moderate": true}'),
('admin', '超级管理员', '{"all_permissions": true}');

-- 创建索引优化查询性能
CREATE INDEX idx_game_users_level ON game_users(level);
CREATE INDEX idx_game_users_coins ON game_users(coins);
CREATE INDEX idx_login_records_ip ON login_records(login_ip);
CREATE INDEX idx_login_records_time_range ON login_records(login_time, logout_time);

-- 插入测试中文数据
INSERT INTO game_users (username, email, password_hash, nickname) VALUES 
('中文用户1', 'chinese1@example.com', 'hashed_password_chinese1', '测试昵称一'),
('中文用户2', 'chinese2@example.com', 'hashed_password_chinese2', '测试昵称二');

-- 验证字符集设置
SELECT 
    TABLE_NAME,
    TABLE_COLLATION,
    CHARACTER_SET_NAME
FROM information_schema.TABLES 
WHERE TABLE_SCHEMA = 'cube_game';

-- 验证中文数据存储
SELECT id, username, nickname FROM game_users WHERE username LIKE '中文%';

-- 显示创建结果
SELECT 'Database and tables created successfully with UTF8MB4 charset!' AS result;
SELECT TABLE_NAME, TABLE_COMMENT FROM information_schema.TABLES 
WHERE TABLE_SCHEMA = 'cube_game' 
ORDER BY TABLE_NAME;