using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class LevelManager : MonoBehaviour
{
    [Header("Level UI")]
    public RectTransform content;
    public GameObject itemPrefab;
    public Animator crossfade;

    [Header("Star Sprites")]
    public Sprite star1Sprite;
    public Sprite star2Sprite;

    [Header("Audio Source")]
    public AudioSource buttonClickSfx;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    private List<RectTransform> items;
    private int numOfRows = 5;
    private int xSpacing = 10;
    private int ySpacing = 40;
    private int visibleItems = 60;
    private bool hasInitialized = false;

    private int topIndex = 0;
    private int bottomIndex = -1;

    private PlayerData playerData;

    private void Awake()
    {
        items = new List<RectTransform>();
        playerData = PlayerData.LoadData();
    }

    private void Start()
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(playerData.musicVolume) * 20);
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(playerData.sfxVolume) * 20);
    }

    private void Update()
    {
        // need to do the level generation in update because cannot get the exact size of stretched ui component
        if (hasInitialized)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Back();
            }

            float itemPrefabSize = ((content.rect.size.x - (xSpacing * (numOfRows + 1))) / numOfRows);

            float scrollY = content.anchoredPosition.y;

            if (scrollY > (topIndex + 1) * (itemPrefabSize + ySpacing))
            {
                bottomIndex++;
                topIndex++;

                for (int i = 0; i < numOfRows; i++)
                {
                    var topItem = items[0];
                    items.RemoveAt(0);
                    items.Add(topItem);

                    float newXPos = (itemPrefabSize * i) + (xSpacing * (i + 1));
                    float newYPos = (-bottomIndex * itemPrefabSize) + (ySpacing * -(bottomIndex + 1));

                    topItem.anchoredPosition = new Vector2(newXPos, newYPos);

                    int level = 1 + (bottomIndex * numOfRows) + i;

                    SetItem(topItem, level);
                }
            }

            else if (scrollY < topIndex * (itemPrefabSize + ySpacing))
            {
                bottomIndex--;
                topIndex--;

                for (int i = 0; i < numOfRows; i++)
                {
                    var bottomItem = items[items.Count - 1];
                    items.RemoveAt(items.Count - 1);
                    items.Insert(0, bottomItem);

                    float newXPos = (itemPrefabSize * i) + (xSpacing * (i + 1));
                    float newYPos = (-topIndex * itemPrefabSize) + (ySpacing * -(topIndex + 1));

                    bottomItem.anchoredPosition = new Vector2(newXPos, newYPos);

                    int level = 1 + (topIndex * numOfRows) + i;

                    SetItem(bottomItem, level);
                }
            }

            if (content.anchoredPosition.y >= content.sizeDelta.y - 2000)
            {
                content.sizeDelta = new Vector2(content.sizeDelta.x, content.sizeDelta.y + 10000);
            }
        }
        else
        {
            hasInitialized = true;
            topIndex = playerData.levelStars.Count / numOfRows;

            float itemPrefabSize = ((content.rect.size.x - (xSpacing * (numOfRows + 1))) / numOfRows);

            float contentYPos = (itemPrefabSize * topIndex) + (ySpacing * (topIndex + 1));
            content.sizeDelta = new Vector2(content.sizeDelta.x, (int)(Mathf.Ceil(contentYPos / 10000f) * 10000));
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Max(0, contentYPos - 200));

            for (int i = topIndex * numOfRows; i < visibleItems + (topIndex * numOfRows); i += numOfRows)
            {
                for (int j = 0; j < numOfRows; j++)
                {
                    RectTransform item = Instantiate(itemPrefab, content).GetComponent<RectTransform>();
                    item.GetComponentInChildren<TMP_Text>().text = (i + j + 1).ToString();

                    item.sizeDelta = new Vector2(itemPrefabSize, itemPrefabSize);

                    float xPos = (itemPrefabSize * j) + (xSpacing * (j + 1));
                    float yPos = (itemPrefabSize * -(i / numOfRows)) + (ySpacing * -((i / numOfRows) + 1));

                    item.anchoredPosition = new Vector2(xPos, yPos);

                    SetItem(item, i + j + 1);

                    items.Add(item);
                    bottomIndex = i / numOfRows;
                }
            }
        }
    }

    private void SetItem(RectTransform item, int level)
    {
        item.GetComponentInChildren<TMP_Text>().text = level.ToString();

        if (level <= 0)
        {
            item.gameObject.SetActive(false);
        }
        else
        {
            item.gameObject.SetActive(true);
            if (playerData.levelStars.Count + 1 == level)
            {
                item.GetComponent<Image>().color = Color.white;

                item.GetChild(0).gameObject.SetActive(false);

                Transform star1 = item.GetChild(2);

                star1.gameObject.SetActive(true);
                star1.GetComponent<Image>().sprite = star2Sprite;

                Transform star2 = item.GetChild(3);

                star2.gameObject.SetActive(true);
                star2.GetComponent<Image>().sprite = star2Sprite;

                Transform star3 = item.GetChild(4);

                star3.gameObject.SetActive(true);
                star3.GetComponent<Image>().sprite = star2Sprite;

                Button button = item.GetComponent<Button>();

                button.interactable = true;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    buttonClickSfx.Play();
                    StartCoroutine(SwitchScene("Game", level));
                });
            }
            else if (playerData.levelStars.Count <= level - 1)
            {
                item.GetComponent<Image>().color = Color.gray;

                item.GetChild(0).gameObject.SetActive(true);

                item.GetChild(2).gameObject.SetActive(false);
                item.GetChild(3).gameObject.SetActive(false);
                item.GetChild(4).gameObject.SetActive(false);

                Button button = item.GetComponent<Button>();

                button.interactable = false;
                button.onClick.RemoveAllListeners();
            }
            else
            {
                item.GetComponent<Image>().color = Color.orange;

                item.GetChild(0).gameObject.SetActive(false);

                Transform star1 = item.GetChild(2);

                star1.gameObject.SetActive(true);
                star1.GetComponent<Image>().sprite = playerData.levelStars[level - 1] >= 1 ? star1Sprite : star2Sprite;

                Transform star2 = item.GetChild(3);

                star2.gameObject.SetActive(true);
                star2.GetComponent<Image>().sprite = playerData.levelStars[level - 1] >= 2 ? star1Sprite : star2Sprite;

                Transform star3 = item.GetChild(4);

                star3.gameObject.SetActive(true);
                star3.GetComponent<Image>().sprite = playerData.levelStars[level - 1] >= 3 ? star1Sprite : star2Sprite;

                Button button = item.GetComponent<Button>();

                button.interactable = true;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    buttonClickSfx.Play();
                    StartCoroutine(SwitchScene("Game", level));
                });
            }
        }
    }

    public void Back()
    {
        buttonClickSfx.Play();
        StartCoroutine(SwitchScene("Menu"));
    }

    private IEnumerator SwitchScene(string name, object args = null)
    {
        crossfade.GetComponent<CanvasGroup>().blocksRaycasts = true;
        crossfade.SetBool("isOpen", true);
        yield return new WaitForSecondsRealtime(0.3f);
        NavigatorController.Navigate(name, args);
    }
}
