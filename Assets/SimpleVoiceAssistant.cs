using System.Collections.Generic;
using System.Linq;
using Meta.XR.BuildingBlocks.AIBlocks;
using Oculus.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRBuilder.Core;

public class SimpleVoiceAssistant : MonoBehaviour
{
    [Header("AI")]
    [SerializeField] private SpeechToTextAgent sstAgent;
    [SerializeField] private LlmAgent llmAgent;

    [Header("UI")]
    [SerializeField] private Button micButton;
    [SerializeField] private TMP_InputField transcriptInputField;
    [SerializeField] private TMP_Text responseText;
    [SerializeField] private PointableUnityEventWrapper pointableWrapper;
    [SerializeField] private GameObject UI;
    [SerializeField] private GameObject TeachingObjects;
    [SerializeField] private TextMeshProUGUI AIButtonText;

    private bool isListening = false;
    private bool waitingForTranscript = false;

    private TextMeshProUGUI buttonText;
    private string localClassification = "UNKNOWN";

    private string parentPrompt = @"You are an Air Cooler User Guide intent classifier.

        Return ONLY one keyword:

        STEP1
        STEP2
        STEP3
        STEP4
        STEP5
        STEP6
        STEP7
        UNKNOWN

        STEP1 = start tutorial, begin guide, start button

        STEP2 = plug in cooler, power outlet, connect power, turn on cooler

        STEP3 = fan, motor, front vents, airflow, how the cooler works

        STEP4 = speed knob, fan speed, low/medium/high speed, adjust airflow speed

        STEP5 = air vents, vent direction, tilt vents, airflow direction

        STEP6 = pump mode, swing mode, pump and swing, cooling pads, water circulation, cooling process, side-to-side airflow

        STEP7 = water inlet, water level indicator, drain outlet, wheels, filling tank, emptying tank, moving cooler

        If no match exists, return UNKNOWN.

        Return only the keyword and nothing else.

        Question: ";

    private void Awake()
    {
        // Cache button text
        if (micButton != null)
        {
            buttonText = micButton.GetComponentInChildren<TextMeshProUGUI>();

            if (buttonText != null)
            {
                buttonText.text = "Start Mic";
            }
        }

        // STT listener
        if (sstAgent != null)
        {
            sstAgent.onTranscript.RemoveListener(OnTranscriptReceived);
            sstAgent.onTranscript.AddListener(OnTranscriptReceived);
        }

        // LLM listener
        if (llmAgent != null)
        {
            llmAgent.onResponseReceived.RemoveListener(OnLlmResponseReceived);
            llmAgent.onResponseReceived.AddListener(OnLlmResponseReceived);
        }

        // UI button
        if (micButton != null && pointableWrapper!=null)
        {
            micButton.onClick.RemoveAllListeners();
            micButton.onClick.AddListener(OnMicButtonPressed);

            pointableWrapper.WhenSelect.RemoveAllListeners();
            pointableWrapper.WhenSelect.AddListener((args) =>
            {
                Debug.Log("[SimpleVoiceAssistant][INPUT] Button selected via Pointable");
                OnMicButtonPressed();
            });

        }

     //   _ = llmAgent.SendPromptAsync(parentPrompt, null);
    }

    private void DisableAllObjects()
    {
        foreach (Transform child in UI.transform)
        {
            child.gameObject.SetActive(false);
        }
        foreach (Transform child in TeachingObjects.transform)
        {
            child.gameObject.SetActive(false);
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) { Debug.Log("[INPUT] Space pressed"); OnMicButtonPressed(); }
    }

    private void OnMicButtonPressed()
    {
        if (!isListening)
        {
            StartListening();
            if (AIButtonText != null)
            {
                AIButtonText.text = "...";
            }
        }
        else
        {
            StopListening();
            if (AIButtonText != null)
            {
                AIButtonText.text = "AI";
            }
        }
    }

    private void StartListening()
    {
        if (sstAgent == null)
        {
            Debug.LogError("[STT] SpeechToTextAgent is missing.");
            return;
        }

        try
        {
            isListening = true;
            waitingForTranscript = false;

            if (transcriptInputField != null)
            {
                transcriptInputField.text = "";
            }

            if (responseText != null)
            {
                responseText.text = "";
            }

            if (buttonText != null)
            {
                buttonText.text = "Send";
            }

            Debug.Log("[SimpleVoiceAssistant][STT] Start Listening");

            sstAgent.StartListening();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[STT] StartListening failed: " + ex.Message);

            isListening = false;
            waitingForTranscript = false;

            if (buttonText != null)
            {
                buttonText.text = "Start Mic";
            }
        }
    }

