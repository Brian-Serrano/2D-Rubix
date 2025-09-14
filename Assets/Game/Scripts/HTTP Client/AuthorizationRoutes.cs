using CI.HttpClient;
using System;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class AuthorizationRoutes
{
    private static AuthorizationRoutes instance;
    private TDRubixHTTPClient client;

    public static AuthorizationRoutes GetInstance(TDRubixHTTPClient client)
    {
        instance ??= new AuthorizationRoutes(client);

        return instance;
    }

    private AuthorizationRoutes(TDRubixHTTPClient client)
    {
        this.client = client;
    }

    public void Login(LoginRequest requestData, Action<TokensAndId> responseCallback, Action<ErrorResponse> errorCallback)
    {
        string url = client.baseUrl + "tr_authorization_routes/log_in";

        HttpRequestMessage message = new HttpRequestMessage
        {
            Uri = new Uri(url),
            Method = HttpAction.Post,
            Content = StringContent.FromObject(requestData)
        };

        client.client.Send(message, HttpCompletionOption.AllResponseContent, response => {

            if (response.IsSuccessStatusCode)
            {
                responseCallback?.Invoke(response.ReadAsJson<TokensAndId>());
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                if (!response.HasContent)
                {
                    errorCallback?.Invoke(new ErrorResponse("Server unavailable", "Server not running."));
                    return;
                }

                errorCallback?.Invoke(response.ReadAsJson<ErrorResponse>());
                return;
            }
        });
    }

    public void Signup(SignupRequest requestData, Action<TokensAndId> responseCallback, Action<ErrorResponse> errorCallback)
    {
        string url = client.baseUrl + "tr_authorization_routes/sign_up";

        HttpRequestMessage message = new HttpRequestMessage
        {
            Uri = new Uri(url),
            Method = HttpAction.Post,
            Content = StringContent.FromObject(requestData)
        };

        client.client.Send(message, HttpCompletionOption.AllResponseContent, response => {

            if (response.IsSuccessStatusCode)
            {
                responseCallback?.Invoke(response.ReadAsJson<TokensAndId>());
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                if (!response.HasContent)
                {
                    errorCallback?.Invoke(new ErrorResponse("Server unavailable", "Server not running."));
                    return;
                }

                errorCallback?.Invoke(response.ReadAsJson<ErrorResponse>());
                return;
            }
        });
    }

    public void Refresh(RefreshToken refreshToken, Action<Tokens> responseCallback, Action<ErrorResponse> errorCallback)
    {
        string url = client.baseUrl + "tr_authorization_routes/refresh";

        HttpRequestMessage message = new HttpRequestMessage
        {
            Uri = new Uri(url),
            Method = HttpAction.Post,
            Content = StringContent.FromObject(refreshToken)
        };

        client.client.Send(message, HttpCompletionOption.AllResponseContent, response => {

            if (response.IsSuccessStatusCode)
            {
                responseCallback?.Invoke(response.ReadAsJson<Tokens>());
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                if (!response.HasContent)
                {
                    errorCallback?.Invoke(new ErrorResponse("Server unavailable", "Server not running."));
                    return;
                }

                errorCallback?.Invoke(response.ReadAsJson<ErrorResponse>());
                return;
            }
        });
    }
}
