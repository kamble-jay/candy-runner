using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AssetLoader : MonoBehaviour
{
    public static AssetLoader Instance { get; private set; }

    private readonly Dictionary<string, AsyncOperationHandle> _handles = new();

    private readonly Dictionary<string, Task> _inProgress = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task PreloadGroupAsync(string label, Action<float> onProgress = null)
    {
        if (_handles.ContainsKey(label))
        {
            onProgress?.Invoke(1f);
            return;
        }

        if (_inProgress.TryGetValue(label, out var existing))
        {
            await existing;
            return;
        }

        var task = LoadGroupInternalAsync(label, onProgress);
        _inProgress[label] = task;
        try   { await task; }
        finally { _inProgress.Remove(label); }
    }

    public async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
    {
        if (_handles.TryGetValue(address, out var cached))
            return (T)cached.Result;

        var handle = Addressables.LoadAssetAsync<T>(address);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            _handles[address] = handle;
            return handle.Result;
        }

        Debug.LogError($"[AssetLoader] Failed to load '{address}': {handle.OperationException}");
        Addressables.Release(handle);
        return null;
    }

    public void Release(string key)
    {
        if (_handles.TryGetValue(key, out var handle))
        {
            Addressables.Release(handle);
            _handles.Remove(key);
            Debug.Log($"[AssetLoader] Released '{key}'");
        }
    }

    public void ReleaseAll()
    {
        foreach (var kv in _handles)
            Addressables.Release(kv.Value);
        _handles.Clear();
    }

    private async Task LoadGroupInternalAsync(string label, Action<float> onProgress)
    {
        var sizeHandle = Addressables.GetDownloadSizeAsync(label);
        await sizeHandle.Task;
        long downloadSize = sizeHandle.Result;
        Addressables.Release(sizeHandle);

        if (downloadSize > 0)
        {
            Debug.Log($"[AssetLoader] Downloading {downloadSize / 1024f:F1} KB for '{label}'");
            var dlHandle = Addressables.DownloadDependenciesAsync(label);
            while (!dlHandle.IsDone)
            {
                onProgress?.Invoke(dlHandle.PercentComplete * 0.5f);
                await Task.Yield();
            }
            Addressables.Release(dlHandle);
        }

        var loadHandle = Addressables.LoadAssetsAsync<UnityEngine.Object>(
            label,
            asset => Debug.Log($"[AssetLoader] Loaded: {asset.name}"));

        while (!loadHandle.IsDone)
        {
            float baseProgress = downloadSize > 0 ? 0.5f : 0f;
            onProgress?.Invoke(baseProgress + loadHandle.PercentComplete * (1f - baseProgress));
            await Task.Yield();
        }

        if (loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            _handles[label] = loadHandle;
            onProgress?.Invoke(1f);
            Debug.Log($"[AssetLoader] Group '{label}' fully loaded.");
        }
        else
        {
            Debug.LogError($"[AssetLoader] Failed to load group '{label}': {loadHandle.OperationException}");
            Addressables.Release(loadHandle);
        }
    }

    private void OnDestroy() => ReleaseAll();
}
