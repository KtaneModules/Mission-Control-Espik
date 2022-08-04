using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KModkit;

public class MissionControl : MonoBehaviour {
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public KMSelectable ButtonSelectable;
    public TextMesh ButtonText;
    public Transform ButtonTransform;

    // Logging info
    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved = false;

    private string mission;
    private bool missionFound = false;
    private bool isUndefined = false;

    private bool canPressButton = true;
    private static bool canPlayIntro = true;

    private bool flickerText = false;
    private float transparency = 0.0f;

    private int mode = 0;
    /* 1: Dead End (Big)
     * 2: Dead End (Small)
     * 3: Disconnected
     */

    // Mission specific variables
    private bool bombSolved = false;
    private static bool deadEndSolve = false;
    private readonly float DEADENDSTART = 12000.0f; // 12000
    private static float finishingTime = 55.0f;
    private float iteration = 0.0f;

    private float currentSecond = 0.0f;

    private MissionControlSettings Settings;

    sealed class MissionControlSettings {
        public bool IntroSound = true;
    }

    // Ran as bomb loads
    private void Awake() {
        moduleId = moduleIdCounter++;

        // Module Settings
        var modConfig = new ModConfig<MissionControlSettings>("MissionControl");
        Settings = modConfig.Settings;
        modConfig.Settings = Settings;

        ButtonSelectable.OnInteract += delegate () { ButtonPressed(); return false; };

        Module.OnActivate += OnActivate;
    }

    // Gets information
    private void Start() {
        StartCoroutine(AnimateButton());

        mission = GetMission();

        switch (mission) {
        case "undefined":
            isUndefined = true;
            break;

        case "mod_dead_end_deadend": // Dead End
            Debug.LogFormat("[Mission Control #{0}] Found mission: \"Dead End\"", moduleId);
            missionFound = true;
            mode = Bomb.GetSolvableModuleNames().Count() == 1 ? 2 : 1;

            if (mode == 2)
                canPressButton = false;

            break;

        case "mod_ktane_EspikHardMissions_disconnected": // Disconnected
            Debug.LogFormat("[Mission Control #{0}] Found mission: \"Disconnected\"", moduleId);
            missionFound = true;
            mode = 3;
            break;
        }
    }


    // Affects the bomb based on mission rules
    private void Update() {
        switch (mode) {
        case 1: // Dead End (Big)
            if (!bombSolved) {
                // Increases the timer speed
                if (ZenModeActive) {
                    if (Mathf.Floor(Bomb.GetTime()) != iteration) {
                        iteration = Mathf.Floor(Bomb.GetTime());

                        // Prevents the timer from going too fast
                        if (iteration >= 12000.0f)
                            TimeRemaining.FromModule(Module, Bomb.GetTime() + 0.75f);

                        else
                            TimeRemaining.FromModule(Module, Bomb.GetTime() + (Mathf.Floor(iteration / 160.0f) / 100.0f));
                    }
                }

                else {
                    if (Mathf.Floor(Bomb.GetTime()) != DEADENDSTART - iteration) {
                        iteration = DEADENDSTART - Mathf.Floor(Bomb.GetTime());
                        TimeRemaining.FromModule(Module, Bomb.GetTime() - (Mathf.Floor(iteration / 160.0f) / 100.0f));
                    }
                }

                // Bomb solves
                if (Bomb.GetSolvedModuleNames().Count() == Bomb.GetSolvableModuleNames().Count()) {
                    bombSolved = true;
                    finishingTime = Bomb.GetTime();
                    deadEndSolve = true;
                }
            }
            break;

        case 2: // Dead End (Small)
            if (!bombSolved) {
                // Keeps the time between 55-56 seconds
                if (ZenModeActive) {
                    if (Bomb.GetTime() > 55.95f)
                        TimeRemaining.FromModule(Module, 55.01f);
                }

                else {
                    if (Bomb.GetTime() < 55.05f) {
                        TimeRemaining.FromModule(Module, 55.99f);
                    }
                }

                // Larger bomb is solved
                if (deadEndSolve) {
                    bombSolved = true;
                    deadEndSolve = false;
                    TimeRemaining.FromModule(Module, finishingTime);
                    Solve();
                }
            }
            break;

        case 3: // Disconnected
            if (Bomb.GetSolvedModuleNames().Count() == 53) {
                if (Mathf.Floor(Bomb.GetTime()) != currentSecond) {
                    float strikeModifier = 20.0f;

                    switch (Bomb.GetStrikes()) {
                        case 0: strikeModifier = 20.0f; break;
                        case 1: strikeModifier = 19.0f; break;
                        case 2: strikeModifier = 18.0f; break;
                        case 3: strikeModifier = 17.0f; break;
                        default: strikeModifier = 16.0f; break;
                    }

                    currentSecond = Mathf.Floor(Bomb.GetTime());

                    if (ZenModeActive)
                        TimeRemaining.FromModule(Module, Bomb.GetTime() + strikeModifier / 24.0f);

                    else
                        TimeRemaining.FromModule(Module, Bomb.GetTime() - strikeModifier / 24.0f);
                }
            }
            break;
        }
    }


