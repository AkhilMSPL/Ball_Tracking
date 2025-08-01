using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class BallProjectionHandler : MonoBehaviour
{
    [Header("API Configuration")]
    private const string baseURL = "https://qaapi.gullyball.com/api/v0/practice/";

    [Header("Scene References")]
    public GameObject sessionIdPanel;
    public TMP_InputField sessionIdInput;
    public GameObject camRotator;
    public GameObject ball;
    public TextMeshProUGUI ballInfoText;
    public Button previousButton;
    public Button nextButton;
    public float frameRate = 30f;

    [Header("DRS Tiles UI")]
    public Image hittingStumpsImage;
    public TMP_Text hittingStumpsText;

    public Image impactInlineImage;
    public TMP_Text impactInlineText;

    public Image pitchingInlineImage;
    public TMP_Text pitchingInlineText;

    public Image lbwImage;
    public TMP_Text lbwText;

    public Image reviewStatusImage;
    public TMP_Text reviewStatusText;

    public TMP_Text originalDecisionText;

    private List<BallTrajectory> ballTrajectories = new();
    private int currentBallIndex = 0;
    private int currentFrame = 0;
    private LineRenderer lineRenderer;
    private Coroutine trackingCoroutine;

    void Start()
    {
        sessionIdPanel.SetActive(true);
        camRotator.SetActive(false);
        lineRenderer = ball.GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = ball.AddComponent<LineRenderer>();

        lineRenderer.startWidth = 0.075f;
        lineRenderer.endWidth = 0.075f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.blue;

        if (previousButton != null) previousButton.onClick.AddListener(ShowPreviousBall);
        if (nextButton != null) nextButton.onClick.AddListener(ShowNextBall);
        UpdateNavigationButtons();
    }
    public void SubmitButton()
    {
        StartCoroutine(FetchAndLoadTrajectory());
    }
    IEnumerator FetchAndLoadTrajectory()
    {
        yield return FetchTrajectoryData((ResponseData data) =>
        {
            if (data.balls != null && data.balls.Count > 0)
            {
                StartCoroutine(LoadTrajectoryData(data));
            }
        });
    }

    IEnumerator FetchTrajectoryData(System.Action<ResponseData> onSuccess)
    {
        string sessionId = sessionIdInput != null ? sessionIdInput.text : string.Empty;
        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogError("Session ID input is empty.");
            yield break;
        }

        string url = baseURL + sessionId;
        string accessToken = SessionManager.Instance.accessToken;

        using UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            try
            {
                var response = JsonConvert.DeserializeObject<ApiResponse>(request.downloadHandler.text);
                if (response?.data != null)
                    onSuccess?.Invoke(response.data);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Deserialization Error: " + e.Message);
            }
        }
        else
        {
            Debug.LogError("API Error: " + request.error);
        }
    }

    IEnumerator LoadTrajectoryData(ResponseData data)
    {
        if (data.balls != null && data.balls.Count > 0)
        {
            ballTrajectories.Clear();

            foreach (var ballData in data.balls)
            {
                List<Vector3> positions = new();
                foreach (var coord in ballData.coordinates)
                {
                    if (coord.Count >= 3)
                    {
                        float x = coord[0] / 1000f;
                        float y = coord[1] / 1000f;
                        float z = coord[2] / 1000f;
                        positions.Add(new Vector3(x, y, z));
                    }
                }

                string speed = ballData.ballSpeed != null ? ballData.ballSpeed.val + " " + ballData.ballSpeed.unit : "N/A";

                ballTrajectories.Add(new BallTrajectory
                {
                    ballId = ballData.ballId,
                    positions = positions,
                    speed = speed,
                    clipUrl = ballData.ballClipUrl,
                    hittingStumps = ballData.hitting_stumps,
                    hittingStumpsFlag = ballData.hitting_stumps_flag,
                    impactInline = ballData.impact_inline,
                    impactInlineFlag = ballData.impact_inline_flag,
                    pitchingInline = ballData.pitching_inline,
                    pitchingInlineFlag = ballData.pitching_inline_flag,
                    lbw = ballData.lbw,
                    lbwFlag = ballData.lbw_flag,
                    reviewStatus = ballData.review_status,
                    reviewStatusFlag = ballData.review_status_flag,
                    originalDecision = ballData.original_decision
                });
            }

            ShowBall(0);
        }

        yield return null;
    }

    void ShowBall(int index)
    {
        if (index < 0 || index >= ballTrajectories.Count) return;

        if (trackingCoroutine != null)
        {
            StopCoroutine(trackingCoroutine);
            trackingCoroutine = null;
        }

        currentBallIndex = index;
        UpdateNavigationButtons();
        UpdateBallInfo();
        trackingCoroutine = StartCoroutine(TrackBall(ballTrajectories[index].positions));
    }

    IEnumerator TrackBall(List<Vector3> positions)
    {
        currentFrame = 0;
        lineRenderer.positionCount = 0;
        camRotator.SetActive(true);

        while (currentFrame < positions.Count)
        {
            ball.transform.position = positions[currentFrame];
            lineRenderer.positionCount = currentFrame + 1;
            lineRenderer.SetPosition(currentFrame, ball.transform.position);
            currentFrame++;
            yield return new WaitForSeconds(1f / frameRate);
        }
    }

    void UpdateTile(Image tileImage, TMP_Text tileText, string valueText, bool isPositive)
    {
        if (tileImage != null)
            tileImage.color = isPositive ? Color.green : Color.red;

        if (tileText != null)
            tileText.text = valueText;
    }

    void UpdateBallInfo()
    {
        if (ballInfoText != null && ballTrajectories.Count > currentBallIndex)
        {
            BallTrajectory ball = ballTrajectories[currentBallIndex];

            ballInfoText.text = $"Ball: {currentBallIndex + 1}\n" +
                                $"Ball Speed: {ball.speed}\n" +
                                $"Frames: {ball.positions.Count}";
            if (ball.hittingStumps == "Umpire's Call")
            {
                hittingStumpsText.text = "Umpire's Call";
                hittingStumpsImage.color = Color.yellow;
            }
            else
            {
                UpdateTile(hittingStumpsImage, hittingStumpsText, ball.hittingStumps, ball.hittingStumpsFlag);
            }
            if(ball.lbwFlag == false)
            {
                lbwText.text = "Not Out";
                lbwImage.color = Color.green;
            }
            else
            {
                lbwText.text = "Out";
                lbwImage.color = Color.red;
            }
            UpdateTile(impactInlineImage, impactInlineText, ball.impactInline, ball.impactInlineFlag);
            UpdateTile(pitchingInlineImage, pitchingInlineText, ball.pitchingInline, ball.pitchingInlineFlag);
            UpdateTile(reviewStatusImage, reviewStatusText, ball.reviewStatus, ball.reviewStatusFlag);

            if (originalDecisionText != null)
                originalDecisionText.text = ball.originalDecision;
        }
    }

    void UpdateNavigationButtons()
    {
        if (previousButton != null)
            previousButton.interactable = (currentBallIndex > 0);

        if (nextButton != null)
            nextButton.interactable = (currentBallIndex < ballTrajectories.Count - 1);
    }

    public void ShowNextBall() => ShowBall(currentBallIndex + 1);
    public void ShowPreviousBall() => ShowBall(currentBallIndex - 1);

    public void ExitButton()
    {
        Application.Quit();
    }

    public class ApiResponse
    {
        public int status;
        public string message;
        public ResponseData data;
    }

    public class ResponseData
    {
        public string _id;
        public string sessionId;
        public string userId;
        public bool coordsGenerated;
        public List<BallData> balls;
    }

    public class BallData
    {
        public string ballId;
        public string ballClipUrl;
        public BallSpeed ballSpeed;
        public List<List<float>> coordinates;
        public string hitting_stumps;
        public bool hitting_stumps_flag;
        public string impact_inline;
        public bool impact_inline_flag;
        public string pitching_inline;
        public bool pitching_inline_flag;
        public string lbw;
        public bool lbw_flag;
        public string review_status;
        public bool review_status_flag;
        public string original_decision;
    }

    public class BallSpeed
    {
        public string val;
        public string unit;
    }

    private class BallTrajectory
    {
        public string ballId;
        public List<Vector3> positions;
        public string speed;
        public string clipUrl;

        public string hittingStumps;
        public bool hittingStumpsFlag;

        public string impactInline;
        public bool impactInlineFlag;

        public string pitchingInline;
        public bool pitchingInlineFlag;

        public string lbw;
        public bool lbwFlag;

        public string reviewStatus;
        public bool reviewStatusFlag;

        public string originalDecision;
    }
}
