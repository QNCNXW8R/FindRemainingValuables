using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace FindRemainingValuables;

[BepInPlugin("QNCNXW8R.FindRemainingValuables", "FindRemainingValuables", "1.0")]
public class FindRemainingValuables : BaseUnityPlugin
{
    internal static FindRemainingValuables Instance { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

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

    internal void Update()
    {
        if (RevealKeybind.Value.IsDown())
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
        var valuables = Object.FindObjectsOfType<ValuableObject>();
        foreach (var valuable in valuables)
        {
            if (!valuable.discovered)
            {
                valuable.Discover(ValuableDiscoverGraphic.State.Discover);
                Logger.LogInfo("Revealed valuables.");
            }
        }
    }
}
