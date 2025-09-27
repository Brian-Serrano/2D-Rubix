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

    public static string GetPath()
    {
        return Path.Combine(Application.persistentDataPath, "player_data.2r");
    }

    public static PlayerData LoadData()
    {
        return PersistentDataController.LoadData<PlayerData>(GetPath());
    }

    public bool SaveData()
    {
        return PersistentDataController.SaveData(this, GetPath());
    }

    public static bool SaveData(byte[] data)
    {
        try
        {
            File.WriteAllBytes(GetPath(), data);

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
        string path = GetPath();

        if (File.Exists(path))
        {
            return File.ReadAllBytes(path);
        }
        else
        {
            Debug.LogError("File not found: " + path);
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
