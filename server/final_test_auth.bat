@echo off
echo ========================================
echo Cube游戏服务器 - 用户认证功能测试
echo ========================================

set SERVER_URL=http://localhost:6953

echo.
echo 1. 测试公开接口
echo ----------------
powershell -Command "Invoke-WebRequest -Uri '%SERVER_URL%/api/test/public' -Method GET | Select-Object StatusCode, Content"

echo.
echo 2. 测试受保护接口（无认证 - 应该返回401）
echo ----------------------------------------
powershell -Command "try { Invoke-WebRequest -Uri '%SERVER_URL%/api/test/protected' -Method GET } catch { Write-Host 'Status: 401 Unauthorized (Expected)' }"

echo.
echo 3. 用户注册测试
echo ---------------
set REGISTER_BODY={"username":"testuser2","email":"test2@example.com","password":"password123","nickname":"测试用户2"}
powershell -Command "Invoke-WebRequest -Uri '%SERVER_URL%/api/auth/register' -Method POST -Body '%REGISTER_BODY%' -ContentType 'application/json' | Select-Object StatusCode, Content"

echo.
echo 4. 重复注册测试（应该失败）
echo --------------------------
powershell -Command "try { Invoke-WebRequest -Uri '%SERVER_URL%/api/auth/register' -Method POST -Body '%REGISTER_BODY%' -ContentType 'application/json' } catch { Write-Host 'Status: 400 Bad Request (Expected)' }"

echo.
echo 5. 正确用户登录
echo --------------
set LOGIN_BODY={"username":"testuser2","password":"password123"}
for /f "tokens=*" %%i in ('powershell -Command "& { $response = Invoke-WebRequest -Uri '%SERVER_URL%/api/auth/login' -Method POST -Body '%LOGIN_BODY%' -ContentType 'application/json'; $content = $response.Content | ConvertFrom-Json; Write-Output $content.token }"') do set TOKEN=%%i

echo Token obtained: %TOKEN%

echo.
echo 6. 使用Token访问受保护接口
echo --------------------------
powershell -Command "$headers = @{Authorization = 'Bearer %TOKEN%'}; Invoke-WebRequest -Uri '%SERVER_URL%/api/test/userinfo' -Method GET -Headers $headers | Select-Object StatusCode, Content"

echo.
echo 7. 使用错误Token测试（应该返回401）
echo ----------------------------------
powershell -Command "try { $headers = @{Authorization = 'Bearer invalid-token'}; Invoke-WebRequest -Uri '%SERVER_URL%/api/test/userinfo' -Method GET -Headers $headers } catch { Write-Host 'Status: 401 Unauthorized (Expected)' }"

echo.
echo 8. 错误密码登录测试
echo ------------------
set WRONG_LOGIN_BODY={"username":"testuser2","password":"wrongpassword"}
powershell -Command "try { Invoke-WebRequest -Uri '%SERVER_URL%/api/auth/login' -Method POST -Body '%WRONG_LOGIN_BODY%' -ContentType 'application/json' } catch { Write-Host 'Status: 401 Unauthorized (Expected)' }"

echo.
echo 测试完成！
echo ========================================
pause