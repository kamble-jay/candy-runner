using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance { get; private set; }

    public GameObject ProfilePanel;

    public List<PlayerData> profiles = new List<PlayerData>();
    public PlayerData currentProfile;

    [Header("UI — can be re-pointed by HomeSceneManager each scene load")]
    public GameObject profilePrefab;
    public Transform profileParent;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI highestScoreText;

    private string SavePath => Path.Combine(Application.persistentDataPath, "profiles.json");
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void Start()
    {
        AutoSelectProfile();
    }

    private void AutoSelectProfile()
    {
        if (currentProfile != null && profiles.Contains(currentProfile))
        {
            RefreshHeaderUI();
            return;
        }
        if (profiles.Count > 0)
            SetMainProfile(profiles[0]);
        else
        {
            var defaultPlayer = new PlayerData { playerName = "Player 1", score = 0 };
            profiles.Add(defaultPlayer);
            Save();
            CreateProfileCard(defaultPlayer);
            SetMainProfile(defaultPlayer);
        }
    }
    
    public void CreateProfile(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            Debug.LogWarning("[ProfileManager] Profile name cannot be empty.");
            return;
        }
        var newProfile = new PlayerData { playerName = name.Trim(), score = 0 };
        profiles.Add(newProfile);
        Save();
        CreateProfileCard(newProfile);
    }

    public void CreateProfile(TextMeshProUGUI nameInput) => CreateProfile(nameInput.text);

    public void DeleteProfile(PlayerCardHandler cardHandler)
    {
        if (cardHandler == null) return;
        profiles.Remove(cardHandler.player);
        if (currentProfile == cardHandler.player) currentProfile = null;
        Destroy(cardHandler.gameObject);
        Save();
        AutoSelectProfile();
    }

    public void SetMainProfile(PlayerData profile)
    {
        if (profile == null) { Debug.LogError("[ProfileManager] SetMainProfile — null profile."); return; }
        currentProfile = profile;
        RefreshHeaderUI();
        Debug.Log($"[ProfileManager] Active: {currentProfile.playerName} — best: {currentProfile.score}");
    }
    
    public void SubmitScore(int score)
    {
        if (currentProfile == null) { Debug.LogWarning("[ProfileManager] No profile selected."); return; }
        if (score <= currentProfile.score) return;
        currentProfile.score = score;
        Save();
        RefreshHeaderUI();
        RefreshCard(currentProfile);
        Debug.Log($"[ProfileManager] New high score — {currentProfile.playerName}: {score}");
    }
    
    private void CreateProfileCard(PlayerData profile)
    {
        if (profilePrefab == null || profileParent == null) return;
        var card = Instantiate(profilePrefab, profileParent);
        card.GetComponent<PlayerCardHandler>()?.UpdateInfo(profile);
    }

    private void RefreshCard(PlayerData profile)
    {
        if (profileParent == null) return;
        foreach (Transform child in profileParent)
        {
            var card = child.GetComponent<PlayerCardHandler>();
            if (card != null && card.player == profile)
            {
                card.UpdateInfo(profile);
                return;
            }
        }
    }

    private void RefreshHeaderUI()
    {
        if (currentProfile == null) return;
        if (playerNameText   != null) playerNameText.text   = currentProfile.playerName;
        if (highestScoreText != null) highestScoreText.text = currentProfile.score.ToString();
    }
    
    public void RefreshHeaderUI_Public() => RefreshHeaderUI();

    private void Load()
    {
        if (!File.Exists(SavePath)) return;
        try
        {
            var wrapper = JsonUtility.FromJson<ProfileListWrapper>(File.ReadAllText(SavePath));
            if (wrapper?.profiles == null) return;
            profiles = wrapper.profiles;
            foreach (var p in profiles) CreateProfileCard(p);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ProfileManager] Load failed: {e.Message}");
        }
    }

    [SerializeField]private GameObject profileCard;
    [SerializeField]private Transform leaderBoardParent;
    public void SetLeaderBoard()
    {
        foreach (Transform t in leaderBoardParent)
            Destroy(t.gameObject);
        
        var p = profiles;
        p.Sort((a, b) => b.score.CompareTo(a.score));
        foreach (var profile in p)
        {
            GameObject go = Instantiate(profileCard, leaderBoardParent);
            go.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = profile.score.ToString();
            go.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = profile.playerName;
        }
    }


    private void Save()
    {
        try
        {
            File.WriteAllText(SavePath, JsonUtility.ToJson(new ProfileListWrapper { profiles = profiles }, true));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ProfileManager] Save failed: {e.Message}");
        }
    }
}

[System.Serializable] public class ProfileListWrapper { public List<PlayerData> profiles; }

[System.Serializable]
public class PlayerData
{
    public string playerName;
    public int score;
}
