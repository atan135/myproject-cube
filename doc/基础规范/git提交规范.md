# Git 提交规范

## 文档说明
本文档定义了 Cube 项目的 Git 提交消息规范，所有团队成员在提交代码时必须遵循此规范。

---

## 提交消息格式

### 基本结构

```
<type>(<scope>): <subject>

<body>

<footer>
```

- **第一行**：类型(范围): 简短描述（必填）
- **第二行**：空行
- **第三行及以后**：详细描述（可选）
- **最后**：页脚信息（可选）

---

## 1. Type（类型）- 必填

提交类型用于说明本次提交的改动性质：

| 类型 | 说明 | 示例 |
|------|------|------|
| `feat` | 新功能（feature） | `feat(combat): 添加角色战斗系统` |
| `fix` | 修复 Bug | `fix(network): 修复连接断开后无法重连的问题` |
| `docs` | 文档变更 | `docs(readme): 更新项目启动说明` |
| `style` | 代码格式调整（不影响代码运行） | `style(ui): 统一代码缩进格式` |
| `refactor` | 重构（既不是新增功能，也不是修复bug） | `refactor(maze): 优化迷宫生成算法结构` |
| `perf` | 性能优化 | `perf(render): 优化场景渲染性能` |
| `test` | 添加或修改测试 | `test(auth): 添加用户登录单元测试` |
| `chore` | 构建过程或辅助工具的变动 | `chore(deps): 更新Unity版本到2022.3` |
| `build` | 构建系统或外部依赖变更 | `build(ci): 配置GitHub Actions自动构建` |
| `ci` | 持续集成配置文件和脚本的变更 | `ci(jenkins): 添加自动部署流程` |
| `revert` | 回退之前的 commit | `revert: 回退 feat(combat): 添加战斗系统` |

### Type 选择指南

- **开发新功能**：使用 `feat`
- **修复问题**：使用 `fix`
- **优化性能**：使用 `perf`
- **代码重构**：使用 `refactor`
- **文档更新**：使用 `docs`
- **测试相关**：使用 `test`
- **配置/依赖**：使用 `chore` 或 `build`

---

## 2. Scope（范围）- 可选但推荐

Scope 用于说明本次提交影响的范围或模块：

### 项目模块范围

| 范围 | 说明 | 适用文件/目录 |
|------|------|--------------|
| `client` | 客户端整体 | `client/` |
| `server` | 服务端整体 | `server/` |
| `doc` | 文档 | `doc/` |

### 客户端模块范围

| 范围 | 说明 | 适用目录 |
|------|------|---------|
| `ui` | UI系统 | `client/Matrix/Assets/Scripts/Game/UI/` |
| `combat` | 战斗系统 | `client/Matrix/Assets/Scripts/Game/Combat/` |
| `maze` | 迷宫系统 | `client/Matrix/Assets/Scripts/Game/Maze/` |
| `character` | 角色系统 | `client/Matrix/Assets/Scripts/Game/Character/` |
| `network` | 网络模块 | `client/Matrix/Assets/Scripts/Network/` |
| `framework` | 框架层 | `client/Matrix/Assets/Scripts/Framework/` |
| `render` | 渲染相关 | 渲染、场景相关代码 |
| `audio` | 音频系统 | 音频相关代码 |
| `asset` | 资源管理 | 资源加载、管理相关 |

### 服务端模块范围

| 范围 | 说明 | 适用目录 |
|------|------|---------|
| `gateway` | 网关服务 | `server/src/Gateway/` |
| `auth` | 认证服务 | `server/src/Services/AuthService/` |
| `match` | 匹配服务 | `server/src/Services/MatchService/` |
| `game` | 游戏服务 | `server/src/Services/GameService/` |
| `data` | 数据服务 | `server/src/Services/DataService/` |
| `database` | 数据库 | `server/src/Database/` |
| `api` | API接口 | API相关代码 |

### 通用范围

| 范围 | 说明 |
|------|------|
| `config` | 配置文件 |
| `deps` | 依赖管理 |
| `build` | 构建相关 |
| `test` | 测试代码 |

### 多模块变更