    private void StopListening()
    {
        if (sstAgent == null)
        {
            Debug.LogError("[STT] SpeechToTextAgent is missing.");
            return;
        }

        try
        {
            Debug.Log("[SimpleVoiceAssistant][STT] Stop Listening");

            isListening = false;
            waitingForTranscript = true;

            if (buttonText != null)
            {
                buttonText.text = "Thinking...";
            }

            sstAgent.StopNow();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[STT] StopNow failed: " + ex.Message);

            waitingForTranscript = false;

            if (buttonText != null)
            {
                buttonText.text = "Start Mic";
            }
        }
    }

    private void OnTranscriptReceived(string transcript)
    {
        Debug.Log("[SimpleVoiceAssistant][STT] Transcript: " + transcript);

        if (transcriptInputField != null)
        {
            transcriptInputField.text = transcript;
        }

        // Only send after we've intentionally stopped listening
        if (waitingForTranscript)
        {
            waitingForTranscript = false;

            if (string.IsNullOrWhiteSpace(transcript))
            {
                Debug.LogWarning("[LLM] Transcript is empty.");

                if (buttonText != null)
                {
                    buttonText.text = "Start Mic";
                }

                return;
            }

            localClassification = ClassifyQuestion(transcript);

            Debug.Log("[SimpleVoiceAssistant] : LOCAL CLASSIFIER = " + localClassification);

            Debug.Log("[SimpleVoiceAssistant] : REQUEST SENT");

            var task = llmAgent.SendPromptAsync(parentPrompt + transcript, null);

            Debug.Log("[SimpleVoiceAssistant] : TASK STATUS = " + task.Status);
            task.ContinueWith(t =>
            {
                Debug.Log("[SimpleVoiceAssistant] : TASK FINISHED");
                Debug.Log("[SimpleVoiceAssistant] : TASK STATUS = " + t.Status);

                if (t.Exception != null)
                {
                    Debug.Log("[SimpleVoiceAssistant] : TASK ERROR = " + t.Exception);
                }
            });

            
        }
    }

    private void OnLlmResponseReceived(string response)
    {
        response = response.Trim();

        Debug.Log("[SimpleVoiceAssistant][SimpleVoiceAssistant] : LLM Response = " + response);
        Debug.Log("[SimpleVoiceAssistant] : RAW=[" + response + "]");
        Debug.Log("[SimpleVoiceAssistant] : LENGTH=" + response.Length);

        if (responseText != null)
        {
            responseText.text = response;
        }

        string finalResponse = response;

        if (string.IsNullOrWhiteSpace(response))
        {
            Debug.Log("[SimpleVoiceAssistant] : Empty LLM response. Using local classifier.");
            finalResponse = localClassification;
        }
        else if (response.ToUpper() == "UNKNOWN" &&
                 localClassification != "UNKNOWN")
        {
            Debug.Log("[SimpleVoiceAssistant] : LLM returned UNKNOWN. Using local classifier.");
            finalResponse = localClassification;
        }
        else if (!response.StartsWith("STEP"))
        {
            Debug.Log("[SimpleVoiceAssistant] : Invalid LLM response. Using local classifier.");
            finalResponse = localClassification;
        }

        Debug.Log("[SimpleVoiceAssistant] : LLM = " + response);
        Debug.Log("[SimpleVoiceAssistant] : LOCAL = " + localClassification);
        Debug.Log("[SimpleVoiceAssistant] : FINAL = " + finalResponse);

        DisableAllObjects();
        //Changed to Local Classification to avoid LLM misclassification issues.
        JumpToChapter(localClassification);

        if (buttonText != null)
        {
            buttonText.text = "Start Mic";
        }
    }

    private void OnDestroy()
    {
        if (sstAgent != null)
        {
            sstAgent.onTranscript.RemoveListener(OnTranscriptReceived);
        }

        if (llmAgent != null)
        {
            llmAgent.onResponseReceived.RemoveListener(OnLlmResponseReceived);
        }

        if (micButton != null)
        {
            micButton.onClick.RemoveAllListeners();

            if (pointableWrapper != null)
            {
                pointableWrapper.WhenSelect.RemoveAllListeners();
            }
        }
    }

