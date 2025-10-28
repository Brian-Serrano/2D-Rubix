using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LeaderboardManager : MonoBehaviour
{
    [Header("Buttons UI")]
    public Image top50ButtonImg;
    public Image myPositionButtonImg;

    [Header("Leaderboard UI")]
    public GameObject leaderboardItemPrefab;
    public Transform leaderboardContainer;

    [Header("Others")]
    public GameObject spinner;
    public GameObject leaderboardScrollView;
    public TMP_Text errorTxt;
    public Animator crossfade;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Audio Source")]
    public AudioSource buttonClickSfx;

    private PlayerData playerData;
    private TDRubixHTTPClient client;
    private bool selectedAroundTab = false;
    private bool leaderboardSaved = false;

    private Dictionary<bool, LeaderboardResponse> cache;

    private Color gray = new Color(0.45f, 0.45f, 0.45f, 1f);

    private void Awake()
    {
        playerData = PlayerData.LoadData();
        client = TDRubixHTTPClient.GetInstance();
        cache = new Dictionary<bool, LeaderboardResponse>();

        BannerAdManager.GetInstance().EnsureBannerVisible();

        SelectAroundTab();

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            spinner.SetActive(true);
            leaderboardScrollView.SetActive(false);
            errorTxt.gameObject.SetActive(false);

            if (JwtHelper.IsExpired(playerData.playerAccessToken))
            {
                RefreshToken refreshToken = new RefreshToken(playerData.playerRefreshToken);

                client.GetAuthorizationRoutes().Refresh(refreshToken, response =>
                {
                    playerData.playerAccessToken = response.accessToken;
                    playerData.playerRefreshToken = response.refreshToken;

                    playerData.SaveData();

                    Debug.Log("New access token issued");

                    SaveLeaderboard();
                }, error =>
                {
                    spinner.SetActive(false);
                    errorTxt.gameObject.SetActive(true);

                    Debug.Log(error.error);
                    Debug.Log(error.details);

                    errorTxt.text = error.details.Truncate(60);
                });
            }
            else
            {
                SaveLeaderboard();
            }
        }
        else
        {
            spinner.SetActive(false);
            errorTxt.gameObject.SetActive(true);
            leaderboardScrollView.SetActive(false);

            errorTxt.text = "No Internet Connection";
        }
    }

    private void Start()
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(playerData.musicVolume) * 20);
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(playerData.sfxVolume) * 20);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Back();
        }
    }

    private void GetLeaderboardData()
    {
        if (leaderboardContainer.childCount <= 0)
        {
            for (int i = 0; i < 50; i++)
            {
                Instantiate(leaderboardItemPrefab, leaderboardContainer);
            }
        }

        string around = selectedAroundTab ? "true" : "false";

        var key = selectedAroundTab;

        if (!cache.ContainsKey(key))
        {
            GetLeaderboardDataFromServer(around, data =>
            {
                cache[key] = data;
                UpdateLeaderboardEntry(data);
            });
        }
        else
        {
            UpdateLeaderboardEntry(cache[key]);
        }
    }

    private void UpdateLeaderboardEntry(LeaderboardResponse leaderboard)
    {
        for (int i = 0; i < leaderboardContainer.childCount; i++)
        {
            Transform row = leaderboardContainer.GetChild(i);

            TMP_Text rankTxt = row.GetChild(0).GetComponent<TMP_Text>();
            TMP_Text playerNameTxt = row.GetChild(1).GetComponent<TMP_Text>();
            TMP_Text amountTxt = row.GetChild(2).GetComponent<TMP_Text>();

            if (leaderboard.leaderboard.Count > i)
            {
                if (leaderboard.leaderboard[i].playerId == playerData.playerId)
                {
                    row.GetComponent<Image>().color = Color.orangeRed;

                    rankTxt.fontStyle = FontStyles.Bold;
                    playerNameTxt.fontStyle = FontStyles.Bold;
                    amountTxt.fontStyle = FontStyles.Bold;

                    rankTxt.text = leaderboard.leaderboard[i].rank.ToString();
                    playerNameTxt.text = leaderboard.leaderboard[i].playerName;
                    amountTxt.text = leaderboard.leaderboard[i].level.ToString();
                }
                else
                {
                    row.GetComponent<Image>().color = Color.orange;

                    rankTxt.fontStyle = FontStyles.Normal;
                    playerNameTxt.fontStyle = FontStyles.Normal;
                    amountTxt.fontStyle = FontStyles.Normal;

                    rankTxt.text = leaderboard.leaderboard[i].rank.ToString();
                    playerNameTxt.text = leaderboard.leaderboard[i].playerName;
                    amountTxt.text = leaderboard.leaderboard[i].level.ToString();
                }
            }
            else
            {
                row.GetComponent<Image>().color = Color.orange;

                rankTxt.fontStyle = FontStyles.Normal;
                playerNameTxt.fontStyle = FontStyles.Normal;
                amountTxt.fontStyle = FontStyles.Normal;

                rankTxt.text = "";
                playerNameTxt.text = "";
                amountTxt.text = "";
            }
        }
    }

    private void SaveLeaderboard()
    {
        LeaderboardRequest request = new LeaderboardRequest(playerData.levelStars.Count);

        client.GetPlayerRoutes().SaveLeaderboardData(playerData.playerAccessToken, request, response =>
        {
            spinner.SetActive(false);
            leaderboardScrollView.SetActive(true);
            leaderboardSaved = true;

            GetLeaderboardData();
        }, error =>
        {
            spinner.SetActive(false);
            errorTxt.gameObject.SetActive(true);

            errorTxt.text = error.details.Truncate(60);
        });
    }

    private void GetLeaderboardDataFromServer(string around, Action<LeaderboardResponse> data)
    {
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            spinner.SetActive(true);
            leaderboardScrollView.SetActive(false);
            errorTxt.gameObject.SetActive(false);

            if (JwtHelper.IsExpired(playerData.playerAccessToken))
            {
                RefreshToken refreshToken = new RefreshToken(playerData.playerRefreshToken);

                client.GetAuthorizationRoutes().Refresh(refreshToken, response =>
                {
                    playerData.playerAccessToken = response.accessToken;
                    playerData.playerRefreshToken = response.refreshToken;

                    playerData.SaveData();

                    Debug.Log("New access token issued");

                    GetLeaderboard();
                }, error =>
                {
                    spinner.SetActive(false);
                    errorTxt.gameObject.SetActive(true);

                    errorTxt.text = error.details.Truncate(60);
                });
            }
            else
            {
                GetLeaderboard();
            }

            void GetLeaderboard()
            {
                client.GetPlayerRoutes().GetLeaderboard(playerData.playerAccessToken, $"?around={around}", response =>
                {
                    spinner.SetActive(false);
                    leaderboardScrollView.SetActive(true);

                    data?.Invoke(response);
                }, error =>
                {
                    spinner.SetActive(false);
                    errorTxt.gameObject.SetActive(true);

                    errorTxt.text = error.details.Truncate(60);
                });
            }
        }
        else
        {
            spinner.SetActive(false);
            errorTxt.gameObject.SetActive(true);
            leaderboardScrollView.SetActive(false);

            errorTxt.text = "No Internet Connection";
        }
    }

    private void CheckLeaderboard()
    {
        if (leaderboardSaved)
        {
            GetLeaderboardData();
        }
        else
        {
            SaveLeaderboard();
        }
    }

    public void SwitchToTop50Tab()
    {
        selectedAroundTab = false;
        buttonClickSfx.Play();

        SelectAroundTab();
        CheckLeaderboard();
    }

    public void SwitchToMyPositionTab()
    {
        selectedAroundTab = true;
        buttonClickSfx.Play();

        SelectAroundTab();
        CheckLeaderboard();
    }

    private void SelectAroundTab()
    {
        if (selectedAroundTab)
        {
            top50ButtonImg.color = Color.white;
            myPositionButtonImg.color = gray;
        }
        else
        {
            top50ButtonImg.color = gray;
            myPositionButtonImg.color = Color.white;
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
