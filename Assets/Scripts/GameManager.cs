using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Transform gridContainer;

    public GameObject blockPrefab;
    public GameObject orange1Prefab;
    public GameObject orange2Prefab;
    public GameObject orange3Prefab;
    public GameObject orange4Prefab;
    public GameObject emptyPrefab;

    public Text timerText;
    public GameObject losePanel;
    public GameObject panelWin;
    public GameObject UIGame;
    public GameObject selectLevelPanel;
    public Button[] levelButtons;
    public GameObject[] levelLocks;

    private float remainingTime = 45f;
    private bool isGameOver = false;

    private GameObject[,] grid = new GameObject[4, 4];
    private Vector2 startTouch;
    private bool isSwiping = false;
    private Vector2 currentTouch;

    public int currentLevel = 0;
    private int maxUnlockedLevel = 0;
    private bool justWon = false;

    private string[][,] levels =
    {
        new string[4, 4]
        {
            { "block", "empty", "orange3", "orange4" },
            { "block", "empty", "orange2", "empty" },
            { "empty", "empty", "empty", "empty" },
            { "empty", "block", "empty", "orange1" }
        },
        new string[4, 4]
        {
            { "orange3", "empty", "empty", "orange4" },
            { "block", "block", "empty", "empty" },
            { "empty", "orange1", "empty", "orange2" },
            { "empty", "empty", "block", "empty" }
        },
        new string[4, 4]
        {
            { "orange4", "block", "orange2", "orange1" },
            { "empty", "block", "empty", "empty" },
            { "orange3", "empty", "empty", "empty" },
            { "empty", "block", "empty", "empty" }
        }
    };

    void Start()
    {
        if (UIGame != null) UIGame.SetActive(false);
        if (selectLevelPanel != null) selectLevelPanel.SetActive(true);

        if (!PlayerPrefs.HasKey("UnlockedLevel"))
        {
            PlayerPrefs.SetInt("UnlockedLevel", 1);
            PlayerPrefs.Save();
        }

        maxUnlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);
        UnlockLevels();
    }

    void UnlockLevels()
    {
        for (int i = 0; i < levelButtons.Length; i++)
        {
            bool isUnlocked = (i < maxUnlockedLevel);

            if (levelButtons[i] != null)
                levelButtons[i].interactable = isUnlocked;

            if (levelLocks != null && i < levelLocks.Length && levelLocks[i] != null)
                levelLocks[i].SetActive(!isUnlocked);
        }
    }

    public void SelectLevel(int level)
    {
        if (level >= maxUnlockedLevel)
        {
            Debug.Log("Level locked");
            return;
        }

        currentLevel = level;
        RestartLevel();
        if (selectLevelPanel != null) selectLevelPanel.SetActive(false);
        if (UIGame != null) UIGame.SetActive(true);
    }

    public void RestartLevel()
    {
        foreach (Transform child in gridContainer)
            Destroy(child.gameObject);

        grid = new GameObject[4, 4];
        CreateGrid();
        isGameOver = false;
        remainingTime = 45f;
        if (panelWin != null) panelWin.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (UIGame != null) UIGame.SetActive(true);
    }

    void CreateGrid()
    {
        string[,] layout = levels[currentLevel];

        int index = 0;
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                GameObject prefab = null;

                switch (layout[r, c])
                {
                    case "block": prefab = blockPrefab; break;
                    case "orange1": prefab = orange1Prefab; break;
                    case "orange2": prefab = orange2Prefab; break;
                    case "orange3": prefab = orange3Prefab; break;
                    case "orange4": prefab = orange4Prefab; break;
                    case "empty": prefab = emptyPrefab; break;
                }

                if (prefab != null)
                {
                    GameObject obj = Instantiate(prefab, gridContainer);
                    obj.transform.SetSiblingIndex(index++);
                    grid[r, c] = obj;

                    Cell cell = obj.GetComponent<Cell>();
                    if (cell != null)
                    {
                        cell.row = r;
                        cell.col = c;
                        cell.gameManager = this;
                    }
                }
            }
        }
    }

    void Update()
    {
        if (!isGameOver)
        {
            remainingTime -= Time.deltaTime;

            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                isGameOver = true;
                if (losePanel != null) losePanel.SetActive(true);
                if (UIGame != null) UIGame.SetActive(false);
            }

            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            if (timerText != null)
                timerText.text = $"{minutes:00}:{seconds:00}";

#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    startTouch = touch.position;
                    isSwiping = true;
                }

                if (isSwiping && touch.phase == TouchPhase.Moved)
                {
                    currentTouch = touch.position;
                    Vector2 delta = currentTouch - startTouch;

                    if (delta.magnitude > 100f)
                    {
                        isSwiping = false;

                        float absX = Mathf.Abs(delta.x);
                        float absY = Mathf.Abs(delta.y);

                        if (absX > absY)
                            MoveAll(delta.x > 0 ? Vector2.right : Vector2.left);
                        else
                            MoveAll(delta.y > 0 ? Vector2.up : Vector2.down);
                    }
                }

                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    isSwiping = false;
            }
