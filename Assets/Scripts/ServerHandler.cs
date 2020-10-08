using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ServerHandler : MonoBehaviour
{
    public static string URL = "localhost:1337";
    public static string out1;
    public static string out2;
    public static string out3;
    public static bool success;
    static ServerHandler instance;

    private void Awake()
    {
        instance = this;
    }

    public static void StartGameSessionToken(string userID, string locationID, string token)
    {
        instance.StartCoroutine(StartGameSessionTokenCoroutine(userID, locationID, token));
    }

    public static IEnumerator StartGameSessionTokenCoroutine(string userID, string locationID, string token)
    {
        success = false;
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("userid", userID);
        form.AddField("locationid", locationID);
        form.AddField("token", token);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/startsessiontoken", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully

            //get session id
            string sessionID = request.GetResponseHeader("sessionid");

            Debug.Log("session created with id: " + sessionID);
            success = true;

            //session id must be saved for future requests
        }
    }

    public static void GetSessionToken(string locationID)
    {
        instance.StartCoroutine(GetSessionTokenCoroutine(locationID));
    }

    public static IEnumerator GetSessionTokenCoroutine(string locationID)
    {
        success = false;
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("locationid", locationID);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/getsessiontoken", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if (request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully
            var encryptedToken = request.GetResponseHeader("token");

            out1 = encryptedToken;

            Debug.LogFormat("new token (encrypted): {0}", encryptedToken);
            success = true;

        }

    }

    public static IEnumerator GetSessionByLocationCoroutine(string locationID)
    {
        success = false;
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("locationid", locationID);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/getactivesessionbylocation", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if(request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully
            var user = request.GetResponseHeader("userid");
            var session = request.GetResponseHeader("sessionid");
            var state = request.GetResponseHeader("state");

            out1 = user;
            out2 = session;
            out3 = state;

            Debug.LogFormat("session: user {0}, session {1}, state {2}", user, session, state);

            success = true;

        }

    }

    public static IEnumerator EndGameSessionCoroutine(string userID, string locationID, string gameState)
    {
        success = false;
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("userid", userID);
        form.AddField("locationid", locationID);
        form.AddField("gamestate", gameState);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/endsession", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if(request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully
            Debug.Log(request.downloadHandler.text);
            success = true;

        }

    }

    public static IEnumerator ConfirmSessionCoroutine(string userID, string locationID)
    {
        success = false;
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("userid", userID);
        form.AddField("locationid", locationID);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/confirmsession", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if(request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully

            Debug.Log(request.downloadHandler.text);
            success = true;

        }

    }

    public static IEnumerator RemoveSessionsByLocationCoroutine(string locationID)
    {
        success = false;
        //adding parameters for the POST request
        WWWForm form = new WWWForm();
        form.AddField("locationid", locationID);

        //creating request
        UnityWebRequest request = UnityWebRequest.Post(URL + "/removesessionsbylocation", form);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if(request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully

            Debug.Log(request.downloadHandler.text);
            success = true;

        }

    }

    public static IEnumerator GetUserCoroutine(string userID)
    {
        success = false;
        //adding parameters for the POST request
        var parameters = "?userid=" + userID;

        //creating request
        UnityWebRequest request = UnityWebRequest.Get(URL + "/getuser" + parameters);

        //sending request and waiting for response
        yield return request.SendWebRequest();

        if(request.isHttpError || request.isNetworkError)
        {
            //error handling
            Debug.Log("request ended with an error: " + request.error);
            Debug.Log(request.downloadHandler.text);
        }
        else
        {
            //response ended succesfully
            out1 = request.downloadHandler.text;
            Debug.Log(request.downloadHandler.text);
            success = true;

        }

    }


}
