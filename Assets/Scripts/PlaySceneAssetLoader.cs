using UnityEngine;
using UnityEngine.UI;
public class PlaySceneAssetLoader : MonoBehaviour
{
    private GameObject _loadingScreen;
    private Slider _progressBar;

    private void Awake()
    {
        _loadingScreen = GameManager.Instance.loadingScreen;
        _progressBar = GameManager.Instance.progressBar;

        if (ObjectPoolManager.instancePool != null)
            ObjectPoolManager.instancePool.enabled = false;
    }

    private async void Start()
    {
        SetLoading(true);

        await AssetLoader.Instance.PreloadGroupAsync("Common", p =>
        {
            if (_progressBar) _progressBar.value = p * 0.5f;
        });

        await AssetLoader.Instance.PreloadGroupAsync("PlayScene", p =>
        {
            if (_progressBar) _progressBar.value = 0.5f + p * 0.5f;
        });

        SetLoading(false);

        if (ObjectPoolManager.instancePool != null)
            ObjectPoolManager.instancePool.enabled = true;
    }

    private void SetLoading(bool visible)
    {
        if (_loadingScreen) _loadingScreen.SetActive(visible);
        if (_progressBar && !visible) _progressBar.value = 0f;
    }
}