#endif
        }
    }

    void MoveAll(Vector2 dir)
    {
        if (dir == Vector2.up)
        {
            for (int r = 1; r < 4; r++)
                for (int c = 0; c < 4; c++)
                    TryMove(r, c, -1, 0);
        }
        else if (dir == Vector2.down)
        {
            for (int r = 2; r >= 0; r--)
                for (int c = 0; c < 4; c++)
                    TryMove(r, c, 1, 0);
        }
        else if (dir == Vector2.left)
        {
            for (int r = 0; r < 4; r++)
                for (int c = 1; c < 4; c++)
                    TryMove(r, c, 0, -1);
        }
        else if (dir == Vector2.right)
        {
            for (int r = 0; r < 4; r++)
                for (int c = 2; c >= 0; c--)
                    TryMove(r, c, 0, 1);
        }

        CheckWinCondition();
    }

    void TryMove(int r, int c, int dr, int dc)
    {
        GameObject obj = grid[r, c];
        if (obj == null || obj.CompareTag("block") || obj.CompareTag("empty")) return;

        int newR = r + dr;
        int newC = c + dc;
        if (newR < 0 || newR >= 4 || newC < 0 || newC >= 4) return;

        GameObject target = grid[newR, newC];
        if (target != null && target.CompareTag("empty"))
        {
            grid[newR, newC] = obj;
            grid[r, c] = target;

            obj.transform.SetSiblingIndex(newR * 4 + newC);
            target.transform.SetSiblingIndex(r * 4 + c);

            Cell objCell = obj.GetComponent<Cell>();
            Cell targetCell = target.GetComponent<Cell>();

            if (objCell != null) { objCell.row = newR; objCell.col = newC; }
            if (targetCell != null) { targetCell.row = r; targetCell.col = c; }
        }
    }

    void CheckWinCondition()
    {
        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                GameObject a = grid[r, c];
                GameObject b = grid[r, c + 1];
                GameObject c1 = grid[r + 1, c];
                GameObject d = grid[r + 1, c + 1];

                if (a == null || b == null || c1 == null || d == null) continue;

                string n1 = a.name.ToLower();
                string n2 = b.name.ToLower();
                string n3 = c1.name.ToLower();
                string n4 = d.name.ToLower();

                if (n1.Contains("orange3") && n2.Contains("orange4") &&
                    n3.Contains("orange1") && n4.Contains("orange2"))
                {
                    if (panelWin != null) panelWin.SetActive(true);
                    if (UIGame != null) UIGame.SetActive(false);
                    isGameOver = true;
                    justWon = true;

                    if (currentLevel + 1 >= maxUnlockedLevel && currentLevel + 1 < levels.Length)
                    {
                        maxUnlockedLevel = currentLevel + 2;
                        PlayerPrefs.SetInt("UnlockedLevel", maxUnlockedLevel);
                        PlayerPrefs.Save();
                    }
                    return;
                }
            }
        }
    }

    public void ReturnHome()
    {
        if (panelWin != null) panelWin.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
        if (UIGame != null) UIGame.SetActive(false);
        if (selectLevelPanel != null) selectLevelPanel.SetActive(true);

        if (justWon)
        {
            maxUnlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 1);
            justWon = false;
        }

        UnlockLevels();
    }
}