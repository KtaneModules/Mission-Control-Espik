using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;

public class MissionControl : MonoBehaviour {
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public KMSelectable ButtonSelectable;
    public TextMesh ButtonText;
    public Transform ButtonTransform;

    // From Mystery Module
    public GameObject[] Cover;
    public GameObject[] PivotRight;
    public GameObject[] PivotLeft;


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
     * 4: Wish
     */

    // Mission specific variables
    private bool bombSolved = false;
    private static bool deadEndSolve = false;
    private readonly float DEADENDSTART = 12000.0f; // 12000
    private static float finishingTime = 55.0f;
    private float iteration = 0.0f;

    private float currentSecond = 0.0f;


    private readonly int[] WISH_THRESHOLDS = { 13, 26, 33, 41, 49, 59, 65, 71, 78, 83, 88, 93 };
    private readonly string[] WISH_MODULES = { "notX01", "deceptiveRainbowArrowsModule", "cube", "ChaoticCountdownModule", "whiteCipher", 
        "blackCipher", "bamboozlingButton", "TripleTraversalModule", "rgbMaze", "perceptron" };
    private readonly string[] WISH_HARD_MODULES = { "EncryptionLingoModule", "WalkingCubeModule" };

    private int buttonPresses = 0;
    private readonly float TIME_LOSS = 0.1f; // 10%

    private KMBombModule[] mystifiedModule = new KMBombModule[12];
    private Vector3[] mystifyScale = new Vector3[12];

    // Mod settings
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

