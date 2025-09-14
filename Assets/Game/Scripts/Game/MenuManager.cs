using Newtonsoft.Json.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Menu UI")]
    public TMP_Text levelTxt;
    public Animator crossfade;

    [Header("Settings UI")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Transform settingsPanel;

    [Header("Account UI")]
    public Transform accountPanel;
    public Transform loginPanel;
    public Transform signupPanel;
    public TMP_InputField loginUsernameTxt;
    public TMP_InputField loginPasswordTxt;
    public TMP_Text loginErrorTxt;
    public TMP_InputField signupUsernameTxt;
    public TMP_InputField signupEmailTxt;
    public TMP_InputField signupPasswordTxt;
    public TMP_InputField signupConfirmPasswordTxt;
    public TMP_Text signupErrorTxt;

    public Button signupButton;
    public Button loginButton;
    public Button saveButton;
    public Button loadButton;
    public Button logoutButton;
    public Button signupSubmitButton;
    public Button loginSubmitButton;

    public TMP_Text accountErrorTxt;
    public TMP_Text accountLoginText;
    public GameObject spinnerContainer;

    [Header("Confirm Panel UI")]
    public Transform confirmPanel;
    public TMP_Text confirmPanelText;
    public Button confirmPanelOkButton;

    [Header("Achievement Panel UI")]
    public Transform achievementPanel;
    public Transform achievementItemContainer;
    public GameObject achievementItemPrefab;

    public AudioSource buttonClickSfx;

    public AudioMixer audioMixer;

    private PlayerData playerData;
    private TDRubixHTTPClient client;
    private BannerAdManager bannerAdManager;

    class AchievementData
    {
        public string title;
        public string description;
        public float progress;

        public AchievementData(string title, string description, float progress)
        {
            this.title = title;
            this.description = description;
            this.progress = progress;
        }
    }

    private void Awake()
    {
        playerData = PlayerData.LoadData();
        client = TDRubixHTTPClient.GetInstance();
        bannerAdManager = BannerAdManager.GetInstance();

        levelTxt.text = "LEVEL: " + (playerData.levelStars.Count + 1);
    }

    private void Start()
    {
        UpdateMusicVolume();
        UpdateSfxVolume();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!spinnerContainer.activeSelf)
            {
                if (settingsPanel.gameObject.activeSelf)
                {
                    CloseSettingsPanel();
                }
                else if (accountPanel.gameObject.activeSelf && !signupPanel.gameObject.activeSelf && !loginPanel.gameObject.activeSelf && !confirmPanel.gameObject.activeSelf)
                {
                    CloseAccountPanel();
                }
                else if (signupPanel.gameObject.activeSelf)
                {
                    CloseSignupPanel();
                }
                else if (loginPanel.gameObject.activeSelf)
                {
                    CloseLoginPanel();
                }
                else if (achievementPanel.gameObject.activeSelf)
                {
                    CloseAchievementPanel();
                }
                else if (confirmPanel.gameObject.activeSelf)
                {
                    CloseConfirmPanel();
                }
            }
        }
    }

    public void Quit()
    {
        buttonClickSfx.Play();
        Application.Quit();
    }

    public void Play()
    {
        buttonClickSfx.Play();
        StartCoroutine(SwitchScene("Game", playerData.levelStars.Count + 1));
    }

    public void Level()
    {
        buttonClickSfx.Play();
        StartCoroutine(SwitchScene("Level"));
    }

    public void Leaderboard()
    {
        buttonClickSfx.Play();
        StartCoroutine(SwitchScene("Leaderboard"));
    }

    public void OpenSettingsPanel()
    {
        musicSlider.value = playerData.musicVolume;
        sfxSlider.value = playerData.sfxVolume;

        settingsPanel.gameObject.SetActive(true);
        buttonClickSfx.Play();
        settingsPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
    }

    public void CloseSettingsPanel()
    {
        playerData.SaveData();

        settingsPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        buttonClickSfx.Play();
        StartCoroutine(DelayedPanelClose(settingsPanel));
    }

    private IEnumerator DelayedPanelClose(Transform panel)
    {
        yield return new WaitForSecondsRealtime(0.2f);
        panel.gameObject.SetActive(false);
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

    public void Login()
    {
        buttonClickSfx.Play();

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            if (ValidateUsername(loginUsernameTxt.text.Trim(), loginErrorTxt) && ValidatePassword(loginPasswordTxt.text.Trim(), loginErrorTxt))
            {
                LoginRequest request = new LoginRequest(loginUsernameTxt.text.Trim(), loginPasswordTxt.text.Trim());

                spinnerContainer.SetActive(true);

                client.GetAuthorizationRoutes().Login(request, response =>
                {
                    playerData.playerAccessToken = response.accessToken;
                    playerData.playerRefreshToken = response.refreshToken;
                    playerData.playerId = response.playerId;
                    playerData.playerName = loginUsernameTxt.text.Trim();

                    playerData.SaveData();

                    ClearLoginInputFields();

                    CheckLoginState();

                    loginErrorTxt.text = "Successfully logged in";
                    loginErrorTxt.color = Color.green;

                    spinnerContainer.SetActive(false);
                }, error =>
                {
                    loginErrorTxt.text = error.details.Truncate(60);
                    loginErrorTxt.color = Color.red;

                    spinnerContainer.SetActive(false);
                });
            }
        }
        else
        {
            loginErrorTxt.text = "No Internet Connection";
            loginErrorTxt.color = Color.red;
        }
    }

    public void Signup()
    {
        buttonClickSfx.Play();

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            bool nameValidate = ValidateUsername(signupUsernameTxt.text.Trim(), signupErrorTxt);
            bool emailValidate = ValidateEmail(signupEmailTxt.text.Trim(), signupErrorTxt);
            bool passwordValidate = ValidatePassword(signupPasswordTxt.text.Trim(), signupErrorTxt);
            bool passwordMatch = PasswordMatch(signupPasswordTxt.text.Trim(), signupConfirmPasswordTxt.text.Trim(), signupErrorTxt);

            if (nameValidate && emailValidate && passwordValidate && passwordMatch)
            {
                SignupRequest request = new SignupRequest(signupUsernameTxt.text.Trim(), signupEmailTxt.text.Trim(), signupPasswordTxt.text.Trim(), signupConfirmPasswordTxt.text.Trim());

                spinnerContainer.SetActive(true);

                client.GetAuthorizationRoutes().Signup(request, response =>
                {
                    playerData.playerAccessToken = response.accessToken;
                    playerData.playerRefreshToken = response.refreshToken;
                    playerData.playerId = response.playerId;
                    playerData.playerName = signupUsernameTxt.text.Trim();

                    playerData.SaveData();

                    ClearSignupInputFields();

                    CheckLoginState();

                    signupErrorTxt.text = "Successfully signed up";
                    signupErrorTxt.color = Color.green;

                    spinnerContainer.SetActive(false);
                }, error =>
                {
                    signupErrorTxt.text = error.details.Truncate(60);
                    signupErrorTxt.color = Color.red;

                    spinnerContainer.SetActive(false);
                });
            }
        }
        else
        {
            signupErrorTxt.text = "No Internet Connection";
            signupErrorTxt.color = Color.red;
        }
    }

    private bool ValidateUsername(string username, TMP_Text text)
    {
        if (username.Length > 20 || username.Length < 8)
        {
            text.text = "Username should be 8 to 20 characters";
            text.color = Color.red;
            return false;
        }
        if (username.Any(u => !char.IsLetterOrDigit(u)))
        {
            text.text = "Username should only contain alphanumeric characters";
            text.color = Color.red;
            return false;
        }

        return true;
    }

    private bool ValidatePassword(string password, TMP_Text text)
    {
        if (password.Length > 20 || password.Length < 8)
        {
            text.text = "Password should be 8 to 20 characters";
            text.color = Color.red;
            return false;
        }
        if (password.Any(u => !char.IsLetterOrDigit(u)))
        {
            text.text = "Password should only contain alphanumeric characters";
            text.color = Color.red;
            return false;
        }

        return true;
    }

    private bool ValidateEmail(string email, TMP_Text text)
    {
        try
        {
            MailAddress address = new MailAddress(email);

            if (email.Length > 100 || email.Length < 15)
            {
                text.text = "Email should be 15 to 100 characters";
                text.color = Color.red;
                return false;
            }
            if (address.Address != email)
            {
                text.text = "Invalid email";
                text.color = Color.red;
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            text.text = "Invalid email";
            text.color = Color.red;
            return false;
        }
    }

    private bool PasswordMatch(string password, string confirmPassword, TMP_Text text)
    {
        if (password != confirmPassword)
        {
            text.text = "Passwords do not match";
            text.color = Color.red;
            return false;
        }

        return true;
    }

    public void OpenAccountPanel()
    {
        CheckLoginState();
        accountErrorTxt.text = "";
        accountPanel.gameObject.SetActive(true);
        accountPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
        buttonClickSfx.Play();
    }

    public void CloseAccountPanel()
    {
        accountPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        buttonClickSfx.Play();
        StartCoroutine(DelayedPanelClose(accountPanel));
    }

    public void OpenSignupPanel()
    {
        signupPanel.gameObject.SetActive(true);
        signupErrorTxt.text = "";
        signupPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
        buttonClickSfx.Play();

        ClearSignupInputFields();
    }

    public void OpenLoginPanel()
    {
        loginPanel.gameObject.SetActive(true);
        loginErrorTxt.text = "";
        loginPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
        buttonClickSfx.Play();

        ClearLoginInputFields();
    }

    public void CloseSignupPanel()
    {
        signupPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        buttonClickSfx.Play();
        StartCoroutine(DelayedPanelClose(signupPanel));
    }

    public void CloseLoginPanel()
    {
        loginPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        buttonClickSfx.Play();
        StartCoroutine(DelayedPanelClose(loginPanel));
    }

    private void Logout()
    {
        CloseConfirmPanel();

        playerData.playerAccessToken = "";
        playerData.playerRefreshToken = "";
        playerData.playerId = 0;
        playerData.playerName = "";

        playerData.SaveData();

        CheckLoginState();
    }

    private void SaveDataToServer()
    {
        CloseConfirmPanel();

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            saveButton.interactable = false;
            loadButton.interactable = false;

            spinnerContainer.SetActive(true);

            if (JwtHelper.IsExpired(playerData.playerAccessToken))
            {
                RefreshToken refreshToken = new RefreshToken(playerData.playerRefreshToken);

                client.GetAuthorizationRoutes().Refresh(refreshToken, response =>
                {
                    playerData.playerAccessToken = response.accessToken;
                    playerData.playerRefreshToken = response.refreshToken;

                    playerData.SaveData();

                    Debug.Log("New access token has been issued");

                    SavePlayerData();
                }, error =>
                {
                    accountErrorTxt.text = error.details.Truncate(60);
                    accountErrorTxt.color = Color.red;

                    saveButton.interactable = true;
                    loadButton.interactable = true;

                    spinnerContainer.SetActive(false);
                });
            }
            else
            {
                SavePlayerData();
            }

            void SavePlayerData()
            {
                client.GetPlayerRoutes().SavePlayerData(playerData.playerAccessToken, response =>
                {
                    accountErrorTxt.text = response.message.Truncate(60);
                    accountErrorTxt.color = Color.green;

                    saveButton.interactable = true;
                    loadButton.interactable = true;

                    spinnerContainer.SetActive(false);
                }, error =>
                {
                    accountErrorTxt.text = error.details.Truncate(60);
                    accountErrorTxt.color = Color.red;

                    saveButton.interactable = true;
                    loadButton.interactable = true;

                    spinnerContainer.SetActive(false);
                }, progress => { });
            }
        }
        else
        {
            accountErrorTxt.text = "No Internet Connection";
            accountErrorTxt.color = Color.red;
        }
    }

    private void LoadDataFromServer()
    {
        CloseConfirmPanel();

        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            saveButton.interactable = false;
            loadButton.interactable = false;

            spinnerContainer.SetActive(true);

            if (JwtHelper.IsExpired(playerData.playerAccessToken))
            {
                RefreshToken refreshToken = new RefreshToken(playerData.playerRefreshToken);

                client.GetAuthorizationRoutes().Refresh(refreshToken, response =>
                {
                    playerData.playerAccessToken = response.accessToken;
                    playerData.playerRefreshToken = response.refreshToken;

                    playerData.SaveData();

                    Debug.Log("New access token has been issued");

                    LoadPlayerData();
                }, error =>
                {
                    accountErrorTxt.text = error.details.Truncate(60);
                    accountErrorTxt.color = Color.red;

                    saveButton.interactable = true;
                    loadButton.interactable = true;

                    spinnerContainer.SetActive(false);
                });
            }
            else
            {
                LoadPlayerData();
            }

            void LoadPlayerData()
            {
                client.GetPlayerRoutes().LoadPlayerData(playerData.playerAccessToken, response =>
                {
                    playerData.SetPlayerDataFromServer(PlayerData.LoadData());

                    playerData.SaveData();

                    accountErrorTxt.text = response.message.Truncate(60);
                    accountErrorTxt.color = Color.green;

                    saveButton.interactable = true;
                    loadButton.interactable = true;

                    levelTxt.text = "LEVEL: " + (playerData.levelStars.Count + 1);

                    UpdateMusicVolume();
                    UpdateSfxVolume();

                    spinnerContainer.SetActive(false);
                }, error =>
                {
                    accountErrorTxt.text = error.details.Truncate(60);
                    accountErrorTxt.color = Color.red;

                    saveButton.interactable = true;
                    loadButton.interactable = true;

                    spinnerContainer.SetActive(false);
                }, progress => { });
            }
        }
        else
        {
            accountErrorTxt.text = "No Internet Connection";
            accountErrorTxt.color = Color.red;
        }
    }

    public void ConfirmLogout()
    {
        confirmPanel.gameObject.SetActive(true);
        confirmPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
        buttonClickSfx.Play();

        confirmPanelText.text = "Are you sure, you want to logout? Your data will not be lost.";

        confirmPanelOkButton.onClick.RemoveAllListeners();

        confirmPanelOkButton.onClick.AddListener(Logout);
    }

    public void ConfirmSaveData()
    {
        confirmPanel.gameObject.SetActive(true);
        confirmPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
        buttonClickSfx.Play();

        confirmPanelText.text = "This will save your data to the server and will overwrite the previous data.";

        confirmPanelOkButton.onClick.RemoveAllListeners();

        confirmPanelOkButton.onClick.AddListener(SaveDataToServer);
    }

    public void ConfirmLoadData()
    {
        confirmPanel.gameObject.SetActive(true);
        confirmPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
        buttonClickSfx.Play();

        confirmPanelText.text = "This will load your data from the server and overwrite your current progress.";

        confirmPanelOkButton.onClick.RemoveAllListeners();

        confirmPanelOkButton.onClick.AddListener(LoadDataFromServer);
    }

    public void CloseConfirmPanel()
    {
        confirmPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        buttonClickSfx.Play();
        StartCoroutine(DelayedPanelClose(confirmPanel));
    }

    private void CheckLoginState()
    {
        if (playerData.playerName.Length > 0 && playerData.playerId > 0)
        {
            accountLoginText.text = $"LOGGED IN AS {playerData.playerName}";

            signupSubmitButton.interactable = false;
            loginSubmitButton.interactable = false;
            logoutButton.interactable = true;
            signupButton.interactable = false;
            loginButton.interactable = false;
            saveButton.interactable = true;
            loadButton.interactable = true;
        }
        else
        {
            accountLoginText.text = $"NOT LOGGED IN";

            signupSubmitButton.interactable = true;
            loginSubmitButton.interactable = true;
            logoutButton.interactable = false;
            signupButton.interactable = true;
            loginButton.interactable = true;
            saveButton.interactable = false;
            loadButton.interactable = false;
        }
    }

    private void ClearSignupInputFields()
    {
        signupUsernameTxt.text = "";
        signupEmailTxt.text = "";
        signupPasswordTxt.text = "";
        signupConfirmPasswordTxt.text = "";
    }

    private void ClearLoginInputFields()
    {
        loginUsernameTxt.text = "";
        loginPasswordTxt.text = "";
    }

    public void OpenAchievementPanel()
    {
        achievementPanel.gameObject.SetActive(true);
        achievementPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", true);
        buttonClickSfx.Play();

        if (achievementItemContainer.childCount == 0)
        {
            List<AchievementData> achievements = new List<AchievementData>
            {
                new AchievementData("Rookie Explorer", "Complete 5 levels", playerData.levelStars.Count / 5f),
                new AchievementData("Seasoned Adventurer", "Complete 10 levels", playerData.levelStars.Count / 10f),
                new AchievementData("Pathfinder", "Complete 25 levels", playerData.levelStars.Count / 25f),
                new AchievementData("Skilled Challenger", "Complete 50 levels", playerData.levelStars.Count / 50f),
                new AchievementData("Veteran Player", "Complete 100 levels", playerData.levelStars.Count / 100f),
                new AchievementData("Master Explorer", "Complete 200 levels", playerData.levelStars.Count / 200f),
                new AchievementData("Elite Challenger", "Complete 350 levels", playerData.levelStars.Count / 350f),
                new AchievementData("Legendary Adventurer", "Complete 500 levels", playerData.levelStars.Count / 500f),
                new AchievementData("Mythic Conqueror", "Complete 1000 levels", playerData.levelStars.Count / 1000f),
                new AchievementData("Immortal Champion", "Complete 2000 levels", playerData.levelStars.Count / 2000f),
                new AchievementData("Casual Gamer", "Play for 1 hour", playerData.totalTime / 3600),
                new AchievementData("Warmed Up", "Play for 2 hours", playerData.totalTime / 7200),
                new AchievementData("Hooked", "Play for 5 hours", playerData.totalTime / 18000),
                new AchievementData("Dedicated", "Play for 10 hours", playerData.totalTime / 36000),
                new AchievementData("Marathoner", "Play for 15 hours", playerData.totalTime / 54000),
                new AchievementData("Day One Survivor", "Play for 1 day", playerData.totalTime / 86400),
                new AchievementData("Endurance Gamer", "Play for 2 days", playerData.totalTime / 172800),
                new AchievementData("Relentless", "Play for 3 days", playerData.totalTime / 259200),
                new AchievementData("Never Sleeps", "Play for 5 days", playerData.totalTime / 432000),
                new AchievementData("Time Lord", "Play for 10 days", playerData.totalTime / 864000),
                new AchievementData("Shiny Collector", "Collect 10 stars", playerData.levelStars.Sum() / 10f),
                new AchievementData("Star Gatherer", "Collect 20 stars", playerData.levelStars.Sum() / 20f),
                new AchievementData("Bright Hoarder", "Collect 50 stars", playerData.levelStars.Sum() / 50f),
                new AchievementData("Starlight Keeper", "Collect 100 stars", playerData.levelStars.Sum() / 100f),
                new AchievementData("Celestial Hunter", "Collect 150 stars", playerData.levelStars.Sum() / 150f),
                new AchievementData("Star Chaser", "Collect 250 stars", playerData.levelStars.Sum() / 250f),
                new AchievementData("Galaxy Collector", "Collect 500 stars", playerData.levelStars.Sum() / 500f),
                new AchievementData("Nebula Keeper", "Collect 1000 stars", playerData.levelStars.Sum() / 1000f),
                new AchievementData("Cosmic Collector", "Collect 2000 stars", playerData.levelStars.Sum() / 2000f),
                new AchievementData("Universe Master", "Collect 5000 stars", playerData.levelStars.Sum() / 5000f)
            };

            foreach (AchievementData achievement in achievements)
            {
                Transform instance = Instantiate(achievementItemPrefab, achievementItemContainer).transform;

                instance.GetChild(0).GetComponent<TMP_Text>().text = achievement.title;
                instance.GetChild(1).GetComponent<TMP_Text>().text = achievement.description;
                instance.GetChild(2).GetComponent<Slider>().value = Mathf.Min(1f, achievement.progress);
                instance.GetChild(3).gameObject.SetActive(Mathf.Min(1f, achievement.progress) >= 1f);
            }
        }
    }

    public void CloseAchievementPanel()
    {
        achievementPanel.GetChild(1).GetComponent<Animator>().SetBool("isOpen", false);
        buttonClickSfx.Play();
        StartCoroutine(DelayedPanelClose(achievementPanel));
    }

    private IEnumerator SwitchScene(string name, object args = null)
    {
        crossfade.GetComponent<CanvasGroup>().blocksRaycasts = true;
        crossfade.SetBool("isOpen", true);
        yield return new WaitForSecondsRealtime(0.3f);
        NavigatorController.Navigate(name, args);
    }
}
