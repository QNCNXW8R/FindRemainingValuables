using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

namespace FindRemainingValuables;

[BepInPlugin("QNCNXW8R.FindRemainingValuables", "FindRemainingValuables", "2.0.0")]
public class FindRemainingValuables : BaseUnityPlugin
{
    internal static FindRemainingValuables Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    private bool sceneReady = false;
    private bool hasRevealedThisScene = false;
    private float previousRemainingValue = -1f;

    // Config entries
    public static ConfigEntry<float>? RevealThreshold;
    public static ConfigEntry<string>? TrackingMethod;
    public static ConfigEntry<string>? GoalType;
    public static ConfigEntry<bool>? EnableHotkeys;
    public static ConfigEntry<KeyboardShortcut>? RevealKeybind;
    public static ConfigEntry<bool>? EnableLogging;

    private void Awake()
    {
        Instance = this;

        RevealThreshold = Config.Bind(
            "General",
            "RevealThreshold",
            0.1f,
            new ConfigDescription(
                "Proportion of goal remaining before revealing all valuables: 0 is never, 1 is always in LevelLoot mode, 4 is probably always in other modes",
                new AcceptableValueRange<float>(0f, 4f) // Ensuring the range is between 0 and 4.
            )
        );

        TrackingMethod = Config.Bind(
            "General",
            "TrackingMethod",
            "Haul",
            new ConfigDescription(
                "Method to track progress towards the threshold",
                new AcceptableValueList<string>("Haul", "Discovery")
            )
        );

        GoalType = Config.Bind(
            "General",
            "GoalType",
            "LevelLoot",
            new ConfigDescription(
                "Which goal to use when calculating reveal threshold: ExtractionGoal, LevelGoal, LevelLoot, Extractions",
                new AcceptableValueList<string>("ExtractionGoal", "LevelGoal", "LevelLoot", "Extractions")
            )
        );

        EnableHotkeys = Config.Bind(
            "Controls",
            "EnableHotkeys",
            true,
            "If true, enables hotkeys"
        );

        RevealKeybind = Config.Bind(
            "Controls",
            "RevealKeybind",
            new KeyboardShortcut(KeyCode.F10),
            new ConfigDescription("Key to press to reveal all remaining valuables", null, "HideFromREPOConfig")
        );

        EnableLogging = Config.Bind(
            "Debug",
            "EnableLogging",
            false,
            "If true, prints debug data to console"
        );

        // Prevent the plugin from being deleted
        this.gameObject.transform.parent = null;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        Patch();
        SceneManager.sceneLoaded += OnSceneLoaded;

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }

    internal void Patch()
    {
        Harmony ??= new Harmony(Info.Metadata.GUID);
        Harmony.PatchAll();
        StartCoroutine(PeriodicValueCheck());
    }

    internal void Unpatch()
    {
        Harmony?.UnpatchSelf();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneReady = true;
        hasRevealedThisScene = false;
    }

    internal void Update()
    {
        if (!sceneReady || hasRevealedThisScene)
            return;

        if (EnableHotkeys.Value &&
            RevealKeybind.Value.MainKey != KeyCode.None &&
            RevealKeybind.Value.IsDown())
        {
            Logger.LogInfo("Force reveal triggered by keybind");
            ForceReveal();
        }
    }

    private IEnumerator PeriodicValueCheck()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            RoundDirector director = Object.FindObjectOfType<RoundDirector>();
            if (director == null || !SemiFunc.RunIsLevel())
                continue;

            float undiscoveredValue = Object.FindObjectsOfType<ValuableObject>().Where(v => !v.discovered).Sum(v => v.dollarValueCurrent);
            if (undiscoveredValue == 0f && previousRemainingValue > 0)
            {
                ForceReveal();
            }

            float currentHaul = director.currentHaul;
            int goal;

