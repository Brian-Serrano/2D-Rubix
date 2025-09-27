using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Event System")]
    public GraphicRaycaster raycaster;
    public EventSystem eventSystem;

    [Header("Game UI")]
    public GameObject blockPrefab;
    public Transform blocksParent;
    public GameObject goalBlockPrefab;
    public Transform goalBlocksParent;
    public TMP_Text timerTxt;
    public TMP_Text levelTxt;
    public float minSwipeDistance = 30f;

    [Header("Panel UI")]
    public Transform pausePanel;
    public Transform winPanel;
    public TMP_Text winTimeTxt;
    public Animator[] starsAnim;
    public Transform losePanel;
    public Slider musicSlider;
    public Slider sfxSlider;
    public Animator crossfade;
    public Transform noAdsPanel;
    public Button add30SecButton;

    [Header("Audio Source")]
    public AudioSource backgroundMusic;
    public AudioSource buttonClickSfx;
    public AudioSource blockSlideSfx;
    public AudioSource blockRotateSfx;
    public AudioSource starSfx;
    public AudioSource loseSfx;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    private Vector2Int block = new Vector2Int(-1, -1);
    private Vector2 startTouchPos;
    private Vector2 endTouchPos;

    private int tableSize;
    private float timer;
    private float totalTimer;
    private float twoStarTime;
    private float oneStarTime;
    private List<Color> colors;
    private int activeAnimations = 0;
    private Queue<IInputQueue> inputBuffer;
    private List<List<Color>> goalTable;
    private bool winShouldCalled = false;
    private GameState gameState;
    private int level;
    private int add30SecChances = 2;
    private int add30SecDone = 0;

    private float cellSizeX;
    private float cellSizeY;
    private float cellSizeOffset;

    private PlayerData playerData;
    private InterstitialAdManager interstitialAdManager;
    private RewardedAdManager rewardedAdManager;

    interface IInputQueue { }

    class SlideQueue : IInputQueue
    {
        public Vector2Int block;
        public Vector2 startTouchPos;
        public Vector2 endTouchPos;

        public SlideQueue(Vector2Int block, Vector2 startTouchPos, Vector2 endTouchPos)
        {
            this.block = block;
            this.startTouchPos = startTouchPos;
            this.endTouchPos = endTouchPos;
        }
    }

    class RotateQueue : IInputQueue
    {
        public int direction;

        public RotateQueue(int direction)
        {
            this.direction = direction;
        }
    }

    enum GameState
    {
        PLAYING, PAUSE, LOSE, WIN
    }

    private void Awake()
    {
        playerData = PlayerData.LoadData();
        level = NavigatorController.GetArguments<int>("Game");
        interstitialAdManager = InterstitialAdManager.GetInstance();
        rewardedAdManager = RewardedAdManager.GetInstance();

        inputBuffer = new Queue<IInputQueue>();
        goalTable = new List<List<Color>>();
        gameState = GameState.PLAYING;

        if (level >= 1 && level <= 5)
        {
            tableSize = 4;
        }
        else if (level >= 6 && level <= 20)
        {
            tableSize = 5;
        }
        else if (level >= 21 && level <= 50)
        {
            tableSize = 6;
        }
        else if (level >= 51 && level <= 100)
        {
            tableSize = 7;
        }
        else if (level >= 101 && level <= 200)
        {
            tableSize = 8;
        }
        else if (level >= 201 && level <= 500)
        {
            tableSize = 9;
        }
        else if (level >= 501)
        {
            tableSize = 10;
        }

        levelTxt.text = "LEVEL: " + level;

        totalTimer = (tableSize - 3) * 75;
        timer = (tableSize - 3) * 75;
        twoStarTime = totalTimer / 3;
        oneStarTime = totalTimer / 6;

        UnityEngine.Random.InitState(level);

        colors = new List<Color>()
        {
            Color.blue, Color.yellow, Color.red, Color.green, Color.violet, Color.orange, Color.cyan, Color.brown, Color.magenta, Color.indigo
        };

        RectTransform parentRect = blocksParent.GetComponent<RectTransform>();

        cellSizeX = parentRect.rect.size.x / tableSize;
        cellSizeY = parentRect.rect.size.y / tableSize;

        cellSizeOffset = cellSizeX * 0.06f;

        GenerateTable();
        GenerateGoalTable();
    }

    private void GenerateTable()
    {
        List<int> items = new List<int>();

        for (int i = 0; i < tableSize; i++)
        {
            for (int j = 0; j < tableSize; j++)
            {
                items.Add(j);
            }
        }

        List<int> shuffled = Shuffle(items);

        for (int i = 0; i < shuffled.Count; i++)
        {
            GameObject instance = Instantiate(blockPrefab, blocksParent);
            instance.GetComponent<Image>().color = colors[shuffled[i]];

            int x = i % tableSize;
            int y = Mathf.FloorToInt(i / tableSize);

            RectTransform rect = instance.GetComponent<RectTransform>();

            rect.sizeDelta = new Vector3(cellSizeX + cellSizeOffset, cellSizeY + cellSizeOffset);

            BlockInfo info = instance.GetComponent<BlockInfo>();

            StartCoroutine(SetXAndY(new Vector2Int(x, y), rect, info));
        }
    }

    private void Start()
    {
        UpdateMusicVolume();
        UpdateSfxVolume();
    }

    private void GenerateGoalTable()
    {
        RectTransform parentRect = goalBlocksParent.GetComponent<RectTransform>();

        float goalCellSizeX = parentRect.rect.size.x / tableSize;
        float goalCellSizeY = parentRect.rect.size.y / tableSize;

        float goalCellSizeOffset = goalCellSizeX * 0.06f;

        List<int> items = new List<int>();

        for (int i = 0; i < tableSize; i++)
        {
            items.Add(i);
        }

        List<int> shuffled = Shuffle(items);

        for (int i = 0; i < shuffled.Count; i++)
        {
            List<Color> goalTableRow = new List<Color>();

            for (int j = 0; j < shuffled.Count; j++)
            {
                GameObject instance = Instantiate(goalBlockPrefab, goalBlocksParent);
                instance.GetComponent<Image>().color = colors[shuffled[i]];
                goalTableRow.Add(colors[shuffled[i]]);

                int x = j;
                int y = i;

                RectTransform rect = instance.GetComponent<RectTransform>();

                rect.sizeDelta = new Vector3(goalCellSizeX + goalCellSizeOffset, goalCellSizeY + goalCellSizeOffset);

                rect.anchoredPosition = new Vector3((goalCellSizeX * x) - (goalCellSizeOffset / 2), (goalCellSizeY * -y) + (goalCellSizeOffset / 2), 0f);
            }

            goalTable.Add(goalTableRow);
        }
    }

    private bool CheckWin()
    {
        for (int i = 0; i < goalTable.Count; i++)
        {
            for (int j = 0; j < goalTable[i].Count; j++)
            {
                BlockInfo info = GetBlock(j, i);

                if (goalTable[i][j] != info.GetComponent<Image>().color)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private IEnumerator SetXAndY(Vector2Int newValue, RectTransform rect, BlockInfo info, bool isDestroy = false)
    {
        yield return ShiftBlock(newValue, rect);

        info.SetX(newValue.x);
        info.SetY(newValue.y);

        if (isDestroy)
        {
            Destroy(info.gameObject);
        }
    }

    private List<int> Shuffle(List<int> array)
    {
        int n = array.Count;

        for (int i = n - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);

            (array[j], array[i]) = (array[i], array[j]);
        }

        return array;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (noAdsPanel.gameObject.activeSelf)
            {
                CloseNoAdsPanel();
            }
            else
            {
                switch (gameState)
                {
                    case GameState.PLAYING:
                        Pause();
                        break;
                    case GameState.PAUSE:
                        Home();
                        break;
                    case GameState.LOSE:
                        Home();
                        break;
                    case GameState.WIN:
                        Home();
                        break;
                }
            }
        }

        if (gameState == GameState.PLAYING)
        {
            timer -= Time.deltaTime;
            timerTxt.text = TimeSpan.FromSeconds(Mathf.RoundToInt(timer)).ToString(@"mm\:ss");

            if (timer <= 0f)
            {
                gameState = GameState.LOSE;
                Time.timeScale = 0f;

                playerData.totalTime += totalTimer;

                playerData.SaveData();

                backgroundMusic.Stop();

                interstitialAdManager.ShowInterstitial(() =>
                {
                    StartCoroutine(SetPauseAfterAd());
                    StartCoroutine(Lose());
                });
            }

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        startTouchPos = touch.position;

                        DetectItem(touch.position);

                        break;

                    case TouchPhase.Ended:
                        endTouchPos = touch.position;

                        if (block != new Vector2Int(-1, -1))
                        {
                            inputBuffer.Enqueue(new SlideQueue(block, startTouchPos, endTouchPos));
                        }

                        break;
                }
            }
        }

        while (activeAnimations == 0 && inputBuffer.Count > 0)
        {
            IInputQueue queue = inputBuffer.Dequeue();

            if (queue is SlideQueue slide)
            {
                DetectSwipe(slide.block, slide.startTouchPos, slide.endTouchPos);
            }
            if (queue is RotateQueue rotate)
            {
                blockRotateSfx.Play();

                if (rotate.direction < 0)
                {
                    StartCoroutine(RotateClockwiseDelayed());
                }
                else
                {
                    StartCoroutine(RotateCounterClockwiseDelayed());
                }
            }
        }

        if (activeAnimations == 0 && winShouldCalled)
        {
            if (CheckWin())
            {
                gameState = GameState.WIN;
                Time.timeScale = 0f;

                int stars = 0;

                if (timer >= twoStarTime)
                {
                    stars = 3;
                }
                else if (timer >= oneStarTime)
                {
                    stars = 2;
                }
                else if (timer >= 0)
                {
                    stars = 1;
                }

                if (level > playerData.levelStars.Count)
                {
                    playerData.levelStars.Add(stars);
                }
                else
                {
                    playerData.levelStars[level - 1] = Mathf.Max(stars, playerData.levelStars[level - 1]);
                }

                playerData.totalTime += (totalTimer - timer);

                playerData.SaveData();

                backgroundMusic.Stop();

                interstitialAdManager.ShowInterstitial(() =>
                {
                    StartCoroutine(SetPauseAfterAd());
                    StartCoroutine(Win(stars));
                });
            }

            winShouldCalled = false;
        }
    }

    private IEnumerator Win(int stars)
    {
        yield return new WaitForSecondsRealtime(0.3f);

        winPanel.gameObject.SetActive(true);
        winPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);

        winTimeTxt.text = TimeSpan.FromSeconds(Mathf.RoundToInt(totalTimer - timer)).ToString(@"mm\:ss");

        StartCoroutine(SetStars(stars));
    }

    private IEnumerator Lose()
    {
        loseSfx.Play();

        yield return new WaitForSecondsRealtime(0.3f);

        losePanel.gameObject.SetActive(true);
        losePanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
    }

    private IEnumerator SetStars(int stars)
    {
        yield return new WaitForSecondsRealtime(0.2f);

        for (int i = 0; i < stars; i++)
        {
            starsAnim[i].gameObject.SetActive(true);
            starsAnim[i].SetBool("scale", true);
            starSfx.Play();

            yield return new WaitForSecondsRealtime(0.4f);
        }
    }

    private void DetectItem(Vector2 screenPos)
    {
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = screenPos
        };

        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(pointerData, results);

        foreach (RaycastResult result in results)
        {
            BlockInfo blockInfo = result.gameObject.GetComponent<BlockInfo>();

            if (blockInfo != null)
            {
                block = new Vector2Int(blockInfo.x, blockInfo.y);
                return;
            }
        }

        block = new Vector2Int(-1, -1);
    }

    private void DetectSwipe(Vector2Int block, Vector2 startTouchPos, Vector2 endTouchPos)
    {
        if (Vector2.Distance(startTouchPos, endTouchPos) >= minSwipeDistance)
        {
            blockSlideSfx.Play();

            Vector2 direction = endTouchPos - startTouchPos;
            Vector2 absDir = new Vector2(Mathf.Abs(direction.x), Mathf.Abs(direction.y));

            if (absDir.x > absDir.y) // Horizontal swipe
            {
                if (direction.x > 0)
                {
                    HorizontalShift(1, block);
                }
                else
                {
                    HorizontalShift(-1, block);
                }
            }
            else // Vertical swipe
            {
                if (direction.y > 0)
                {
                    VerticalShift(-1, block);
                }
                else
                {
                    VerticalShift(1, block);
                }
            }
        }
    }

    private void HorizontalShift(int dir, Vector2Int block)
    {
        List<BlockInfo> infos = new List<BlockInfo>();

        for (int i = 0; i < tableSize; i++)
        {
            infos.Add(GetBlock(i, block.y));
        }

        foreach (BlockInfo info in infos)
        {
            if (dir > 0)
            {
                if (info.x == tableSize - 1)
                {
                    HorizontalMoveToSide(info, dir);
                }
                else
                {
                    Vector2Int newVal = new Vector2Int(PositiveMod(info.x + dir, tableSize), info.y);

                    StartCoroutine(SetX(newVal, info.GetComponent<RectTransform>(), info));
                }
            }
            else
            {
                if (info.x == 0)
                {
                    HorizontalMoveToSide(info, dir);
                }
                else
                {
                    Vector2Int newVal = new Vector2Int(PositiveMod(info.x + dir, tableSize), info.y);

                    StartCoroutine(SetX(newVal, info.GetComponent<RectTransform>(), info));
                }
            }
        }
    }

    private void HorizontalMoveToSide(BlockInfo info, int dir)
    {
        GameObject copy = Instantiate(info.gameObject, blocksParent);

        RectTransform rect = info.GetComponent<RectTransform>();

        BlockInfo copyInfo = copy.GetComponent<BlockInfo>();
        RectTransform copyRect = copyInfo.GetComponent<RectTransform>();

        copyInfo.SetX(info.x);

        UpdateBlock(copyInfo, copyRect);

        info.SetX(info.x - (tableSize * dir));

        UpdateBlock(info, rect);

        StartCoroutine(SetX(new Vector2Int(copyInfo.x + dir, copyInfo.y), copyRect, copyInfo, true));

        StartCoroutine(SetX(new Vector2Int(info.x + dir, info.y), rect, info));
    }

    private IEnumerator SetX(Vector2Int newValue, RectTransform rect, BlockInfo info, bool isDestroy = false)
    {
        yield return ShiftBlock(newValue, rect);

        info.SetX(newValue.x);

        if (isDestroy)
        {
            Destroy(info.gameObject);
        }
    }

    private void VerticalShift(int dir, Vector2Int block)
    {
        List<BlockInfo> infos = new List<BlockInfo>();

        for (int i = 0; i < tableSize; i++)
        {
            infos.Add(GetBlock(block.x, i));
        }

        foreach (BlockInfo info in infos)
        {
            if (dir > 0)
            {
                if (info.y == tableSize - 1)
                {
                    VerticalMoveToSide(info, dir);
                }
                else
                {
                    Vector2Int newVal = new Vector2Int(info.x, PositiveMod(info.y + dir, tableSize));

                    StartCoroutine(SetY(newVal, info.GetComponent<RectTransform>(), info));
                }
            }
            else
            {
                if (info.y == 0)
                {
                    VerticalMoveToSide(info, dir);
                }
                else
                {
                    Vector2Int newVal = new Vector2Int(info.x, PositiveMod(info.y + dir, tableSize));

                    StartCoroutine(SetY(newVal, info.GetComponent<RectTransform>(), info));
                }
            }
        }
    }

    private void VerticalMoveToSide(BlockInfo info, int dir)
    {
        GameObject copy = Instantiate(info.gameObject, blocksParent);

        RectTransform rect = info.GetComponent<RectTransform>();

        BlockInfo copyInfo = copy.GetComponent<BlockInfo>();
        RectTransform copyRect = copyInfo.GetComponent<RectTransform>();

        copyInfo.SetY(info.y);

        UpdateBlock(copyInfo, copyRect);

        info.SetY(info.y - (tableSize * dir));

        UpdateBlock(info, rect);

        StartCoroutine(SetY(new Vector2Int(copyInfo.x, copyInfo.y + dir), copyRect, copyInfo, true));

        StartCoroutine(SetY(new Vector2Int(info.x, info.y + dir), rect, info));
    }

    private IEnumerator SetY(Vector2Int newValue, RectTransform rect, BlockInfo info, bool isDestroy = false)
    {
        yield return ShiftBlock(newValue, rect);

        info.SetY(newValue.y);

        if (isDestroy)
        {
            Destroy(info.gameObject);
        }
    }

    public void RotateClockwise()
    {
        inputBuffer.Enqueue(new RotateQueue(-1));
    }

    private IEnumerator RotateClockwiseDelayed()
    {
        yield return RotateBoard(Quaternion.Euler(0, 0, -90));

        List<BlockInfo> infos = new List<BlockInfo>();

        for (int y = 0; y < tableSize; y++)
        {
            for (int x = 0; x < tableSize; x++)
            {
                infos.Add(GetBlock(x, y));
            }
        }

        for (int i = 0; i < infos.Count; i++)
        {
            BlockInfo info = infos[i];

            info.SetX(tableSize - 1 - Mathf.FloorToInt(i / tableSize));
            info.SetY(i % tableSize);

            UpdateBlock(info, info.GetComponent<RectTransform>());
        }
    }

    public void RotateCounterClockwise()
    {
        inputBuffer.Enqueue(new RotateQueue(1));
    }

    private IEnumerator RotateCounterClockwiseDelayed()
    {
        yield return RotateBoard(Quaternion.Euler(0, 0, 90));

        List<BlockInfo> infos = new List<BlockInfo>();

        for (int y = 0; y < tableSize; y++)
        {
            for (int x = 0; x < tableSize; x++)
            {
                infos.Add(GetBlock(x, y));
            }
        }

        for (int i = 0; i < infos.Count; i++)
        {
            BlockInfo info = infos[i];

            info.SetY(tableSize - 1 - (i % tableSize));
            info.SetX(Mathf.FloorToInt(i / tableSize));

            UpdateBlock(info, info.GetComponent<RectTransform>());
        }
    }

    private int PositiveMod(int value, int modulus)
    {
        return ((value % modulus) + modulus) % modulus;
    }

    private BlockInfo GetBlock(int x, int y)
    {
        for (int i = 0; i < blocksParent.childCount; i++)
        {
            BlockInfo info = blocksParent.GetChild(i).GetComponent<BlockInfo>();

            if (info != null)
            {
                if (info.x == x && info.y == y)
                {
                    return info;
                }
            }
        }

        return null;
    }

    private IEnumerator MoveToTarget(Vector3 targetPos, RectTransform rect, float duration = 0.1f)
    {
        activeAnimations++;

        Vector3 startPos = rect.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            rect.anchoredPosition = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.anchoredPosition = targetPos;

        winShouldCalled = true;

        activeAnimations--;
    }

    private IEnumerator RotateBoard(Quaternion targetRotation, float duration = 0.2f)
    {
        activeAnimations++;

        Quaternion startRotation = blocksParent.rotation;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            blocksParent.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        // this is for animation purposes only, return to the origin after the rotation animation
        blocksParent.rotation = startRotation;

        winShouldCalled = true;

        activeAnimations--;
    }

    private IEnumerator ShiftBlock(Vector2Int info, RectTransform rect)
    {
        yield return MoveToTarget(new Vector3((cellSizeX * info.x) - (cellSizeOffset / 2), (cellSizeY * -info.y) + (cellSizeOffset / 2), 0f), rect);
    }

    private void UpdateBlock(BlockInfo info, RectTransform rect)
    {
        rect.anchoredPosition = new Vector3((cellSizeX * info.x) - (cellSizeOffset / 2), (cellSizeY * -info.y) + (cellSizeOffset / 2), 0f);
    }

    public void Pause()
    {
        Time.timeScale = 0f;

        gameState = GameState.PAUSE;

        backgroundMusic.Pause();

        musicSlider.value = playerData.musicVolume;
        sfxSlider.value = playerData.sfxVolume;

        buttonClickSfx.Play();

        pausePanel.gameObject.SetActive(true);
        pausePanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
    }

    public void Resume()
    {
        pausePanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        buttonClickSfx.Play();
        playerData.SaveData();
        StartCoroutine(ResumeDelayed());
    }

    private IEnumerator ResumeDelayed()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        pausePanel.gameObject.SetActive(false);
        Time.timeScale = 1f;
        gameState = GameState.PLAYING;
        backgroundMusic.UnPause();
    }

    public void Home()
    {
        if (gameState == GameState.PAUSE)
        {
            playerData.totalTime += (totalTimer - timer);
            playerData.SaveData();
        }

        buttonClickSfx.Play();
        StartCoroutine(SwitchScene("Level"));
    }

    public void Retry()
    {
        if (gameState == GameState.PAUSE)
        {
            playerData.totalTime += (totalTimer - timer);
            playerData.SaveData();
        }

        buttonClickSfx.Play();
        StartCoroutine(SwitchScene("Game", level));
    }

    public void Next()
    {
        buttonClickSfx.Play();
        StartCoroutine(SwitchScene("Game", level + 1));
    }

    private void UpdateMusicVolume()
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(playerData.musicVolume) * 20);
    }

    private void UpdateSfxVolume()
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(playerData.sfxVolume) * 20);
    }

    public void OnMusicVolumeChange(float value)
    {
        playerData.musicVolume = value;
        UpdateMusicVolume();
    }

    public void OnSfxVolumeChange(float value)
    {
        playerData.sfxVolume = value;
        UpdateSfxVolume();
    }

    public void Add30Seconds()
    {
        if (add30SecDone < add30SecChances)
        {
            buttonClickSfx.Play();
            backgroundMusic.Pause();
            Time.timeScale = 0f;

            rewardedAdManager.ShowRewardedAd(() => { }, () =>
            {
                Time.timeScale = 1f;
                timer += 30f;
                totalTimer += 30f;
                add30SecDone++;

                if (add30SecDone >= add30SecChances)
                {
                    add30SecButton.interactable = false;
                }
                backgroundMusic.UnPause();
            }, () =>
            {
                StartCoroutine(SetPauseAfterAd());
                OpenNoAdsPanel();
            });
        }
    }

    private void OpenNoAdsPanel()
    {
        noAdsPanel.gameObject.SetActive(true);
        noAdsPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
    }

    public void CloseNoAdsPanel()
    {
        Time.timeScale = 1f;
        buttonClickSfx.Play();
        noAdsPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        StartCoroutine(DelayedPanelClose(noAdsPanel));
        backgroundMusic.UnPause();
    }

    private IEnumerator DelayedPanelClose(Transform panel)
    {
        yield return new WaitForSecondsRealtime(0.2f);
        panel.gameObject.SetActive(false);
    }

    private IEnumerator SetPauseAfterAd()
    {
        yield return null; // Wait 1 frame so SDK finishes its reset
        Time.timeScale = 0f;
    }

    private IEnumerator SwitchScene(string name, object args = null)
    {
        crossfade.GetComponent<CanvasGroup>().blocksRaycasts = true;
        crossfade.SetBool("isOpen", true);
        yield return new WaitForSecondsRealtime(0.3f);
        Time.timeScale = 1f;
        NavigatorController.Navigate(name, args);
    }
}
