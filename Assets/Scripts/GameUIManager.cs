using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance;

    [Header("Mana Bars")]
    public Image manaBarP1;
    public Image manaBarP2;

    [Header("Time Display")]
    public TMP_Text timeText;

    [Header("Buttons")]
    public Button btnSpawnBox;
    public Button btnFreeze;
    public Button btnEarthquake;
    public Button btnTornado;

    private Player localPlayer;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void RegisterLocalPlayer(Player player)
    {
        localPlayer = player;

        if (btnSpawnBox != null)
            btnSpawnBox.onClick.AddListener(() => localPlayer.OnClickSpawnBox());

        if (btnFreeze != null)
            btnFreeze.onClick.AddListener(() => localPlayer.OnClickSkill("Freeze"));

        if (btnEarthquake != null)
            btnEarthquake.onClick.AddListener(() => localPlayer.OnClickSkill("Earthquake"));

        if (btnTornado != null)
            btnTornado.onClick.AddListener(() => localPlayer.OnClickSkill("Tornado"));
    }

    public void UpdateMana(float current, int playerIndex, float max)
    {
        float fill = current / max;
        if (playerIndex == 0 && manaBarP1 != null)
            manaBarP1.fillAmount = fill;
        else if (playerIndex == 1 && manaBarP2 != null)
            manaBarP2.fillAmount = fill;
    }

    public void UpdateTime(float time)
    {
        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    // Cập nhật mana cho tất cả players
    public void UpdateAllPlayerMana()
    {
        Player[] allPlayers = FindObjectsOfType<Player>();
        for (int i = 0; i < allPlayers.Length; i++)
        {
            if (i < 2) // Chỉ hỗ trợ tối đa 2 players
            {
                UpdateMana(allPlayers[i].GetCurrentMana(), i, allPlayers[i].maxMana);
            }
        }
    }
}