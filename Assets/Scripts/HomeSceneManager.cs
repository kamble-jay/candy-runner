using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HomeSceneManager : MonoBehaviour
{
    public static HomeSceneManager Instance { get; private set; }

    [Header("References")]
    public Button playButton;

    public Button profileEditButton;

    [Header("Profile UI (optional — can live on home canvas)")]
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI highestScoreText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        GameManager.Instance.RegisterPlayButton(playButton);
        AsssignProfileEditButton();
        if (ProfileManager.Instance != null)
        {
            if (playerNameText   != null) ProfileManager.Instance.playerNameText   = playerNameText;
            if (highestScoreText != null) ProfileManager.Instance.highestScoreText = highestScoreText;
            ProfileManager.Instance.RefreshHeaderUI_Public();
        }
    }

    public void AsssignProfileEditButton()
    {
        profileEditButton.onClick.AddListener(() =>
        {
            ProfileManager.Instance.ProfilePanel.SetActive(true);
        });
    }
}
