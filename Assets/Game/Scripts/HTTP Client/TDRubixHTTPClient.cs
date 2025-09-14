using CI.HttpClient;
using UnityEngine;

public class TDRubixHTTPClient
{
    private static TDRubixHTTPClient instance;

    public HttpClient client;
    public string baseUrl = "https://briser-games-server.onrender.com/";

    public static TDRubixHTTPClient GetInstance()
    {
        instance ??= new TDRubixHTTPClient();

        return instance;
    }

    private TDRubixHTTPClient()
    {
        client = new HttpClient();
    }

    public AuthorizationRoutes GetAuthorizationRoutes()
    {
        return AuthorizationRoutes.GetInstance(this);
    }

    public PlayerRoutes GetPlayerRoutes()
    {
        return PlayerRoutes.GetInstance(this);
    }
}
