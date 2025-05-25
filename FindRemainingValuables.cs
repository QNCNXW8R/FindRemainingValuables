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

[BepInPlugin("QNCNXW8R.FindRemainingValuables", "FindRemainingValuables", "1.0")]
public class FindRemainingValuables : BaseUnityPlugin
{
    internal static FindRemainingValuables Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    private bool sceneReady = false;
    private bool hasRevealedThisScene = false;

    // Config entries
    public static ConfigEntry<float>? RevealThreshold;
    public static ConfigEntry<string>? GoalType;
    public static ConfigEntry<KeyboardShortcut>? RevealKeybind;
    public static ConfigEntry<bool>? EnableLogging;

    private void Awake()
    {
        Instance = this;

        RevealThreshold = Config.Bind(
            "General",
            "RevealThreshold",
            0.5f,
            new ConfigDescription(
                "Proportion of goal remaining before revealing all valuables: 0 is never, 4 is (probably) always",
                new AcceptableValueRange<float>(0f, 4f) // Ensuring the range is between 0 and 4.
            )
        );

        GoalType = Config.Bind(
            "General",
            "GoalType",
            "Level",
            new ConfigDescription(
                "Which goal to use when calculating reveal threshold: Extraction or Level",
                new AcceptableValueList<string>("Extraction", "Level")
            )
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
        // hasRevealedThisScene = !(SemiFunc.RunIsLobby() || SemiFunc.RunIsShop() || SemiFunc.RunIsArena());
    }

    internal void Update()
    {
        if (!sceneReady || hasRevealedThisScene)
            return;

        if (RevealKeybind.Value.IsDown())
        {
            Logger.LogInfo("Force reveal triggered by keybind");
            ForceReveal();
            // hasRevealedThisScene = true;
        }
    }

    private IEnumerator PeriodicValueCheck()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            RoundDirector director = Object.FindObjectOfType<RoundDirector>();
            if (director == null)
                continue;

            float currentHaul = director.currentHaul;
            int goal;
            if (GoalType.Value == "Level")
            {
                goal = director.haulGoal;
            }
            else
            {
                goal = director.extractionHaulGoal;
            }

            float threshold = RevealThreshold.Value;

            ValuableObject[] valuables = Object.FindObjectsOfType<ValuableObject>();
            float totalValue = valuables.Sum(v => v.dollarValueCurrent);

            float unhauledValue = totalValue - currentHaul;
            float thresholdValue = goal * threshold;

            if (unhauledValue <= thresholdValue && goal > 0)
            {
                ForceReveal();
            }

            if ((bool)EnableLogging?.Value)
            {
                Logger.LogInfo($"Current Haul: {currentHaul}, Goal: {goal}");
                Logger.LogInfo($"Unhauled Value: {unhauledValue}, Threshold: {thresholdValue}");
            }
        }
    }
    internal void ForceReveal()
    {
        if (hasRevealedThisScene) return;
        hasRevealedThisScene = true;
        var valuables = Object.FindObjectsOfType<ValuableObject>();

        foreach (var valuable in valuables)
        {
            if (!valuable.discovered)
            {
                valuable.Discover(ValuableDiscoverGraphic.State.Discover);
            }
        }

        float remaining = valuables
            .Where(v => !v.discovered)
            .Sum(v => v.dollarValueCurrent);

        PlayNotificationSound();
        ShowHaulMessage($"Valuables Revealed! ${Mathf.RoundToInt(remaining)} Left!");
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