    // Lights turn on
    private void OnActivate() {
        if (missionFound) {
            if (canPlayIntro && Settings.IntroSound) {
                canPlayIntro = false;
                Audio.PlaySoundAtTransform("missionControl_inEffect", transform);
            }
        }

        else {
            flickerText = true;

            if (isUndefined) {
                Debug.LogFormat("[Mission Control #{0}] Unable to detect missions.", moduleId);
                ButtonText.text = "FATAL\nERROR";
            }

            else {
                Debug.LogFormat("[Mission Control #{0}] No mission found.", moduleId);
                ButtonText.text = "ERROR";
            }

            StartCoroutine(FlickerTextRoutine());
        }
    }

    // Resets the sound effect
    private void OnDestroy() {
        canPlayIntro = true;
    }


    // Rotates the button so the texture animates
    private IEnumerator AnimateButton() {
        while (true) {
            ButtonTransform.Rotate(new Vector3(0.05f, 0.0f, 0.0f));
            yield return new WaitForSeconds(0.02f);
        }
    }


    // Makes the text fade in and out
    private IEnumerator FlickerTextRoutine() {
        while (flickerText) {
            transparency += 0.01f;
            transparency %= 2.0f;

            if (transparency > 1.0f) // Down
                ButtonText.color = new Color(1.0f, 1.0f, 1.0f, 2.0f - transparency);

            else // Up
                ButtonText.color = new Color(1.0f, 1.0f, 1.0f, transparency);

            yield return new WaitForSeconds(0.02f);
        }
    }


    // Gets the mission - code by S.
    private string GetMission() {
        try {
            Component gameplayState = GameObject.Find("GameplayState(Clone)").GetComponent("GameplayState");
            Type type = gameplayState.GetType();
            FieldInfo fieldMission = type.GetField("MissionToLoad", BindingFlags.Public | BindingFlags.Static);
            return fieldMission.GetValue(gameplayState).ToString();
        }

        catch (NullReferenceException) {
            return "undefined";
        }
    }
    

    // Button is pressed
    private void ButtonPressed() {
        ButtonSelectable.AddInteractionPunch(0.5f);

        // Unmodified time
        if (canPressButton && !moduleSolved) {
            Audio.PlaySoundAtTransform("missionControl_buttonPress", transform);
            Debug.LogFormat("[Mission Control #{0}] Button pressed at {1}.", moduleId, Bomb.GetFormattedTime());

            if (Bomb.GetSerialNumberNumbers().Sum() == Math.Floor(Bomb.GetTime()) % 60)
                Solve();

            else
                Strike();
        }
    }


    // Module solves
    private void Solve() {
        Debug.LogFormat("[Mission Control #{0}] Module solved!", moduleId);
        moduleSolved = true;
        GetComponent<KMBombModule>().HandlePass();
    }

    // Module strikes
    private void Strike() {
        Debug.LogFormat("[Mission Control #{0}] Strike!", moduleId);
        GetComponent<KMBombModule>().HandleStrike();
    }


#pragma warning disable 414
    private bool ZenModeActive;
#pragma warning restore 414
}