using System;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;
using System.IO;


public class YooAssetLauncher : MonoBehaviour
{
    public enum YooAssetLoadMode{
        EditorSimulateMode,
        OfflinePlayMode,
        HostPlayMode
    }
    public YooAssetLoadMode loadMode;
    private string _packageName = "DefaultPackage";

    public async Task InitializeYooAsset()
    {
        // 建议在 Start 中调用异步方法，不要直接在 async void 中写太多业务
        bool success = await InitializeYooAssetAsync();
        
        if (success)
        {
            await LoadGameSceneAsync();
        }
    }
    private async Task<bool> InitializeYooAssetAsync()
    {
        switch (loadMode)
        {
            case YooAssetLoadMode.EditorSimulateMode:
                return await InitializeYooAssetAsyncEditorSimulateMode();
            case YooAssetLoadMode.OfflinePlayMode:
                return await InitializeYooAssetAsyncOfflinePlayMode();
            case YooAssetLoadMode.HostPlayMode:
                return await InitializeYooAssetAsyncHostPlayMode();
            default:
                return await InitializeYooAssetAsyncEditorSimulateMode();
        }
    }

    private async Task<bool> InitializeYooAssetAsyncEditorSimulateMode()
    {
        #if UNITY_EDITOR
        try
        {
            // 1. 初始化资源系统
            YooAssets.Initialize();

            // 2. 创建并设置默认包
            var package = YooAssets.CreatePackage(_packageName);
            YooAssets.SetDefaultPackage(package);

            var buildResult = EditorSimulateModeHelper.SimulateBuild(_packageName);    
            var packageRoot = buildResult.PackageRootDirectory;
            var fileSystemParams = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
    
            var createParameters = new EditorSimulateModeParameters();
            createParameters.EditorFileSystemParameters = fileSystemParams;
            // 4. 异步初始化并等待
            // YooAsset 的 Operation 对象可以直接 await 其 .Task 属性
            InitializationOperation operation = package.InitializeAsync(createParameters);
            await operation.Task;

            var versionOperation = package.RequestPackageVersionAsync();
            await versionOperation.Task;

            // 3. 【核心】更新清单
            // 只有这一步成功后，ActiveManifest 才会从 null 变成有效值
            var updateOperation = package.UpdatePackageManifestAsync(versionOperation.PackageVersion);
            await updateOperation.Task;
            if (updateOperation.Status == EOperationStatus.Succeed) 
            {
                // 此时 ActiveManifest 已经成功赋值
                Debug.Log("清单更新成功！");
                return true;
            }
            return false;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
        #else
            return false;
        #endif
    }
    private async Task<bool> InitializeYooAssetAsyncOfflinePlayMode()
    {
        try
        {
            // 1. 初始化资源系统
            YooAssets.Initialize();

            // 2. 创建并设置默认包
            var package = YooAssets.GetPackage(_packageName);
            if (package == null)
            {
                package = YooAssets.CreatePackage(_packageName);
            }
            YooAssets.SetDefaultPackage(package);
            var fileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
    
            var createParameters = new OfflinePlayModeParameters();
            createParameters.BuildinFileSystemParameters = fileSystemParams;
            // 4. 异步初始化并等待
            // YooAsset 的 Operation 对象可以直接 await 其 .Task 属性
            InitializationOperation operation = package.InitializeAsync(createParameters);
            await operation.Task;
            if (operation.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"[{_packageName}] 初始化失败: {operation.Error}");
                return false;
            }
            var versionOperation = package.RequestPackageVersionAsync();
            await versionOperation.Task;
            if (versionOperation.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"[{_packageName}] 初始化失败: {versionOperation.Error}");
                return false;
            }
            string packageVersion = versionOperation.PackageVersion;
            Debug.Log($"[{_packageName}] 包版本: {packageVersion}");
            var updateOperation = package.UpdatePackageManifestAsync(packageVersion);
            await updateOperation.Task;
            if (updateOperation.Status != EOperationStatus.Succeed) 
            { 
                Debug.LogError($"[{_packageName}] 更新版本失败: {updateOperation.Error}");
                return false;
            }
            // 此时 ActiveManifest 已经成功赋值
            Debug.Log("离线模式 ActiveManifest 已就绪");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }
    private async Task<bool> InitializeYooAssetAsyncHostPlayMode()
    {
        try
        {
            // 1. 初始化资源系统
            YooAssets.Initialize();

            // 2. 创建并设置默认包
            var package = YooAssets.CreatePackage(_packageName);
            YooAssets.SetDefaultPackage(package);
            string defaultHostServer = "http://127.0.0.1/CDN/Android/v1.0";
            string fallbackHostServer = "http://127.0.0.1/CDN/Android/v1.0";
            IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
            var cacheFileSystemParams = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
            var buildinFileSystemParams = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();   
    
            var createParameters = new HostPlayModeParameters();
            createParameters.BuildinFileSystemParameters = buildinFileSystemParams; 
            createParameters.CacheFileSystemParameters = cacheFileSystemParams;
            // 4. 异步初始化并等待
            // YooAsset 的 Operation 对象可以直接 await 其 .Task 属性
            InitializationOperation operation = package.InitializeAsync(createParameters);
            await operation.Task;

            if (operation.Status == EOperationStatus.Succeed)
            {
                Debug.Log($"[{_packageName}] 初始化成功!");
                return true;
            }
            else
            {
                Debug.LogError($"[{_packageName}] 初始化失败: {operation.Error}");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }

    private async Task LoadGameSceneAsync()
    {
        // var package = YooAssets.GetPackage(_packageName);
        // // 示例：异步加载资源
        // AssetHandle handle = package.LoadAssetAsync<GameObject>("LoginWindow");
        
        // await handle.Task; // 等待加载完成

        // if (handle.Status == EOperationStatus.Succeed)
        // {
        //     Instantiate(handle.AssetObject);
        //     // 记得在不需要时释放句柄
        //     // handle.Dispose(); 
        // }
    }

    
    private async Task RequestPackageVersion()
    {
        var package = YooAssets.GetPackage("DefaultPackage");
        var operation = package.RequestPackageVersionAsync();
        await operation.Task;
        //更新成功
        if (operation.Status == EOperationStatus.Succeed)
        {
            //更新成功
            string packageVersion = operation.PackageVersion;
            Debug.Log($"Request package Version : {packageVersion}");
        }else{
            //更新失败
            Debug.LogError(operation.Error);
        }
        
    }


    /// <summary>
    /// 远端资源地址查询服务类
    /// </summary>
    private class RemoteServices : IRemoteServices
    {
        private readonly string _defaultHostServer;
        private readonly string _fallbackHostServer;

        public RemoteServices(string defaultHostServer, string fallbackHostServer)
        {
            _defaultHostServer = defaultHostServer;
            _fallbackHostServer = fallbackHostServer;
        }
        string IRemoteServices.GetRemoteMainURL(string fileName)
        {
            return $"{_defaultHostServer}/{fileName}";
        }
        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            return $"{_fallbackHostServer}/{fileName}";
        }
    }
}