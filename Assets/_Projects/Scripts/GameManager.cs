using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool IsPlaying => state == GameState.Playing;

    [SerializeField] private float timeLimitSeconds = 120f;

    private enum GameState { Playing, Won, Lost }
    private GameState state;
    private int totalCoins, totalDiamonds, coins, diamonds, respawns;
    private bool hasKey;
    private float timeLeft;
    private string message = "Collect treasure and the key, then reach the exit.";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        totalCoins = GameObject.FindGameObjectsWithTag("coin").Length;
        totalDiamonds = GameObject.FindGameObjectsWithTag("diamond").Length;
        timeLeft = timeLimitSeconds;
        state = GameState.Playing;
        Debug.Log($"[Awake] Found {totalCoins} coins and {totalDiamonds} diamonds.");
    }

    private void Update()
    {
        if (!IsPlaying) return;
        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0f;
            state = GameState.Lost;
            message = "Time ran out.";
            Debug.Log("[GameManager] YOU LOSE - time ran out.");
        }
    }

    public void CollectCoin()
    {
        if (!IsPlaying) return;
        coins++;
        Debug.Log($"[GameManager] Coins {coins}/{totalCoins}");
    }

    public void CollectDiamond()
    {
        if (!IsPlaying) return;
        diamonds++;
        Debug.Log($"[GameManager] Diamonds {diamonds}/{totalDiamonds}");
    }

    public void CollectKey()
    {
        if (!IsPlaying) return;
        hasKey = true;
        Debug.Log("[GameManager] Key collected.");
    }

    public void RecordRespawn(string reason = "Hazard hit - returned to spawn.")
    {
        if (!IsPlaying) return;
        respawns++;
        message = reason;
        Debug.Log($"[GameManager] Respawn recorded ({respawns}). {reason}");
    }

    public void ReachExit()
    {
        if (!IsPlaying) return;
        if (coins >= totalCoins && diamonds >= totalDiamonds && hasKey)
        {
            state = GameState.Won;
            message = "All treasure and the key collected!";
            Debug.Log("[GameManager] YOU WIN!");
        }
        else
        {
            message = "Exit locked: collect all coins, diamonds and the key.";
            Debug.Log("[GameManager] " + message);
        }
    }

    private void OnGUI()
    {
        GUIStyle status = new GUIStyle(GUI.skin.label) { fontSize = 22 };
        status.normal.textColor = Color.white;
        GUI.Label(new Rect(16, 12, 700, 35), $"Coins {coins}/{totalCoins}   Diamonds {diamonds}/{totalDiamonds}   Key {(hasKey ? "Yes" : "No")}", status);
        GUI.Label(new Rect(16, 46, 700, 35), $"Time {Mathf.CeilToInt(timeLeft)}s   Respawns {respawns}", status);
        GUI.Label(new Rect(16, 80, 900, 35), message, status);

        if (state == GameState.Playing) return;
        GUIStyle result = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        result.normal.textColor = state == GameState.Won ? Color.green : Color.red;
        GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 85), state == GameState.Won ? "YOU WIN!" : "YOU LOSE!", result);
        GUIStyle restart = new GUIStyle(status) { alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(0, Screen.height * 0.35f + 85, Screen.width, 40), "Press R to restart", restart);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
