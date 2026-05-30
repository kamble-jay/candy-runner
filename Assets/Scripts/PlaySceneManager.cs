using TMPro;
using UnityEngine;

public class PlaySceneManager : MonoBehaviour
{
    public static PlaySceneManager Instance { get; private set; }

    [Header("References")]
    public GameObject characterPrefab;
    public Transform huddleSpawnPosition;
    public Transform characterSpawnPoint;

    [Header("UI")]
    public TextMeshProUGUI scoreText;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        scoreText.text = "0";
        var ch = Instantiate(characterPrefab, characterSpawnPoint);
        var controller = ch.GetComponent<PlayerController>();
        GameManager.Instance.player = controller;
        GameManager.Instance.StartGame();
    }

    public void UpdateScore(int score)
    {
        if (scoreText) scoreText.text = score.ToString();
    }
}