    private void JumpToChapter(string llmResponse)
    {
        if (ProcessRunner.Current == null)
        {
            Debug.Log("[SimpleVoiceAssistant][SimpleVoiceAssistant] : ProcessRunner.Current is null.");
            return;
        }

        string targetChapterName = "";

        switch (llmResponse.Trim().ToUpper())
        {
            case "STEP1":
                targetChapterName = "Step1 : Start";
                break;

            case "STEP2":
                targetChapterName = "Step2 : Plug In";
                break;

            case "STEP3":
                targetChapterName = "Step3 : Introduction";
                break;

            case "STEP4":
                targetChapterName = "Step4 : Speed Adjustment";
                break;

            case "STEP5":
                targetChapterName = "Step5 : Vent Adjustment";
                break;

            case "STEP6":
                targetChapterName = "Step6 : Pump and Swing";
                break;

            case "STEP7":
                targetChapterName = "Step7 : Showing Other Parts";
                break;

            case "UNKNOWN":
                Debug.Log("[SimpleVoiceAssistant][SimpleVoiceAssistant] : LLM returned UNKNOWN.");
                return;

            default:
                Debug.Log("[SimpleVoiceAssistant][SimpleVoiceAssistant] : Unrecognized keyword = " + llmResponse);
                return;
        }

        var chapter = ProcessRunner.Current.Data.Chapters
            .FirstOrDefault(c => c.Data.Name == targetChapterName);

        if (chapter == null)
        {
            Debug.Log("[SimpleVoiceAssistant][SimpleVoiceAssistant] : Chapter not found: " + targetChapterName);

            Debug.Log("[SimpleVoiceAssistant][SimpleVoiceAssistant] : Available chapters:");

            foreach (var ch in ProcessRunner.Current.Data.Chapters)
            {
                Debug.Log("[SimpleVoiceAssistant][SimpleVoiceAssistant] : " + ch.Data.Name);
            }

            return;
        }

        // Don't jump if already in the requested chapter
        if (ProcessRunner.Current.Data.Current == chapter)
        {
            Debug.Log("[SimpleVoiceAssistant][SimpleVoiceAssistant] : Already in chapter: " + chapter.Data.Name);
            return;
        }

        Debug.Log("[SimpleVoiceAssistant][SimpleVoiceAssistant][SimpleVoiceAssistant] : Jumping to chapter: " + chapter.Data.Name);

        ProcessRunner.SetNextChapter(chapter);

        ProcessRunner.SkipCurrentChapter();
    }


    private string ClassifyQuestion(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return "UNKNOWN";

        question = question.ToLower();

        Dictionary<string, int> scores = new Dictionary<string, int>()
        {
            { "STEP1", 0 },
            { "STEP2", 0 },
            { "STEP3", 0 },
            { "STEP4", 0 },
            { "STEP5", 0 },
            { "STEP6", 0 },
            { "STEP7", 0 }
        };

        // STEP1 - Start
        AddScore(scores, "STEP1", question,
            ("start", 3),
            ("begin", 3),
            ("tutorial", 2),
            ("guide", 2));

        // STEP2 - Plug In
        AddScore(scores, "STEP2", question,
            ("plug", 4),
            ("power", 3),
            ("electricity", 3),
            ("socket", 3),
            ("outlet", 3),
            ("turn on", 2));

        // STEP3 - Introduction
        AddScore(scores, "STEP3", question,
            ("fan", 4),
            ("motor", 4),
            ("airflow", 3),
            ("air flow", 3),
            ("front vent", 3),
            ("front vents", 3),
            ("how it works", 5),
            ("cooler work", 5));

        // STEP4 - Speed
        AddScore(scores, "STEP4", question,
            ("speed", 5),
            ("speed knob", 6),
            ("knob", 3),
            ("low", 2),
            ("medium", 2),
            ("high", 2),
            ("fan speed", 6));

        // STEP5 - Vents
        AddScore(scores, "STEP5", question,
            ("vent", 5),
            ("vents", 5),
            ("direction", 4),
            ("tilt", 5),
            ("air direction", 5));

        // STEP6 - Pump & Swing
        AddScore(scores, "STEP6", question,
            ("pump", 6),
            ("swing", 6),
            ("cooling pad", 5),
            ("cooling pads", 5),
            ("water circulation", 5),
            ("water tank", 3),
            ("side to side", 5),
            ("cooling", 3));

        // STEP7 - Utility Parts
        AddScore(scores, "STEP7", question,
            ("water inlet", 8),
            ("water level", 8),
            ("indicator", 4),
            ("drain", 8),
            ("drain outlet", 8),
            ("wheel", 7),
            ("wheels", 7),
            ("tank", 4),
            ("fill", 5),
            ("empty", 5));

        string bestStep = "UNKNOWN";
        int bestScore = 0;

        foreach (var pair in scores)
        {
            if (pair.Value > bestScore)
            {
                bestScore = pair.Value;
                bestStep = pair.Key;
            }
        }

        Debug.Log("[SimpleVoiceAssistant] : Classification Result = " + bestStep);
        Debug.Log("[SimpleVoiceAssistant] : Score = " + bestScore);

        return bestScore >= 3 ? bestStep : "UNKNOWN";


    }

    private void AddScore(
    Dictionary<string, int> scores,
    string step,
    string question,
    params (string keyword, int weight)[] keywords)
    {
        foreach (var item in keywords)
        {
            if (question.Contains(item.keyword))
            {
                scores[step] += item.weight;
            }
        }
    }

}