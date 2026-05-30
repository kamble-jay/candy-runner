using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCardHandler : MonoBehaviour
{
    public PlayerData player;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI scoreText;
    public Button card;
    public Button deleteCard;

    private void Start()
    {
        if (card)
            card.onClick.AddListener(() => ProfileManager.Instance.SetMainProfile(player));
        if (deleteCard)
            deleteCard.onClick.AddListener(() => ProfileManager.Instance.DeleteProfile(this));
    }

    public void UpdateInfo(PlayerData playerData)
    {
        player = playerData;

        if (playerNameText) playerNameText.text = playerData.playerName;
        if (scoreText) scoreText.text = playerData.score.ToString();
    }
}