如果改动涉及多个模块，可以：
- 使用更上层的范围：`feat(client): 添加多个UI界面`
- 省略范围：`feat: 实现完整的登录流程`
- 拆分成多个提交（推荐）

---

## 3. Subject（主题）- 必填

简短描述本次提交的内容，是提交消息的核心。

### 编写规则

1. **使用祈使句，现在时态**
   - ✅ 正确：`添加用户登录功能`
   - ❌ 错误：`添加了用户登录功能`、`已添加用户登录功能`

2. **不要大写首字母**
   - ✅ 正确：`feat(ui): 添加主菜单界面`
   - ❌ 错误：`feat(ui): 添加主菜单界面`

3. **结尾不加句号**
   - ✅ 正确：`fix(network): 修复断线重连问题`
   - ❌ 错误：`fix(network): 修复断线重连问题。`

4. **长度限制**
   - 建议不超过 50 个字符
   - 中文不超过 25 个字

5. **语言选择**
   - 本项目使用中文描述
   - 保持整个项目提交语言统一

### 好的示例

```
feat(combat): 添加角色技能系统
fix(network): 修复弱网环境下的断线问题
docs(api): 更新服务端API文档
refactor(maze): 优化迷宫生成算法
perf(render): 减少draw call提升帧率
```

### 不好的示例

```
❌ feat(combat): 添加了战斗系统的技能功能，包括主动技能和被动技能。（太长）
❌ fix: 修复bug（描述不清晰）
❌ update（缺少type和scope）
❌ feat(ui): UI更新。（结尾有句号，描述不具体）
```

---

## 4. Body（正文）- 可选

详细描述本次提交的改动内容，适用于复杂的改动。

### 何时需要 Body

- 改动逻辑复杂，需要详细说明
- 需要解释为什么做这个改动
- 需要说明改动的影响范围
- 与之前行为有显著差异

### 编写建议

1. **与主题之间空一行**
2. **说明改动原因和目的**
3. **描述改动的具体内容**
4. **对比改动前后的行为**
5. **每行不超过 72 个字符**

### 示例

```
feat(combat): 添加角色战斗技能系统

实现了角色的主动技能、被动技能和终极技能三种类型。
每个角色可配置3个主动技能、1个被动技能、1个终极技能。

主要改动：
- 创建技能基类 Skill.cs
- 实现技能管理器 SkillManager.cs
- 添加技能配置表 SkillConfig.json
- 实现技能触发和冷却机制

技能释放需要消耗能量，终极技能需要充能后才能使用。
```

---

## 5. Footer（页脚）- 可选

页脚用于关联 Issue 或声明不兼容变更。

### 关闭 Issue

使用关键字关闭相关 Issue：

```
Closes #123
Closes #123, #456
Fixes #789
Resolves #100
```

### 关联 Issue（不关闭）

```
Refs #123
Related to #456
See also #789
```

### 不兼容变更（Breaking Changes）

如果改动导致向后不兼容，必须在 Footer 中声明：

```
BREAKING CHANGE: 用户认证API返回格式变更

之前返回: { token: string }
现在返回: { accessToken: string, refreshToken: string }

所有调用登录接口的客户端代码需要更新。
```

### 完整示例

```
feat(api): 重构用户认证接口

升级认证机制，使用双token方案提升安全性。
增加 accessToken（1小时有效）和 refreshToken（30天有效）。

BREAKING CHANGE: 登录接口返回格式变更

之前返回:
{
  "token": "xxx"
}

现在返回:
{
  "accessToken": "xxx",
  "refreshToken": "yyy",
  "expiresIn": 3600
}

Closes #234
```

---

## 完整示例

### 示例 1：简单的新功能

```bash
git commit -m "feat(ui): 添加游戏设置界面"
```

### 示例 2：Bug修复

```bash
git commit -m "fix(network): 修复玩家断线后房间状态异常的问题"
```

### 示例 3：带详细说明的提交

```bash
git commit -m "feat(maze): 实现迷宫程序化生成算法

采用改进的 Prim 算法生成迷宫，保证所有房间可达。

主要特性：
- 支持配置迷宫大小和难度
- 自动生成不同类型的房间（安全、陷阱、资源、Boss）
- 保证至少有一条通往出口的路径
- 生成时间控制在1秒以内

算法复杂度：O(n²)，n为房间数量。

Closes #45"
```

