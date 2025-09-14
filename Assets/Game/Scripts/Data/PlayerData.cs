using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class PlayerData
{
    // login info
    public string playerAccessToken;
    public string playerRefreshToken;
    public int playerId;
    public string playerName;

    // player stats
    public List<int> levelStars;
    public float totalTime;

    // sounds
    public float musicVolume;
    public float sfxVolume;

    private static string fileName = "player_data.2r";

    public PlayerData()
    {
        playerAccessToken = "";
        playerRefreshToken = "";
        playerId = 0;
        playerName = "";

        levelStars = new List<int>();
        totalTime = 0f;

        musicVolume = 1f;
        sfxVolume = 1f;
    }

    public static PlayerData LoadData()
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        return PersistentDataController.LoadData<PlayerData>(path);
    }

    public bool SaveData()
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);
        return PersistentDataController.SaveData(this, path);
    }

    public static bool SaveData(byte[] data)
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        try
        {
            File.WriteAllBytes(filePath, data);

            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }

    public static byte[] ReadData()
    {
        string filePath = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(filePath))
        {
            return File.ReadAllBytes(filePath);
        }
        else
        {
            Debug.LogError("File not found: " + filePath);
            return null;
        }
    }

    public void SetPlayerDataFromServer(PlayerData playerData)
    {
        levelStars = playerData.levelStars;
        totalTime = playerData.totalTime;

        musicVolume = playerData.musicVolume;
        sfxVolume = playerData.sfxVolume;
    }
}