        case "mod_ktane_EspikHardMissions_wish": // Wish
            Debug.LogFormat("[Mission Control #{0}] Found mission: \"Wish\"", moduleId);
            missionFound = true;
            mode = 4;
            StartCoroutine(HideWishModules());
            break;
        }
    }


    // Finds and covers the modules for Wish
    private IEnumerator HideWishModules() {
        int modulesLeft = 10;
        int hardModulesLeft = 2;

        int[] indecies = new int[10];
        for (int i = 0; i < indecies.Length; i++)
            indecies[i] = i;

        int[] hardIndecies = { 10, 11 };

        // Code from Mystery Module
        for (int i = 0; i < transform.parent.childCount; i++) {
            var module = transform.parent.GetChild(i).gameObject.GetComponent<KMBombModule>();
            if (module == null)
                continue;

            // First set of Wish modules
            if (modulesLeft > 0) {
                foreach (string name in WISH_MODULES) {
                    if (module.ModuleType == name) {
                        int rand = UnityEngine.Random.Range(0, modulesLeft);
                        mystifiedModule[indecies[rand]] = module;
                        StartCoroutine(CoverModule(indecies[rand]));

                        modulesLeft--;
                        for (int j = rand; j < modulesLeft; j++) {
                            indecies[j] = indecies[j + 1];
                        }
                        break;
                    }
                }
            }

            // Second set of Wish modules
            if (hardModulesLeft > 0) {
                foreach (string name in WISH_HARD_MODULES) {
                    if (module.ModuleType == name) {
                        int rand = UnityEngine.Random.Range(0, hardModulesLeft);
                        mystifiedModule[hardIndecies[rand]] = module;
                        StartCoroutine(CoverModule(hardIndecies[rand]));

                        hardModulesLeft--;
                        for (int j = rand; j < hardModulesLeft; j++) {
                            hardIndecies[j] = hardIndecies[j + 1];
                        }
                        break;
                    }
                }
            }
        }

        yield return null;
    }

    // Covers a module for Wish
    private IEnumerator CoverModule(int num) {
        // Code from Mystery Module
        Debug.LogFormat(@"[Mission Control #{0}] Hiding module: {1}", moduleId, mystifiedModule[num].ModuleDisplayName);

        Cover[num].SetActive(true);
        var mysPos = mystifiedModule[num].transform.localPosition;
        Cover[num].transform.parent = mystifiedModule[num].transform.parent;

        var scale = new Vector3(.95f, .95f, .95f);
        Cover[num].transform.localScale = scale;
        Cover[num].transform.rotation = mystifiedModule[num].transform.rotation;
        if (Cover[num].transform.rotation == new Quaternion(0f, 0f, 1f, 0f))
            Cover[num].transform.localPosition = new Vector3(mysPos.x, mysPos.y - 0.02f, mysPos.z);
        else
            Cover[num].transform.localPosition = new Vector3(mysPos.x, mysPos.y + 0.02f, mysPos.z);
        Debug.LogFormat(@"<Mission Control #{0}> Rotation: {1}", moduleId, Cover[num].transform.rotation);
        yield return null;
        Cover[num].transform.parent = transform.parent;

        /*MethodInfo mth;
        foreach (var component in mystifiedModule[num].gameObject.GetComponents<MonoBehaviour>())
            if ((mth = component.GetType().GetMethod("MysteryModuleHiding", BindingFlags.Public | BindingFlags.Instance)) != null) {
                if (mth.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(KMBombModule[]) }))
                    mth.Invoke(component, new object[] { keyModules.ToArray() });
                else if (mth.GetParameters().Length == 0)
                    mth.Invoke(component, null);
            }*/

        mystifyScale[num] = mystifiedModule[num].transform.localScale;
        mystifiedModule[num].transform.localScale = new Vector3(0, 0, 0);
        yield return null;
    }

    // Reveals a module for Wish
    private IEnumerator RevealWishModule(int num) {
        // Code from Mystery Module
        Debug.LogFormat(@"[Mission Control #{0}] Revealing module: {1}", moduleId, mystifiedModule[num].ModuleDisplayName);

        /*MethodInfo mth;
        foreach (var component in mystifiedModule[num].gameObject.GetComponents<MonoBehaviour>())
            if ((mth = component.GetType().GetMethod("MysteryModuleRevealing", BindingFlags.Public | BindingFlags.Instance)) != null && mth.GetParameters().Length == 0)
                mth.Invoke(component, null);*/

        var duration = 2.0f;
        var elapsed = 0.0f;
        while (elapsed < duration) {
            yield return null;
            elapsed += Time.deltaTime;
            mystifiedModule[num].transform.localScale = Vector3.Lerp(new Vector3(0.0f, 0.0f, 0.0f), mystifyScale[num], elapsed / duration);
            PivotRight[num].transform.localEulerAngles = new Vector3(0.0f, 0.0f, -90.0f * elapsed / duration);
            PivotLeft[num].transform.localEulerAngles = new Vector3(0.0f, 0.0f, 90.0f * elapsed / duration);
        }
        mystifiedModule[num].transform.localScale = mystifyScale[num];
        Destroy(Cover[num]);
        yield return null;
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

        // Wish
        if (mode == 4 && canPressButton && !moduleSolved) {
            canPressButton = false;
            Audio.PlaySoundAtTransform("missionControl_buttonPress", transform);
            Debug.LogFormat("[Mission Control #{0}] You pressed the button. Total presses: {1}", moduleId, buttonPresses + 1);

            StartCoroutine(RevealWishModule(buttonPresses));
            if (WISH_THRESHOLDS[buttonPresses] > Bomb.GetSolvedModuleNames().Count()) {
                Audio.PlaySoundAtTransform("missionControl_badChime", transform);
                TimeRemaining.FromModule(Module, Bomb.GetTime() * (1.0f - TIME_LOSS));
            }

            else
                Audio.PlaySoundAtTransform("missionControl_goodChime", transform);

            buttonPresses++;
            if (buttonPresses >= WISH_THRESHOLDS.Length)
                Solve();

            canPressButton = true;
        }

        // Unmodified rules
        else if (canPressButton && !moduleSolved) {
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
        Module.HandlePass();
    }

    // Module strikes
    private void Strike() {
        Debug.LogFormat("[Mission Control #{0}] Strike!", moduleId);
        Module.HandleStrike();
    }


    // Variable set by Tweaks for Zen mode detection
    #pragma warning disable 414
    private bool ZenModeActive;
    #pragma warning restore 414

    // Twitch Plays command handler - by eXish
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <##> [Presses the button when the seconds digits of the bomb's timer are '##']";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length == 1)
                yield return "sendtochaterror Please specify when to press the button!";
            else if (parameters.Length > 2)
                yield return "sendtochaterror Too many parameters!";
            else
            {
                int time = -1;
                if (!int.TryParse(parameters[1], out time))
                {
                    yield return "sendtochaterror!f The specified seconds digits '" + parameters[1] + "' are invalid!";
                    yield break;
                }
                if (time < 0 || time > 59)
                {
                    yield return "sendtochaterror The specified seconds digits '" + parameters[1] + "' are invalid!";
                    yield break;
                }
                if (parameters[1].Length < 2)
                {
                    yield return "sendtochaterror The specified seconds digits '" + parameters[1] + "' are invalid!";
                    yield break;
                }
                if (!canPressButton)
                {
                    yield return "sendtochaterror The button cannot be pressed yet!";
                    yield break;
                }
                yield return null;
                while (time != Math.Floor(Bomb.GetTime()) % 60) yield return "trycancel Halted waiting to press the button due to a cancel request.";
                ButtonSelectable.OnInteract();
            }
        }
    }

    // Twitch Plays autosolver - by eXish
    IEnumerator TwitchHandleForcedSolve()
    {
        while ((Bomb.GetSerialNumberNumbers().Sum() != Math.Floor(Bomb.GetTime()) % 60) || !canPressButton) yield return true;
        ButtonSelectable.OnInteract();
    }
}