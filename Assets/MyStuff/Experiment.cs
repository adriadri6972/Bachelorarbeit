using ExciteOMeter;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ARStressExperiment:MonoBehaviour {
    [Header("UI Panels")]
    public GameObject greetingPanel;
    public GameObject groupSelectionPanel;
    public GameObject experimentUIPanel;
    public GameObject settingsPanel;
    public GameObject videoPanel;

    [Header("Text & Anzeige")]
    public TMP_Text taskText;
    public TMP_Text taskText_check;
    public TMP_Text timeText;
    public Image timeImage;

    [Header("Herzrate Visualisierung")]
    public RectTransform hrTransform;
    public TMP_Text hrText;
    public Image hrImage;
    public ReactInletUI hrReceiverUI;


    [Header("Buttons")]
    public Button continueButton;
    public Button settingsButton;
    public Button backButton;
    public Button pickVideoButton;
    public Button resetVideoButton;


    [Header("Input Fields")]
    public TMP_InputField input_BaselineDuration;
    public TMP_InputField input_BaselineTaskDuration;
    public TMP_InputField input_StressDuration;
    public TMP_InputField input_StressTaskDuration;
    public TMP_InputField input_RestDuration;
    public TMP_InputField input_FakeHRBase;
    public TMP_InputField input_FakeHRVariance;
    public TMP_InputField input_CurrentVideoPath;


    [Header("Textdateien")]
    public TextAsset baselineFile;
    public TextAsset stressFile;

    private List<string> baselineTasks = new List<string>();
    private List<string> stressTasks = new List<string>();

    private enum Group { A_RealHR, B_FakeHR, C_NoHR }
    private Group selectedGroup;

    private bool showRealHR = false, showFakeHR = false;

    private float baselineDuration = 120f, baselineTaskInterval = 5f, stressDuration = 480f, stressTaskTime = 10f, restDuration = 300f;
    private int fakeHR_Base = 65, fakeHR_Variance = 5;

    private string videoPath = "";

    private void Update() {
        taskText_check.text = taskText.text;
    }

    void Start() {
        greetingPanel.SetActive(true);
        groupSelectionPanel.SetActive(false);
        experimentUIPanel.SetActive(false);
        settingsPanel.SetActive(false);
        videoPanel.SetActive(false);
        continueButton.gameObject.SetActive(false);

        taskText.text = "";
        taskText_check.text = "";
        hrText.text = "";
        timeText.enabled = false;
        timeImage.enabled = false;

        Color heartColor = new Color(0.9f, 0.2f, 0.2f);
        hrText.color = heartColor;
        hrImage.color = heartColor;

        settingsButton.onClick.AddListener(OpenSettings);
        backButton.onClick.AddListener(CloseSettings);
        pickVideoButton.onClick.AddListener(PickVideo);
        continueButton.onClick.AddListener(OnContinueClicked);

        LoadSettings();
    }

    public void StartExperiment() {
        greetingPanel.SetActive(false);
        groupSelectionPanel.SetActive(true);
    }

    public void SelectGroupA() => SelectGroup(Group.A_RealHR);
    public void SelectGroupB() => SelectGroup(Group.B_FakeHR);
    public void SelectGroupC() => SelectGroup(Group.C_NoHR);

    void SelectGroup(Group group) {
        selectedGroup = group;
        groupSelectionPanel.SetActive(false);
        experimentUIPanel.SetActive(true);

        showRealHR = group == Group.A_RealHR;
        showFakeHR = group == Group.B_FakeHR;
        hrTransform.gameObject.SetActive(group != Group.C_NoHR);

        LoadTasks();
        StartCoroutine(RunExperiment());
    }

    void LoadTasks() {
        baselineTasks.AddRange(baselineFile.text.Split('\n'));
        stressTasks.AddRange(stressFile.text.Split('\n'));
    }

    IEnumerator RunExperiment() {
        yield return StartCoroutine(Phase(baselineTasks,baselineDuration,false,baselineTaskInterval));
        yield return StartCoroutine(WaitForContinue());

        yield return StartCoroutine(Phase(stressTasks,stressDuration,true,stressTaskTime));
        yield return StartCoroutine(WaitForContinue());

        yield return StartCoroutine(PlayRestVideoAndReset());
    }

    IEnumerator Phase(List<string> tasks,float duration,bool isStress,float taskTime) {
        float timer = 0f, taskTimer = 0f, hrTimer = 0f;
        int index = 0;

        timeImage.enabled = isStress;
        timeText.enabled = isStress;

        if (tasks.Count > 0) taskText.text = tasks[index];

        while (timer < duration) {
            hrTimer += Time.deltaTime;
            if (hrTimer >= 1f) {
                int hrVal = showRealHR ? GetRealHR() : (showFakeHR ? GetSimulatedHR() : 0);
                hrText.text = hrVal > 0 ? hrVal.ToString() : "";
                if (hrVal > 0) StartCoroutine(PulseHR(hrVal));
                hrTimer = 0f;
            }

            if (isStress) {
                float remaining = taskTime - taskTimer;
                int s = Mathf.FloorToInt(remaining);
                int ms = Mathf.FloorToInt((remaining - s) * 1000);
                timeText.text = $"{s:00}.{ms:000}";

                float blinkSpeed = remaining <= 2 ? 8 : remaining <= 5 ? 4 : remaining <= 10 ? 2 : 0;
                float alpha = Mathf.PingPong(Time.time * blinkSpeed, 1f);
                Color c = new Color(1, 0, 0, alpha);
                timeText.color = c;
                timeImage.color = c;
            }

            taskTimer += Time.deltaTime;
            if (taskTimer >= taskTime) {
                taskText.text = "";
                yield return new WaitForSeconds(0.5f);
                index++;
                if (index < tasks.Count) taskText.text = tasks[index];
                taskTimer = 0f;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        taskText.text = "";
        timeText.text = "";
        timeImage.enabled = false;
    }

    IEnumerator WaitForContinue() {
        taskText.text = "short break";
        continueButton.gameObject.SetActive(true);
        continueButton.GetComponentInChildren<TMP_Text>().text = "Continue";
        bool clicked = false;
        continueButton.onClick.AddListener(() => clicked = true);
        while (!clicked) yield return null;
        continueButton.onClick.RemoveAllListeners();
        continueButton.gameObject.SetActive(false);
    }

    IEnumerator PlayRestVideoAndReset() {
        continueButton.interactable = false;
        continueButton.gameObject.SetActive(false);
        videoPanel.SetActive(true);
        yield return null;

        var vp = videoPanel.GetComponentInChildren<VideoPlayer>();

        string fallback = Path.Combine(Application.streamingAssetsPath, "default_video.mp4");
        string finalPath = (!string.IsNullOrEmpty(videoPath) && File.Exists(videoPath)) ? videoPath : fallback;

        if (vp != null && File.Exists(finalPath)) {
            vp.url = finalPath;
            vp.Prepare();

            while (!vp.isPrepared)
                yield return null;

            vp.Play();
            bool finished = false;
            vp.loopPointReached += (_) => finished = true;

            float hrTimer = 0f;

            while (!finished) {
                hrTimer += Time.deltaTime;

                if (hrTimer >= 1f) {
                    int hrVal = showRealHR ? GetRealHR() : (showFakeHR ? GetSimulatedHR() : 0);
                    hrText.text = hrVal > 0 ? hrVal.ToString() : "";
                    if (hrVal > 0) StartCoroutine(PulseHR(hrVal));
                    hrTimer = 0f;
                }

                yield return null;
            }
        }

        videoPanel.SetActive(false);

        taskText.text = "Finished!";
        continueButton.GetComponentInChildren<TMP_Text>().text = "Exit";
        continueButton.gameObject.SetActive(true);
        continueButton.interactable = true;

        bool clicked = false;
        continueButton.onClick.AddListener(() => clicked = true);
        while (!clicked) yield return null;
        continueButton.onClick.RemoveAllListeners();

        continueButton.gameObject.SetActive(false);
        experimentUIPanel.SetActive(false);
        greetingPanel.SetActive(true);
    }




    IEnumerator PulseHR(int hrVal) {
        float beatInterval = 60f / Mathf.Clamp(hrVal, 40, 180);
        Vector3 original = hrTransform.localScale;
        Vector3 pulse = original * 1.015f;
        float half = beatInterval / 2f;

        float t = 0f;
        while (t < 1f) { hrTransform.localScale = Vector3.Lerp(original,pulse,t); t += Time.deltaTime / half; yield return null; }
        t = 0f;
        while (t < 1f) { hrTransform.localScale = Vector3.Lerp(pulse,original,t); t += Time.deltaTime / half; yield return null; }
        hrTransform.localScale = original;
    }

    int GetSimulatedHR() => fakeHR_Base + Random.Range(-fakeHR_Variance,fakeHR_Variance + 1);

    int GetRealHR() {
        if (hrReceiverUI != null && hrReceiverUI.valueText != null) {
            if (int.TryParse(hrReceiverUI.valueText.text,out int hr))
                return hr;
        }
        return 0; // fallback
    }


    void OnContinueClicked() { }

    void OpenSettings() {
        greetingPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    void CloseSettings() {
        settingsPanel.SetActive(false);
        greetingPanel.SetActive(true);
    }

    public void OnBaselineDurationChanged(string val) {
        if (float.TryParse(val,out float f)) {
            f = Mathf.Max(f,10);
            baselineDuration = f;
            PlayerPrefs.SetFloat("baselineDuration",f);
            StartCoroutine(UpdateInputFieldNextFrame(input_BaselineDuration,f.ToString()));
        }
    }

    public void OnBaselineTaskDurationChanged(string val) {
        if (float.TryParse(val,out float f)) {
            f = Mathf.Max(f,2f);
            baselineTaskInterval = f;
            PlayerPrefs.SetFloat("baselineTaskInterval",f);
            StartCoroutine(UpdateInputFieldNextFrame(input_BaselineTaskDuration,f.ToString()));
        }
    }

    public void OnStressDurationChanged(string val) {
        if (float.TryParse(val,out float f)) {
            f = Mathf.Max(f,10f);
            stressDuration = f;
            PlayerPrefs.SetFloat("stressDuration",f);
            StartCoroutine(UpdateInputFieldNextFrame(input_StressDuration,f.ToString()));
        }
    }

    public void OnStressTaskDurationChanged(string val) {
        if (float.TryParse(val,out float f)) {
            f = Mathf.Max(f,2f);
            stressTaskTime = f;
            PlayerPrefs.SetFloat("stressTaskTime",f);
            StartCoroutine(UpdateInputFieldNextFrame(input_StressTaskDuration,f.ToString()));
        }
    }

    public void OnRestDurationChanged(string val) {
        if (float.TryParse(val,out float f)) {
            f = Mathf.Max(f,10f);
            restDuration = f;
            PlayerPrefs.SetFloat("restDuration",f);
            StartCoroutine(UpdateInputFieldNextFrame(input_RestDuration,f.ToString()));
        }
    }

    public void OnFakeHRBaseChanged(string val) {
        if (int.TryParse(val,out int v)) {
            v = Mathf.Max(v,40);
            fakeHR_Base = v;
            PlayerPrefs.SetInt("fakeHR_Base",v);
            StartCoroutine(UpdateInputFieldNextFrame(input_FakeHRBase,v.ToString()));
        }
    }

    public void OnFakeHRVarianceChanged(string val) {
        if (int.TryParse(val,out int v)) {
            v = Mathf.Max(v,1);
            fakeHR_Variance = v;
            PlayerPrefs.SetInt("fakeHR_Variance",v);
            StartCoroutine(UpdateInputFieldNextFrame(input_FakeHRVariance,v.ToString()));
        }
    }

    IEnumerator UpdateInputFieldNextFrame(TMP_InputField field,string newText) {
        yield return null; // wartet 1 Frame
        field.text = newText;
    }


    void LoadSettings() {
        baselineDuration = PlayerPrefs.GetFloat("baselineDuration",baselineDuration);
        baselineTaskInterval = PlayerPrefs.GetFloat("baselineTaskInterval",baselineTaskInterval);
        stressDuration = PlayerPrefs.GetFloat("stressDuration",stressDuration);
        stressTaskTime = PlayerPrefs.GetFloat("stressTaskTime",stressTaskTime);
        restDuration = PlayerPrefs.GetFloat("restDuration",restDuration);
        fakeHR_Base = PlayerPrefs.GetInt("fakeHR_Base",fakeHR_Base);
        fakeHR_Variance = PlayerPrefs.GetInt("fakeHR_Variance",fakeHR_Variance);
        videoPath = PlayerPrefs.GetString("videoPath","");

        input_BaselineDuration.text = baselineDuration.ToString();
        input_BaselineTaskDuration.text = baselineTaskInterval.ToString();
        input_StressDuration.text = stressDuration.ToString();
        input_StressTaskDuration.text = stressTaskTime.ToString();
        input_RestDuration.text = restDuration.ToString();
        input_FakeHRBase.text = fakeHR_Base.ToString();
        input_FakeHRVariance.text = fakeHR_Variance.ToString();

        if (input_CurrentVideoPath != null)
            input_CurrentVideoPath.text = !string.IsNullOrEmpty(videoPath) ? videoPath : "Default Video";

    }

    public void PickVideo() {
        NativeFilePicker.PickFile((path) => {
            if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
                videoPath = path;
                PlayerPrefs.SetString("videoPath",videoPath);
                PlayerPrefs.Save();
                Debug.Log("Video selected: " + path);
            }
        },new string[] { "video/*" });

    }
    public void ResetVideoPath() {
        videoPath = "";
        PlayerPrefs.DeleteKey("videoPath");
        PlayerPrefs.Save();
        if (input_CurrentVideoPath != null)
            input_CurrentVideoPath.text = "Default Video";
    }

    public void UpdateCurrentVideoPathUI() {
        if (input_CurrentVideoPath != null)
            input_CurrentVideoPath.text = !string.IsNullOrEmpty(videoPath) ? videoPath : "Default Video";
    }

}
