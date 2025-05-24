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
    public static ConfigEntry<float>? thresholdConfig;

    private void Awake()
    {
        Instance = this;

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

    private IEnumerator PeriodicValueCheck()
    {

        ConfigEntry<float> thresholdConfig = Config.Bind(
            "General",
            "RevealThreshold",
            0.5f,
            new ConfigDescription(
                "Proportion of goal remaining before revealing all valuables. 0 is never, 4 is (probably) always.",
                new AcceptableValueRange<float>(0f, 4f) // Ensuring the range is between 0 and 4.
            )
        );

        while (true)
        {
            yield return new WaitForSeconds(5f);

            RoundDirector director = Object.FindObjectOfType<RoundDirector>();
            if (director == null)
                continue;

            float currentHaul = director.currentHaul;
            int goal = director.haulGoal;
            float threshold = thresholdConfig.Value;

            ValuableObject[] valuables = Object.FindObjectsOfType<ValuableObject>();
            float totalValue = valuables.Sum(v => v.dollarValueCurrent);

            float unhauledValue = totalValue - currentHaul;
            float thresholdValue = goal * threshold;

            if (unhauledValue <= thresholdValue)
            {
                foreach (var valuable in valuables)
                {
                    if (!valuable.discovered)
                    {
                        valuable.Discover(ValuableDiscoverGraphic.State.Discover);
                        Logger.LogInfo("Revealed valuables.");
                    }
                }
            }

            Logger.LogInfo($"Current Haul: {currentHaul}, Goal: {goal}");
            Logger.LogInfo($"Unhauled Value: {unhauledValue}, Threshold: {thresholdValue}");
        }
    }
}