            if (GoalType.Value == "LevelGoal")
            {
                goal = director.haulGoal;

                int totalPoints = director.extractionPoints;
                int completedPoints = director.extractionPointsCompleted;

                if (totalPoints > 0 && director.extractionPointList != null)
                {
                    float goalPerPoint = (float)director.haulGoal / totalPoints;

                    foreach (GameObject pointObj in director.extractionPointList)
                    {
                        ExtractionPoint point = pointObj.GetComponent<ExtractionPoint>();
                        if (point != null && point.currentState == ExtractionPoint.State.Complete)
                        {
                            currentHaul += goalPerPoint;
                        }
                    }
                }
            }
            else if (GoalType.Value == "ExtractionGoal")
            {
                goal = director.extractionHaulGoal;
            }
            else if (GoalType.Value == "LevelLoot")
            {
                goal = (int)Object.FindObjectsOfType<ValuableObject>().Sum(v => v.dollarValueCurrent);
            }
            else {
                goal = 1;
            }

            float threshold = RevealThreshold.Value;

            ValuableObject[] valuables = Object.FindObjectsOfType<ValuableObject>();
            float totalValue = valuables.Sum(v => v.dollarValueCurrent);
            float remainingValue;

            if (GoalType.Value == "Extractions")
            {
                remainingValue = director.extractionPoints - director.extractionPointsCompleted;
            }
            else if (TrackingMethod.Value == "Haul")
            {
                remainingValue = totalValue - currentHaul;
            }
            else
            {
                remainingValue = undiscoveredValue;
            }

            float thresholdValue = goal * threshold;

            if (remainingValue <= thresholdValue && goal > 0 && (director.extractionPointActive || director.extractionPointsCompleted > 0))
            {
                ForceReveal();
            }

            if ((bool)EnableLogging?.Value)
            {
                Logger.LogInfo($"Current Haul: {currentHaul}, Goal: {goal}");
                Logger.LogInfo($"Missing Value: {remainingValue}, Threshold: {thresholdValue}");
            }

            previousRemainingValue = Object.FindObjectsOfType<ValuableObject>().Where(v => !v.discovered).Sum(v => v.dollarValueCurrent);
        }
    }

    internal void ForceReveal()
    {
        if (hasRevealedThisScene) return;
        hasRevealedThisScene = true;
        var valuables = Object.FindObjectsOfType<ValuableObject>();

        float remainingValue;

        remainingValue = Object.FindObjectsOfType<ValuableObject>().Where(v => !v.discovered).Sum(v => v.dollarValueCurrent);

        if (remainingValue == 0)
        {
            remainingValue = previousRemainingValue;
        }

        foreach (var valuable in valuables)
        {
            if (!valuable.discovered)
            {
                valuable.Discover(ValuableDiscoverGraphic.State.Discover);
            }
        }

        PlayNotificationSound();
        ShowHaulMessage($"${Mathf.RoundToInt(remainingValue)} of Valuables Revealed!");
        Logger.LogInfo("Revealed valuables.");
    }

    public void PlayNotificationSound()
    {
        var clip = Resources.FindObjectsOfTypeAll<AudioClip>()
            .FirstOrDefault(c => c.name.Equals("valuable tracker target found"));
        if (clip != null)
        {
            Vector3 position = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            Logger.LogInfo($"Playing Audio: {clip.name}");
            AudioSource.PlayClipAtPoint(clip, position);
        }
        else
        {
            Logger.LogWarning("AudioClip 'valuable tracker target found' not found.");
        }
    }

    private void ShowHaulMessage(string message, float duration = 2f)
    {
        if (HaulUI.instance == null)
        {
            Logger.LogWarning("HaulUI.instance not found.");
            return;
        }

        var haulUI = HaulUI.instance;

        // Disable the Update() loop temporarily
        haulUI.enabled = false;

        // Get the private Text field
        var textField = AccessTools.Field(typeof(HaulUI), "Text").GetValue(haulUI) as TextMeshProUGUI;
        if (textField != null)
        {
            textField.text = message;
        }

        haulUI.SemiUITextFlashColor(Color.yellow, duration);
        haulUI.SemiUISpringShakeY(2f, 4f, 1f);

        // Re-enable after delay
        haulUI.StartCoroutine(EnableHaulUIAfterDelay(haulUI, duration));
    }

    private IEnumerator EnableHaulUIAfterDelay(HaulUI ui, float delay)
    {
        yield return new WaitForSeconds(delay);
        ui.enabled = true; // Reactivates Update() so it resumes showing $X / $Y
    }
}