### 示例 4：性能优化

```bash
git commit -m "perf(render): 优化场景渲染性能

通过以下方式提升渲染性能：
- 实现对象池，减少 GC 开销
- 添加遮挡剔除，减少不必要的渲染
- 合并材质球，降低 draw call

性能提升：
- 帧率从45fps提升到60fps
- Draw call从500降到150
- 内存占用降低30%

Refs #78"
```

### 示例 5：重构代码

```bash
git commit -m "refactor(network): 重构网络模块架构

将网络模块拆分为连接层、协议层和业务层，提高代码可维护性。

改动内容：
- 创建 NetworkConnection 处理连接管理
- 创建 NetworkProtocol 处理消息序列化
- 创建 NetworkService 处理业务逻辑
- 移除冗余代码约200行

此次重构不改变对外接口，不影响现有功能。"
```

### 示例 6：不兼容变更

```bash
git commit -m "feat(combat): 重构战斗伤害计算系统

优化伤害计算公式，使其更加平衡。

BREAKING CHANGE: 伤害计算公式变更

旧公式：damage = attack - defense
新公式：damage = attack * (100 / (100 + defense))

所有角色和装备的攻击、防御数值需要重新配置。

Closes #123"
```

---

## 特殊场景

### 1. 合并多个小改动

如果有多个相关的小改动，建议合并为一个提交：

```bash
git add file1.cs file2.cs file3.cs
git commit -m "feat(ui): 完善主菜单界面功能

- 添加开始游戏按钮
- 添加设置按钮
- 添加退出按钮
- 优化按钮布局"
```

### 2. 临时提交（需要后续修改）

使用 WIP（Work In Progress）前缀：

```bash
git commit -m "WIP: feat(combat): 战斗系统开发中"
```

**注意**：WIP 提交不应该合并到主分支，应该在完成后使用 `git rebase` 整理。

### 3. 紧急修复

```bash
git commit -m "hotfix(server): 修复生产环境内存泄漏问题

Closes #999"
```

### 4. 回退提交

```bash
git revert abc1234
git commit -m "revert: 回退 'feat(combat): 添加战斗系统'

此功能导致客户端崩溃，暂时回退等待修复。

Refs #456"
```

---

## Git 提交最佳实践

### 1. 提交频率

- ✅ **小步提交**：每完成一个小功能就提交
- ✅ **原子提交**：每个提交只包含一个逻辑改动
- ❌ **避免巨大提交**：不要一次提交几千行代码
- ❌ **避免混合提交**：不要在一个提交中混合多个不相关的改动

### 2. 提交前检查

```bash
# 查看改动内容
git diff

# 查看暂存区内容
git diff --staged

# 检查代码格式
# （确保通过 linter 检查）

# 运行测试
# （确保测试通过）
```

### 3. 修改最后一次提交

```bash
# 修改提交消息
git commit --amend -m "新的提交消息"

# 添加遗漏的文件
git add forgotten_file.cs
git commit --amend --no-edit
```

**注意**：不要修改已经推送到远程的提交！

### 4. 拆分大改动

如果改动很大，使用交互式暂存：

```bash
# 选择性暂存文件的部分改动
git add -p

# 分多次提交
git commit -m "feat(ui): 添加主菜单框架"
git add other_files.cs
git commit -m "feat(ui): 实现主菜单逻辑"
```

### 5. 整理提交历史

在合并到主分支前，整理提交历史：

```bash
# 交互式 rebase 最近 3 个提交
git rebase -i HEAD~3

# 可以进行：
# - 合并提交（squash）
# - 修改提交消息（reword）
# - 删除提交（drop）
# - 调整顺序（reorder）
```

---

## 工具配置

### 1. 提交模板

创建全局提交模板：

