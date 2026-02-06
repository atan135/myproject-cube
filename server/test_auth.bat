@echo off
echo 测试用户注册和登录功能
echo =========================

set SERVER_URL=http://localhost:6953

echo.
echo 1. 测试公开接口
curl -X GET %SERVER_URL%/api/test/public
echo.

echo.
echo 2. 测试受保护接口（应该返回401）
curl -X GET %SERVER_URL%/api/test/protected
echo.

echo.
echo 3. 用户注册测试
echo 注册用户: testuser1
curl -X POST %SERVER_URL%/api/auth/register ^
  -H "Content-Type: application/json" ^
  -d "{\"username\":\"testuser1\",\"email\":\"test1@example.com\",\"password\":\"password123\",\"nickname\":\"测试用户1\"}"
echo.

echo.
echo 4. 重复注册同一用户名（应该失败）
curl -X POST %SERVER_URL%/api/auth/register ^
  -H "Content-Type: application/json" ^
  -d "{\"username\":\"testuser1\",\"email\":\"test2@example.com\",\"password\":\"password123\"}"
echo.

echo.
echo 5. 用户登录测试
echo 登录用户: testuser1
curl -X POST %SERVER_URL%/api/auth/login ^
  -H "Content-Type: application/json" ^
  -d "{\"username\":\"testuser1\",\"password\":\"password123\"}"
echo.

echo.
echo 6. 错误密码登录测试
curl -X POST %SERVER_URL%/api/auth/login ^
  -H "Content-Type: application/json" ^
  -d "{\"username\":\"testuser1\",\"password\":\"wrongpassword\"}"
echo.

echo.
echo 7. 不存在的用户登录测试
curl -X POST %SERVER_URL%/api/auth/login ^
  -H "Content-Type: application/json" ^
  -d "{\"username\":\"nonexistent\",\"password\":\"password123\"}"
echo.

echo 测试完成！
pause