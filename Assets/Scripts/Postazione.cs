using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QRCoder;
using QRCoder.Unity; 
using System;
using System.IO;
using System.Runtime.CompilerServices;

// classe corrispondente al JSON di risposta alla richiesta HTTP "getuser"
public class UserDB
{
    public string id;
    public string username;
    public string facebookID;
}

public class Config
{
    public string ServerURL;
    public string LocationID;
    public string CypherKey;
}

public class Postazione : MonoBehaviour
{
    public string LocationID;
    public string CypherKey;
    [TextArea]
    public string BusyMessage;
    [TextArea]
    public string WaitingMessagePattern;
    public Material QRCodeMaterial;
    public GameObject QRCodeParent;
    public GameObject BusyParent;
    public TextMesh BusyText;
    private string lastUserID;
    private string lastUserName;
    private string lastToken;
    private string decryptedToken;
    private bool isBusy;    

#region Richieste HTTP
    IEnumerator StartSession(string user, string location, string token)
    {
        yield return StartCoroutine(ServerHandler.StartGameSessionTokenCoroutine(user, location, token));
    }

    IEnumerator ConfirmSession(string user, string location)
    {
        yield return StartCoroutine(ServerHandler.ConfirmSessionCoroutine(user, location));
    }

    IEnumerator EndSession(string user, string location)
    {
        yield return StartCoroutine(ServerHandler.EndGameSessionCoroutine(user, location, "cancelled:true"));
    }

    IEnumerator RemoveSessions(string location)
    {
        yield return StartCoroutine(ServerHandler.RemoveSessionsByLocationCoroutine(location));
    }

    IEnumerator IsLocationBusy(string location)
    {
        yield return StartCoroutine(ServerHandler.GetSessionByLocationCoroutine(location));
        isBusy = ServerHandler.success;
        if(isBusy)
        {
            string state = ServerHandler.out3;
            if(state == "waiting" || state == "playing")
            {
                string userID = ServerHandler.out1;
                yield return StartCoroutine(ServerHandler.GetUserCoroutine(userID));
                if(ServerHandler.success)
                {
                    UserDB user = JsonUtility.FromJson<UserDB>(ServerHandler.out1);
                    lastUserID = user.id;
                    lastUserName = user.username;
                }
            }
            UpdateMessage(state);
        }
    }

    IEnumerator GetSessionToken(string location)
    {
        yield return StartCoroutine(ServerHandler.GetSessionTokenCoroutine(location));
        lastToken = ServerHandler.out1.ToUpper();
    }
#endregion


    // Controlli di debug
    void OnGUI()
    {
        if(isBusy)
        {
            if(GUI.Button(new Rect(10f, 10f, 200f, 40f), "Confirm session"))
            {
                StartCoroutine(ConfirmSession(lastUserID, LocationID));
            }

            if(GUI.Button(new Rect(10f, 50f, 200f, 40f), "End session"))
            {
                StartCoroutine(EndSession(lastUserID, LocationID));
            }
        }
        else
        {
            if(GUI.Button(new Rect(10f, 10f, 200f, 40f), "Start session"))
            {
                StartCoroutine(StartSession(lastUserID, LocationID, decryptedToken));
            }

            if(GUI.Button(new Rect(10f, 50f, 200f, 40f), "Delete sessions"))
            {
                StartCoroutine(RemoveSessions(LocationID));
            }
        }
    }

    // Crea la texture del QR Code corrispondente alla string in input
    public Texture2D CreateQRCode(string payload)
    {
        try
        {
            QRCodeGenerator generator = new QRCodeGenerator();
            QRCodeData data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.H);
            UnityQRCode qrCode = new UnityQRCode(data);
            return qrCode.GetGraphic(20);
        }
        catch(Exception e)
        {
            Debug.LogError(e.Message);
        }
        return null;
    }

    // Loop principale:
    // Ogni secondo controlla se la postazione è occupata
    // Se lo è, mostra il messaggio "Postazione Occupata"
    // Se non lo è, genera il QR Code e lo mostra
    IEnumerator Loop()
    {
        bool generate = true;
        while(true)
        {
            yield return StartCoroutine(IsLocationBusy(LocationID));
            if(isBusy)
            {
                generate = true;
            }
            if(generate && !isBusy)
            {
                yield return StartCoroutine(GetSessionToken(LocationID));
                decryptedToken = AESEncrypter.Decrypt(lastToken, CypherKey);
                string qrMessage = LocationID + "&" + decryptedToken;
                Texture2D tex2D = CreateQRCode(qrMessage);
                QRCodeMaterial.SetTexture("_MainTex", tex2D);
                generate = false;
            }
            UpdateGraphics();
            yield return new WaitForSeconds(1f);
        }
    }

    // all'avvio legge configurazione da file e fa partire il loop principale
    void Start()
    {
#if !UNITY_EDITOR
        if(File.Exists("Config.txt"))
        {
            Config newConfig = JsonUtility.FromJson<Config>(File.ReadAllText("Config.txt"));
            if(newConfig != null)
            {
                LocationID = newConfig.LocationID;
                CypherKey = newConfig.CypherKey;
                ServerHandler.URL = newConfig.ServerURL;
            }
        }
#endif
        StartCoroutine(Loop());
    }

    void UpdateGraphics()
    {
        BusyParent.SetActive(isBusy);
        QRCodeParent.SetActive(!isBusy);
    }

    void UpdateMessage(string state)
    {
        switch(state)
        {
            case "playing":
                BusyText.text = BusyMessage;
                break;
            case "waiting":
                BusyText.text = String.Format(WaitingMessagePattern, lastUserName);
                break;
        }

    }
}