```bash
# 创建模板文件
cat > ~/.gitmessage << 'EOF'
# <type>(<scope>): <subject>
# 
# <body>
# 
# <footer>

# Type 类型（必填）：
#   feat:     新功能
#   fix:      修复Bug
#   docs:     文档
#   style:    格式
#   refactor: 重构
#   perf:     性能优化
#   test:     测试
#   chore:    构建/工具
#   build:    构建系统
#   ci:       持续集成
#   revert:   回退
#
# Scope 范围（可选）：
#   client, server, ui, combat, maze, network, etc.
#
# Subject 主题（必填）：
#   简短描述，不超过50字符
#
# Body 正文（可选）：
#   详细描述改动内容、原因和影响
#
# Footer 页脚（可选）：
#   Closes #123
#   BREAKING CHANGE: 描述不兼容变更
EOF

# 配置 Git 使用模板
git config --global commit.template ~/.gitmessage
```

### 2. Git Hooks

在项目根目录创建 `.husky` 或使用 Git hooks：

```bash
# .git/hooks/commit-msg
#!/bin/sh

commit_msg=$(cat "$1")
pattern="^(feat|fix|docs|style|refactor|perf|test|chore|build|ci|revert)(\(.+\))?: .{1,50}"

if ! echo "$commit_msg" | grep -qE "$pattern"; then
    echo "错误：提交消息不符合规范"
    echo "格式：<type>(<scope>): <subject>"
    echo "示例：feat(combat): 添加角色技能系统"
    exit 1
fi
```

### 3. Commitizen（可选）

安装 Commitizen 实现交互式提交：

```bash
# 全局安装
npm install -g commitizen cz-conventional-changelog

# 初始化项目
commitizen init cz-conventional-changelog --save-dev --save-exact

# 使用
git cz
# 或
npm run commit
```

### 4. Commitlint（可选）

自动检查提交消息格式：

```bash
# 安装
npm install --save-dev @commitlint/config-conventional @commitlint/cli

# 配置 commitlint.config.js
module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'type-enum': [
      2,
      'always',
      [
        'feat', 'fix', 'docs', 'style', 'refactor',
        'perf', 'test', 'chore', 'build', 'ci', 'revert'
      ]
    ]
  }
};
```

---

## 常见问题

### Q1: 忘记写范围(scope)怎么办？

A: Scope 是可选的，但推荐填写。如果改动范围不明确，可以省略。

### Q2: 一次改动涉及多个模块怎么办？

A: 
- 优先考虑拆分成多个提交
- 如果必须一起提交，使用更上层的范围
- 或者省略范围，在 subject 中说明

### Q3: 提交消息写错了怎么办？

A:
```bash
# 如果还没有 push
git commit --amend -m "正确的提交消息"

# 如果已经 push
# 不推荐修改，除非是个人分支
git commit --amend -m "正确的提交消息"
git push -f
```

### Q4: 需要回退某个提交怎么办？

A:
```bash
# 使用 revert（推荐，保留历史）
git revert <commit-hash>

# 使用 reset（危险，会删除历史）
git reset --hard <commit-hash>
```

### Q5: 开发到一半需要临时提交怎么办？

A: 使用 `git stash` 暂存改动，或使用 WIP 提交（记得后续整理）。

---

## 检查清单

在提交代码前，请确认：

- [ ] 提交消息符合格式规范
- [ ] Type 类型选择正确
- [ ] Scope 范围填写准确（如适用）
- [ ] Subject 描述清晰简洁
- [ ] 代码已通过编译
- [ ] 代码已通过 Linter 检查
- [ ] 相关测试已通过
- [ ] 没有提交调试代码
- [ ] 没有提交敏感信息（密码、密钥等）
- [ ] 提交内容是原子性的（单一职责）
- [ ] 如有不兼容变更，已添加 BREAKING CHANGE 说明

---

## 参考资源

- [Conventional Commits 规范](https://www.conventionalcommits.org/)
- [Angular 提交规范](https://github.com/angular/angular/blob/master/CONTRIBUTING.md)
- [Commitizen 工具](https://github.com/commitizen/cz-cli)
- [Commitlint 工具](https://github.com/conventional-changelog/commitlint)

---

## 文档修订历史

| 版本 | 日期 | 修改内容 | 修改人 |
|------|------|---------|--------|
| 1.0 | 2026-01-22 | 创建文档 | System |

---

**本规范自发布之日起生效，所有团队成员必须遵守。**
