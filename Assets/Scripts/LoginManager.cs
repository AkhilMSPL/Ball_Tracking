using Newtonsoft.Json;
using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField phoneInput;
    public GameObject loginPanel;

    private const string BaseUrl = "https://qaapi.gullyball.com";
    private string userNameForLogin;

    void Start()
    {
        loginPanel.SetActive(true);
        VerifyAccessTokenOnStart();
    }

    void VerifyAccessTokenOnStart()
    {
        string accessToken = PlayerPrefs.GetString("accessToken", "");
        string refreshToken = PlayerPrefs.GetString("refreshToken", "");

        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.Log("No access token found. User needs to log in.");
            return;
        }

        StartCoroutine(VerifyAccessTokenCoroutine(accessToken, refreshToken));
    }

    IEnumerator VerifyAccessTokenCoroutine(string accessToken, string refreshToken)
    {
        string url = BaseUrl + "/api/v0/user/verify-access-token";
        UnityWebRequest www = new UnityWebRequest(url, "POST");
        www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(""));
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Authorization", "Bearer " + accessToken);
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Access token valid. Proceed to main app.");
            SessionManager.Instance.accessToken = accessToken;
            loginPanel.SetActive(false);
        }
        else
        {
            Debug.Log("Access token expired. Trying refresh token.");
            StartCoroutine(RefreshAccessToken(refreshToken));
        }
    }

    IEnumerator RefreshAccessToken(string refreshToken)
    {
        string url = BaseUrl + "/api/v0/user/refresh-access-token";
        UnityWebRequest www = new UnityWebRequest(url, "POST");
        www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(""));
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Authorization", "Bearer " + refreshToken);
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            var json = www.downloadHandler.text;
            var newToken = JsonConvert.DeserializeObject<TokenResponse>(json);

            PlayerPrefs.SetString("accessToken", newToken.data.accessToken);
            SessionManager.Instance.accessToken = newToken.data.accessToken;

            PlayerPrefs.SetString("refreshToken", newToken.data.refreshToken);
            SessionManager.Instance.refreshToken = newToken.data.refreshToken;

            Debug.Log("Tokens refreshed successfully.");
            loginPanel.SetActive(false);
        }
        else
        {
            Debug.Log("Refresh token expired. User needs to log in again.");
        }
    }

    public void CheckExistingUser()
    {
        string phoneNumber = "+91-" + phoneInput.text.Trim();
        StartCoroutine(CheckUserCoroutine(phoneNumber));
    }

    IEnumerator CheckUserCoroutine(string phoneNumber)
    {
        string url = BaseUrl + "/api/v1/user/existingUser";
        var requestJson = new CheckUserRequest
        {
            userIdentity = phoneNumber,
            claimAssist = true,
            isIdVerified = true,
            idVerificationSrc = "OTPLESS"
        };

        string json = JsonConvert.SerializeObject(requestJson);
        UnityWebRequest www = new UnityWebRequest(url, "POST");
        www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            var res = JsonConvert.DeserializeObject<CheckUserResponse>(www.downloadHandler.text);
            if (res.data != null && res.data.nextStep.action == "login")
            {
                userNameForLogin = res.data.claimAssist.userClaims.userName;
                Debug.Log("User exists. Proceeding to login: " + userNameForLogin);
                StartCoroutine(LoginUser());
            }
            else
            {
                Debug.Log("User does not exist. Please register through the Gullyball app.");
            }
        }
        else
        {
            Debug.Log("Check user failed: " + www.error);
        }
    }

    IEnumerator LoginUser()
    {
        string url = BaseUrl + "/api/v1/user/login";

        var req = new LoginRequest
        {
            identity = userNameForLogin,
            fcmDeviceToken = "",
            deviceId = "d3",
            platform = "android",
            claimAssist = true
        };

        string json = JsonConvert.SerializeObject(req);
        UnityWebRequest www = new UnityWebRequest(url, "POST");
        www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            var loginRes = JsonConvert.DeserializeObject<LoginResponse>(www.downloadHandler.text);

            PlayerPrefs.SetString("accessToken", loginRes.data.user.accessToken);
            SessionManager.Instance.accessToken = loginRes.data.user.accessToken;

            PlayerPrefs.SetString("refreshToken", loginRes.data.user.refreshToken);
            SessionManager.Instance.refreshToken = loginRes.data.user.refreshToken;
            loginPanel.SetActive(false);

            Debug.Log("Login successful.");
        }
        else
        {
            Debug.LogError("Login failed: " + www.error);
        }
    }

    // --- Models ---
    [Serializable]
    public class CheckUserRequest
    {
        public string userIdentity;
        public bool claimAssist;
        public bool isIdVerified;
        public string idVerificationSrc;
    }

    [Serializable]
    public class CheckUserResponse
    {
        public int status;
        public string message;
        public CheckUserData data;
    }

    [Serializable]
    public class CheckUserData
    {
        public string msg;
        public NextStep nextStep;
        public AuthInfo authInfo;
        public ClaimAssist claimAssist;
    }

    [Serializable]
    public class NextStep
    {
        public string action;
    }

    [Serializable]
    public class AuthInfo
    {
        public string primarySignupSrc;
        public MobileNo mobileNo;
    }

    [Serializable]
    public class MobileNo
    {
        public string primary;
        public bool isPrimaryVerified;
        public string secondary;
        public string countryCode;
    }

    [Serializable]
    public class ClaimAssist
    {
        public bool canClaim;
        public UserClaims userClaims;
    }

    [Serializable]
    public class UserClaims
    {
        public string userName;
        public string userPrimaryIdName;
        public string userPrimaryIdVal;
        public string usrProfilePic;
    }

    [Serializable]
    public class LoginRequest
    {
        public string identity;
        public string password;
        public string fcmDeviceToken;
        public string deviceId;
        public string platform;
        public bool claimAssist;
    }

    [Serializable]
    public class LoginResponse
    {
        public int status;
        public string message;
        public LoginData data;
    }

    [Serializable]
    public class LoginData
    {
        public LoginUserData user;
    }

    [Serializable]
    public class LoginUserData
    {
        public string accessToken;
        public string refreshToken;
    }

    [Serializable]
    public class TokenResponse
    {
        public int status;
        public string message;
        public TokenData data;
    }

    [Serializable]
    public class TokenData
    {
        public string accessToken;
        public string refreshToken;
    }
}
