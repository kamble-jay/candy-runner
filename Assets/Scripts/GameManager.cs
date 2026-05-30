using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    public System.Action<int> OnScoreChanged;
    public System.Action OnGameOver;
    
    public int Score{ get; private set; }
    public int BestScore{ get; private set; }
    public bool IsPlaying{ get; private set; }
    
    [HideInInspector] public PlayerController player;
    [HideInInspector] public Button playButton;

    [Header("Persistent UI (lives on the DontDestroyOnLoad canvas)")]
    public GameObject loadingScreen;
    public Slider progressBar;
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    
    public Transform leaderBoardParent;
    
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BestScore = PlayerPrefs.GetInt("BestScore", 0);
        IsPlaying = false;
    }
    
    public void RegisterPlayButton(Button btn)
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayPressed);

        playButton = btn;
        playButton.onClick.AddListener(OnPlayPressed);
    }

    public void StartGame()
    {
        Time.timeScale = 1;
        Score     = 0;
        IsPlaying = true;
        player?.ResetState();
        SetGameOverPanel(false);
        OnScoreChanged?.Invoke(Score);
    }

    public void AddScore(int amount)
    {
        if (!IsPlaying) return;
        Score += amount;

        if (Score > BestScore)
        {
            BestScore = Score;
            PlayerPrefs.SetInt("BestScore", BestScore);
        }
        
        PlaySceneManager.Instance?.UpdateScore(Score);
        OnScoreChanged?.Invoke(Score);
    }

    public void GameOver()
    {
        if (!IsPlaying) return;

        IsPlaying = false;
        Time.timeScale = 0;

        ProfileManager.Instance?.SubmitScore(Score);

        if (finalScoreText) finalScoreText.text = $"Score: {Score}";
        SetGameOverPanel(true);
        OnGameOver?.Invoke();

        Debug.Log($"[GameManager] Game Over — Score: {Score}  Best: {BestScore}");
    }

    public void RestartGame()
    {
        Time.timeScale = 1;
        IsPlaying = false;
        SetGameOverPanel(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoHome()
    {
        Time.timeScale = 1;
        IsPlaying = false;
        SetGameOverPanel(false);
        OnGoHomeAsync();
    }

    private void SetGameOverPanel(bool visible)
    {
        if (gameOverPanel) gameOverPanel.SetActive(visible);
    }
    private async void OnPlayPressed()
    {
        if (ProfileManager.Instance == null || ProfileManager.Instance.currentProfile == null)
        {
            Debug.LogWarning("[GameManager] No profile selected — cannot start game.");
            return;
        }

        if (playButton) playButton.interactable = false;
        SetLoadingScreen(true);

        await AssetLoader.Instance.PreloadGroupAsync("PlayScene", p =>
        {
            if (progressBar) progressBar.value = p;
        });

        AssetLoader.Instance.Release("HomeScene");
        SceneManager.LoadScene("PlayScene");
    }

    private async void OnGoHomeAsync()
    {
        SetLoadingScreen(true);
        AssetLoader.Instance.Release("PlayScene");

        await AssetLoader.Instance.PreloadGroupAsync("HomeScene", p =>
        {
            if (progressBar) progressBar.value = p;
        });

        var sceneHandle = UnityEngine.AddressableAssets.Addressables.LoadSceneAsync(
            "Assets/Scene/HomeScreen.unity",
            LoadSceneMode.Single
        );

        while (!sceneHandle.IsDone)
        {
            if (progressBar) progressBar.value = sceneHandle.PercentComplete;
            await System.Threading.Tasks.Task.Yield();
        }

        SetLoadingScreen(false);
    }

    private void SetLoadingScreen(bool visible)
    {
        if (loadingScreen) loadingScreen.SetActive(visible);
        if (progressBar)   progressBar.value = 0f;
    }
}
