using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using Wawa.DDL;
using UnityEngine.Windows.Speech;
using UnityEngine.UI;

public class MissionControl : MonoBehaviour {
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public KMSelectable ButtonSelectable;
    public TextMesh ButtonText;
    public TextMesh ButtonBigText;
    public Transform ButtonTransform;

    // From Mystery Module
    public GameObject[] Cover;
    public GameObject[] PivotRight;
    public GameObject[] PivotLeft;

    public Material PlanetMaterial;
    public Material BorderMaterial;
    public Material VignetteMaterial;

    public SpriteRenderer GoldenSlot;
    public Sprite[] GoldenSprites;

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
     * 5: Precise Instability
     * 6: For No Eyes Only
     * 7: Lost To Time
     * 8: Flyer's Manual Curse / Flyer's Alternative Manual Curse
     * 9: The Father of the Abyss
     * 10: The Mountain / The Mountain B-Side
     * 11: Command Prompt
     */


    // Mission specific variables
    private bool bombSolved = false;

    // Dead End
    private static bool deadEndSolve = false;
    private const float DEADENDSTART = 12000.0f; // 12000
    private static float finishingTime = 55.0f;
    private float iteration = 0.0f;

    // Disconnected
    private float currentSecond = 0.0f;

    // Wish
    private readonly int[] WISH_THRESHOLDS = { 13, 26, 33, 41, 49, 59, 65, 71, 78, 83, 88, 93 };
    private readonly string[] WISH_MODULES = { "notX01", "deceptiveRainbowArrowsModule", "cube", "ChaoticCountdownModule", "whiteCipher", 
        "blackCipher", "bamboozlingButton", "TripleTraversalModule", "rgbMaze", "perceptron" };
    private readonly string[] WISH_HARD_MODULES = { "EncryptionLingoModule", "WalkingCubeModule" };

    private int buttonPresses = 0;
    private readonly float TIME_LOSS = 0.1f; // 10%

    private KMBombModule[] mystifiedModule = new KMBombModule[12];
    private Vector3[] mystifyScale = new Vector3[12];

    // Precise Instability
    private bool acceptingStrikes = false;
    private bool readyToChange = false;
    private int actualStrikes = 0;
    private int bombStrikes = 0;
    private int storedNumber = 1;
    private float storedTime = 0.0f;

    private bool franticMode = false;
    private bool freezeTimer = false;
    private int displayedSecond = 30;
    private int enteredSecond = 0;
    private bool enteredTimerNumber = false;

    private const int JAM_STRIKE_LIMIT = 4;
    private const float JAM_BOMB_TIME = 4800.0f;

    private readonly string[] JAM_MODULES = { "3dTunnels", "AdjacentLettersModule", "atlantis", "bafflingBox", "boolMaze", "CheapCheckoutModule", "colorfulHexabuttons",
        "ColourFlash", "CrazyTalk", "cruelModulo", "cucumberModule", "decimation", "DecolourFlashModule", "digitString", "DiscoloredSquaresModule", "GlitchedButtonModule",
        "GrayButtonModule", "gyromaze", "HitmanModule", "KritHoldUps", "HumanResourcesModule", "identificationCrisis", "Indentation", "SCP2719", "jewelVault", "latinHypercube",
        "Laundry", "Lean", "letteredHexabuttons", "mazematics", "meteor", "MssngvWls", "neptune", "NotBitmapsModule", "OnlyConnectModule", "PianoParadoxModule", "poetry", "rottenBeans",
        "PasswordV2", "qSchlagDenBomb", "SetModule", "shapesAndColors", "shapeshift", "simonSelectsModule", "SimonShiftsModule", "simonStumbles", "ButtonV2", "stabilityModule",
        "timeMachine", "undertunneling", "vigenereCipher", "widdershins", "X01", "YahtzeeModule" };

    private KMBombModule[] jamModule = new KMBombModule[54];
    private Vector3[] jamModuleScale = new Vector3[54];
    private Vector3 missionControlScale;

    private readonly float BORDER_GREEN = 0.6640625f;
    private readonly float BUTTON_GREEN = 0.75f;
    private readonly float BUTTON_BLUE = 0.5f;

    private bool goldenPresent = false;
    private bool goldenActive = false;

    // For No Eyes Only
    private CameraPostProcess postProcess = null;
    private Transform cameraPos = null;
    private const int BLINDMODS = 47; // 47

    // The Father of the Abyss
    private float abyssTime = 12000.0f;

    // Command Prompt
    public GameObject fakeCubeSel;
    public GameObject processingLED;
    public GameObject textBacking;
    public Image textBackingImg;
    public Text overlayText;
    public Color textBackingColor;
    private DictationRecognizer dictationRecognizer;
    private Dictionary<string, string> modIDToScript = new Dictionary<string, string>()
    {
        { "KeypadV2", "AdvancedKeypad" },
        { "spwiz3DMaze", "ThreeDMazeModule" },
        { "SimonScreamsModule", "SimonScreamsModule" },
        { "CheapCheckoutModule", "CheapCheckoutModule" },
        { "YahtzeeModule", "YahtzeeModule" },
        { "visual_impairment", "VisualImpairment" },
        { "wire", "wireScript" },
        { "TheDigitModule", "TheDigitScript" },
        { "krazyTalk", "krazyTalkScript" },
        { "calcModule", "calcModuleScript" },
        { "PrimeChecker", "PrimeCheckerScript" },
        { "bootTooBig", "bootTooBigScript" },
        { "KritHoldUps", "HoldUpsScript" },
        { "polygons", "polygons" },
        { "Negativity", "NegativityScript" },
        { "Jailbreak", "Jailbreak" },
        { "colorNumbers", "colorNumberCode" },
        { "GSPentabutton", "PentabuttonScript" },
        { "BaybayinWords", "BaybayinWords" },
        { "symbolicCoordinates", "symbolicCoordinatesScript" },
        { "doubleScreenModule", "DoubleScreenScript" },
        { "notCrazyTalk", "NCTScript" },
        { "Words", "Words" },
        { "insaIlo", "InsaIloScript" },
        { "quizbowl", "QuizbowlScript" },
        { "shogiIdentification", "ShogiIdentificationScript" },
        { "PurchasingProperties", "PurchasingPropertiesGameplay" },
        { "reverseMorse", "reverseMorseScript" },
        { "InnerConnectionsModule", "InnerConnectionsScript" },
        { "invisymbol", "InvisymbolScript" },
        { "MaroonButtonModule", "MaroonButtonScript" },
        { "RedButtonModule", "RedButtonScript" },
        { "GrayButtonModule", "GrayButtonScript" },
        { "presidentialElections", "presidentialElectionsScript" },
        { "USACycle", "USACycle" },
        { "handTurkey", "handTurkey" },
        { "xelWhackTheCops", "WhackTheCops" },
        { "doofenshmirtzEvilIncModule", "DoofenshmirtzEvilIncScript" },
        { "Patterns", "Patterns" },
        { "tripleTermModule", "TripleTermScript" },
        { "jobApplication", "JobApplicationScript" },
        { "HaikuModule", "HaikuScript" },
        { "surveySays", "SurveySays" },
        { "BattleshipModule", "BattleshipModule" },
        { "whiteout", "whiteoutScript" },
        { "HexiEvilFMN", "EvilMemory" }
    };
    private Dictionary<string, string> modIDToAssembly = new Dictionary<string, string>()
    {
        { "KeypadV2", "HexiAdvancedBaseModules" },
        { "spwiz3DMaze", "3DMaze" },
        { "SimonScreamsModule", "SimonScreams" },
        { "CheapCheckoutModule", "CheapCheckoutModule" },
        { "YahtzeeModule", "Yahtzee" },
        { "visual_impairment", "visual_impairment" },
        { "wire", "wire" },
        { "TheDigitModule", "TheDigitModule" },
        { "krazyTalk", "krazyTalk" },
        { "calcModule", "calcModule" },
        { "PrimeChecker", "PrimeChecker" },
        { "bootTooBig", "bootTooBig" },
        { "KritHoldUps", "KritHoldUps" },
        { "polygons", "polygons" },
        { "Negativity", "Negativity" },
        { "Jailbreak", "TriviaMurderPartyPack" },
        { "colorNumbers", "colorNumbers" },
        { "GSPentabutton", "GSPentabutton" },
        { "BaybayinWords", "BaybayinWords" },
        { "symbolicCoordinates", "symbolicCoordinates" },
        { "doubleScreenModule", "doubleScreenModule" },
        { "notCrazyTalk", "notMods" },
        { "Words", "TriviaMurderPartyPack" },
        { "insaIlo", "insaIlo" },
        { "quizbowl", "quizbowl" },
        { "shogiIdentification", "shogiIdentification" },
        { "PurchasingProperties", "PurchasingProperties" },
        { "reverseMorse", "reverseMorse" },
        { "InnerConnectionsModule", "InnerConnections" },
        { "invisymbol", "invisymbol" },
        { "MaroonButtonModule", "BunchOfButtonsPack" },
        { "RedButtonModule", "BunchOfButtonsPack" },
        { "GrayButtonModule", "BunchOfButtonsPack" },
        { "presidentialElections", "presidentialElections" },
        { "USACycle", "USACycle" },
        { "handTurkey", "handTurkey" },
        { "xelWhackTheCops", "xelWhackTheCops" },
        { "doofenshmirtzEvilIncModule", "doofenshmirtzEvilIncModule" },
        { "Patterns", "TriviaMurderPartyPack" },
        { "tripleTermModule", "familiarFacesModules" },
        { "jobApplication", "jobApplication" },
        { "HaikuModule", "HaikuModule" },
        { "surveySays", "surveySays" },
        { "BattleshipModule", "Battleship" },
        { "whiteout", "whiteout" },
        { "HexiEvilFMN", "HexiCruelFMN" }
    };
    private GameObject selectedModule = null;
    private Coroutine displayText = null;
    private string[] reservedWords = { " DASH ", " DOT ", " PLUS ", " MINUS ", " ZERO ", " ONE ", " TWO ", " THREE ", " FOUR ", " FIVE ", " SIX ", " SEVEN ", " EIGHT ", " NINE ", " NOTHING ", " SPACE " };
    private string[] reservedWordsReplacements = { " - ", " . ", " + ", " - ", " 0 ", " 1 ", " 2 ", " 3 ", " 4 ", " 5 ", " 6 ", " 7 ", " 8 ", " 9 ", "", " " };
    private bool processingCmd;

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

        cameraPos = Camera.main.transform;
    }

    // Gets information
    private void Start() {
        StartCoroutine(AnimateButton());
        mission = GetMission();
        Debug.LogFormat("<Mission Control #{0}> Mission: {1}", moduleId, mission);

        switch (mission) {
        case "undefined":
            isUndefined = true;
            break;

        case "mod_dead_end_deadend": // Dead End
            Debug.LogFormat("[Mission Control #{0}] Found mission: \"Dead End\".", moduleId);
            missionFound = true;
            mode = Bomb.GetSolvableModuleNames().Count() == 1 ? 2 : 1;

            if (mode == 2)
                canPressButton = false;

            break;

        case "mod_ktane_EspikHardMissions_disconnected": // Disconnected
            Debug.LogFormat("[Mission Control #{0}] Found mission: \"Disconnected\".", moduleId);
            missionFound = true;
            mode = 3;
            break;

        case "mod_ktane_EspikHardMissions_wish": // Wish
            Debug.LogFormat("[Mission Control #{0}] Found mission: \"Wish\".", moduleId);
            missionFound = true;
            mode = 4;
            StartCoroutine(HideWishModules());
            break;

        case "mod_jamMissions_Espik": // Precise Instability
            Debug.LogFormat("[Mission Control #{0}] Found mission: \"Precise Instability\".", moduleId);
            missionFound = true;
            mode = 5;
            StartCoroutine(HideJamModules());
            storedNumber = UnityEngine.Random.Range(1, 21);
            break;

        case "mod_blindfoldMissions_blindBomb": // For No Eyes Only
            Debug.LogFormat("[Mission Control #{0}] Found mission: \"For No Eyes Only\".", moduleId);
            missionFound = true;
            mode = 6;
            SetBlackScreen();
            break;

        case "mod_blindfoldMissions_blindBombTest": // For No Eyes Only [Practice]
            Debug.LogFormat("[Mission Control #{0}] Found mission: \"For No Eyes Only [Practice]\".", moduleId);
            missionFound = true;
            mode = 6;
            break;

        case "mod_missionpack_VFlyer_missionTimeConstraint": // Lost To Time
            Debug.LogFormat("[Mission Control #{0}] Found mission: \"Lost To Time\".", moduleId);
            missionFound = true;
            mode = 7;
            break;

        case "mod_missionpack_VFlyer_missionModuleCorruption": // Flyer's Manual Curse
        case "mod_missionpack_VFlyer_missionModuleCorruptionALT": // Flyer's Alterative Manual Curse
            Debug.LogFormat("[Mission Control #{0}] Found mission: \"Flyer's Manual Curse\". Mission ran can be an ALT version.", moduleId);
            missionFound = true;
            mode = 8;
            break;

        case "mod_DansPissionMack_redacted": // The Father of the Abyss
            Debug.LogFormat("[Mission Control #{0}] Found mission: \"The Father of the Abyss\".", moduleId);
            missionFound = true;
            mode = 9;
            abyssTime = Bomb.GetTime();
            break;

        case "mod_theBombsBlanMade_mountain": //The Mountain
        case "mod_theBombsBlanMade_mountainBside": //The Mountain B-Side
            Debug.LogFormat("[Mission Control #{0}] Found mission: \"The Mountain{1}\".", moduleId, mission.Contains("Bside") ? " B-Side" : "");
            missionFound = true;
            mode = 10;
            ButtonTransform.localEulerAngles = new Vector3(0f, 0f, 270f);
            goldenPresent = true;
            StartCoroutine(AnimateGolden());
            break;

        case "mod_eXishMissions_cmdprompt": // Command Prompt
            Debug.LogFormat("[Mission Control #{0}] Found mission: \"Command Prompt\".", moduleId);
            missionFound = true;
            mode = 11;
            break;
        }
    }

    // Sets up everything for Command Prompt
    private void InitCmdPrompt()
    {
        for (int i = 0; i < transform.parent.childCount; i++)
        {
            Transform componentTransform = transform.parent.GetChild(i);
            KMBombModule bombModule = componentTransform.GetComponent<KMBombModule>();
            if (bombModule != null)
            {
                GameObject cube = Instantiate(fakeCubeSel, componentTransform);
                cube.transform.localPosition = componentTransform.localPosition;
                cube.transform.localEulerAngles = componentTransform.localEulerAngles;
                KMSelectable modSel = componentTransform.GetComponent<KMSelectable>();
                KMSelectable cubeSel = cube.GetComponent<KMSelectable>();
                var parentFace = componentTransform.GetComponent(ReflectionHelper.FindGameType("Selectable")).GetValue<object>("Parent");
                cubeSel.Parent = modSel;
                modSel.Children = new KMSelectable[] { cubeSel };
                modSel.ChildRowLength = 1;
                modSel.UpdateChildrenProperly();
                componentTransform.GetComponent(ReflectionHelper.FindGameType("Selectable")).SetValue("Parent", parentFace);
                if (bombModule.ModuleType == "Jailbreak")
                {
                    modSel.OnFocus = delegate () {
                        selectedModule = componentTransform.gameObject;
                    };
                    modSel.OnDefocus = delegate () {
                        selectedModule = null;
                    };
                    componentTransform.gameObject.GetComponent(ReflectionHelper.FindType(modIDToScript[bombModule.ModuleType], modIDToAssembly[bombModule.ModuleType])).SetValue("Focused", false);
                }
                else
                {
                    modSel.OnFocus += delegate () {
                        selectedModule = componentTransform.gameObject;
                    };
                    modSel.OnDefocus += delegate () {
                        selectedModule = null;
                    };
                }
            }
        }
        if (SystemInfo.operatingSystem.ToLower().Contains("windows"))
            StartDictationEngine();
        else
        {
            Debug.LogFormat("[Mission Control #{0}] DictationRecognizer error: GET_A_WINDOWS_COMPUTER_ERROR.", moduleId);
            flickerText = true;
            ButtonText.text = "VOICE\nERROR";
            StartCoroutine(FlickerTextRoutine());
        }
    }

    // Creates the voice recognition system for Command Prompt
    private void StartDictationEngine()
    {
        dictationRecognizer = new DictationRecognizer();
        dictationRecognizer.DictationResult += DictationRecognizer_OnDictationResult;
        dictationRecognizer.DictationComplete += DictationRecognizer_OnDictationComplete;
        dictationRecognizer.DictationError += DictationRecognizer_OnDictationError;
        dictationRecognizer.Start();
    }

    // Destroys the voice recognition system for Command Prompt
    private void CloseDictationEngine()
    {
        if (dictationRecognizer != null)
        {
            dictationRecognizer.DictationComplete -= DictationRecognizer_OnDictationComplete;
            dictationRecognizer.DictationResult -= DictationRecognizer_OnDictationResult;
            dictationRecognizer.DictationError -= DictationRecognizer_OnDictationError;
            if (dictationRecognizer.Status == SpeechSystemStatus.Running)
            {
                dictationRecognizer.Stop();
            }
            dictationRecognizer.Dispose();
        }
    }

    // Determines if the voice recognition system for Command Prompt needs a restart or throws a fatal error
    private void DictationRecognizer_OnDictationComplete(DictationCompletionCause completionCause)
    {
        switch (completionCause)
        {
            case DictationCompletionCause.TimeoutExceeded:
            case DictationCompletionCause.PauseLimitExceeded:
            case DictationCompletionCause.Canceled:
            case DictationCompletionCause.Complete:
                // Restart required
                CloseDictationEngine();
                StartDictationEngine();
                break;
            case DictationCompletionCause.UnknownError:
            case DictationCompletionCause.AudioQualityFailure:
            case DictationCompletionCause.MicrophoneUnavailable:
            case DictationCompletionCause.NetworkFailure:
                // Fatal error
                CloseDictationEngine();
                Debug.LogFormat("[Mission Control #{0}] DictationRecognizer encountered a fatal error.", moduleId);
                flickerText = true;
                ButtonText.text = "VOICE\nERROR";
                StartCoroutine(FlickerTextRoutine());
                break;
        }
    }

    // Runs whenever a command is successfully heard on Command Prompt
    private void DictationRecognizer_OnDictationResult(string text, ConfidenceLevel confidence)
    {
        if (selectedModule != null && !processingCmd)
        {
            string modID = selectedModule.GetComponent<KMBombModule>().ModuleType;
            text = " " + text.ToUpper() + " ";
            text = text.Replace(":00", "").Replace("MR.", "MR").Replace("COL.", "COL");
            for (int i = 0; i < reservedWords.Length; i++)
                text = ReplaceAllInstances(text, reservedWords[i], reservedWordsReplacements[i]);
            text = text.Trim();
            Debug.LogFormat("<Mission Control #{0}> Received command \"{1}\" for \"{2}\".", moduleId, text, modID);
            if (modID == "MissionControl")
            {
                if (text == "HELP")
                    text = TwitchHelpMessage.ToUpper().Replace("!{0} ", "").Replace(" | COUNTDOWN <1-20> [PRESSES THE BUTTON WHEN THE COUNTDOWN TIMER IS THE SPECIFIED NUMBER ON PRECISE INSTABILITY]", "");
                else
                    StartCoroutine(HandleCommand(null, text, modID));
            }
            else
            {
                object component = selectedModule.GetComponent(ReflectionHelper.FindType(modIDToScript[modID], modIDToAssembly[modID]));
                if (text == "HELP")
                {
                    if (modID == "YahtzeeModule")
                        text = component.GetValue<string>("TwitchHelpMessage").ToUpper().Replace("!{0} ", "").Replace(" | DONE [SOLVE]", "");
                    else
                        text = component.GetValue<string>("TwitchHelpMessage").ToUpper().Replace("!{0} ", "");
                }
                else
                    StartCoroutine(HandleCommand(component, text, modID));
            }
            if (displayText != null)
            {
                StopCoroutine(displayText);
                overlayText.color = Color.green;
                textBackingImg.color = textBackingColor;
            }
            displayText = StartCoroutine(DisplayCmdPromptText(text));
        }
    }

    // Runs whenever the voice recognition system on Command Prompt throws an error
    private void DictationRecognizer_OnDictationError(string error, int hresult)
    {
        Debug.LogFormat("[Mission Control #{0}] DictationRecognizer error: \"{1}\".", moduleId, error);
        flickerText = true;
        ButtonText.text = "VOICE\nERROR";
        StartCoroutine(FlickerTextRoutine());
    }

    // Replaces all instances of a word for commands in Command Prompt
    private string ReplaceAllInstances(string text, string replace1, string replace2)
    {
        while (text.Contains(replace1))
            text = text.Replace(replace1, replace2);
        return text;
    }

    // Handles each ran command on Command Prompt, some of this code may not be necessary for this bomb but better safe than sorry
    private IEnumerator HandleCommand(object component, string text, string modID)
    {
        processingCmd = true;
        int strikeCt = Bomb.GetStrikes();
        int solves = Bomb.GetSolvedModuleNames().Count();
        IEnumerator routine = null;
        bool simple = false;
        try
        {
            routine = component == null ? ProcessTwitchCommand(text.ToLower()) : component.CallMethod<IEnumerator>("ProcessTwitchCommand", text.ToLower());
        } catch (InvalidCastException) { simple = true; }
        if (simple)
        {
            IEnumerable<KMSelectable> btns = component.CallMethod<IEnumerable<KMSelectable>>("ProcessTwitchCommand", text.ToLower());
            foreach (KMSelectable btn in btns)
            {
                btn.OnInteract();
                yield return new WaitForSeconds(.1f);
                if (btn.OnInteractEnded != null)
                    btn.OnInteractEnded();

                if (strikeCt != Bomb.GetStrikes() || solves != Bomb.GetSolvedModuleNames().Count())
                    break;
            }
        }
        else
        {
            if (routine == null)
            {
                processingCmd = false;
                yield break;
            }
            while (true)
            {
                bool? moved = routine.MoveNext();
                if (moved.HasValue && !moved.Value)
                    break;

                object currentObj = routine.Current;
                if (currentObj is IEnumerable<KMSelectable>)
                {
                    foreach (var selectable in (IEnumerable<KMSelectable>)currentObj)
                    {
                        selectable.OnInteract();
                        yield return new WaitForSeconds(.1f);
                        if (selectable.OnInteractEnded != null)
                            selectable.OnInteractEnded();

                        if (strikeCt != Bomb.GetStrikes() || solves != Bomb.GetSolvedModuleNames().Count())
                            break;
                    }
                }
                else if (currentObj is string)
                {
                    Match match;
                    float waitTime;
                    string currentString = (string)currentObj;
                    if (currentString.RegexMatch(@"^(sendtochaterror!h) +(\S(?:\S|\s)*)$"))
                        break;
                    else if (currentString.RegexMatch(@"^trycancel((?: (?:.|\\n)+)?)$"))
                    {
                        yield return null;
                        continue;
                    }
                    else if (currentString.RegexMatch(out match, "^trywaitcancel ([0-9]+(?:\\.[0-9])?)((?: (?:.|\\n)+)?)$") && float.TryParse(match.Groups[1].Value, out waitTime))
                        yield return new WaitForSeconds(waitTime);
                }
                else
                    yield return currentObj;

                if (strikeCt != Bomb.GetStrikes() || solves != Bomb.GetSolvedModuleNames().Count())
                    break;
            }
        }
        if (strikeCt != Bomb.GetStrikes() || solves != Bomb.GetSolvedModuleNames().Count())
        {
            if (modID == "krazyTalk" && component.GetValue<bool>("_isHolding"))
                component.SetValue("_isHolding", false);
        }
        processingCmd = false;
    }

    // Displays command text on the user's screen temporarily
    private IEnumerator DisplayCmdPromptText(string text)
    {
        overlayText.text = text;
        yield return new WaitForSeconds(5f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime;
            overlayText.color = Color.Lerp(Color.green, Color.clear, t);
            textBackingImg.color = Color.Lerp(textBackingColor, Color.clear, t);
            yield return null;
        }
        overlayText.text = "";
        overlayText.color = Color.green;
        displayText = null;
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

    // Finds and hides modules for Precise Instability
    private IEnumerator HideJamModules() {
        canPressButton = false;
        int moduleCounter = 0;
        for (int i = 0; i < transform.parent.childCount; i++) {
            var module = transform.parent.GetChild(i).gameObject.GetComponent<KMBombModule>();
            if (module == null)
                continue;

            if (moduleCounter < jamModule.Length) {
                foreach (string name in JAM_MODULES) {
                    if (module.ModuleType == name) {
                        jamModule[moduleCounter] = module;
                        StartCoroutine(HideJamModule(moduleCounter, false, false));
                        moduleCounter++;
                        break;
                    }
                }
            }
        }

        Debug.LogFormat("<Mission Control #{0}> Hiding module: Mission Control", moduleId);
        missionControlScale = Module.transform.localScale;
        Module.transform.localScale = new Vector3(0, 0, 0);
        readyToChange = true;
        yield return null;
    }


    // Covers a module for Wish
    private IEnumerator CoverModule(int num) {
        // Code from Mystery Module
        Debug.LogFormat("[Mission Control #{0}] Hiding module: {1}", moduleId, mystifiedModule[num].ModuleDisplayName);

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
        Debug.LogFormat("<Mission Control #{0}> Rotation: {1}", moduleId, Cover[num].transform.rotation);
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
        Debug.LogFormat("[Mission Control #{0}] Revealing module: {1}", moduleId, mystifiedModule[num].ModuleDisplayName);

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


    // Hides a module for Precise Instability
    private IEnumerator HideJamModule(int num, bool delay, bool validate) {
        yield return new WaitForSeconds(0.02f);
        Debug.LogFormat("<Mission Control #{0}> Hiding module: {1}", moduleId, jamModule[num].ModuleDisplayName);

        if (!delay) {
            jamModuleScale[num] = jamModule[num].transform.localScale;
            jamModule[num].transform.localScale = new Vector3(0, 0, 0);
        }

        else {
            var duration = 0.5f;
            var elapsed = 0.0f;
            while (elapsed < duration) {
                yield return null;
                elapsed += Time.deltaTime;
                jamModule[num].transform.localScale = Vector3.Lerp(jamModuleScale[num], new Vector3(0.0f, 0.0f, 0.0f), elapsed / duration);
            }

            jamModule[num].transform.localScale = new Vector3(0, 0, 0);
        }

        if (validate)
            readyToChange = true;

        yield return null;
    }

    // Reveals a module for Precise Instability
    private IEnumerator RevealJamModule(int num, bool validate) {
        Debug.LogFormat("<Mission Control #{0}> Revealing module: {1}", moduleId, jamModule[num].ModuleDisplayName);

        var duration = 2.0f;
        var elapsed = 0.0f;
        while (elapsed < duration) {
            yield return null;
            elapsed += Time.deltaTime;
            jamModule[num].transform.localScale = Vector3.Lerp(new Vector3(0.0f, 0.0f, 0.0f), jamModuleScale[num], elapsed / duration);
        }

        jamModule[num].transform.localScale = jamModuleScale[num];

        if (validate)
            readyToChange = true;

        yield return null;
    }


    // Affects the bomb based on mission rules
    private void Update() {
        var solveCount = Bomb.GetSolvedModuleNames().Count;

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

        case 5: // Precise Instability
            if (acceptingStrikes && readyToChange && bombStrikes != Bomb.GetStrikes()) {
                actualStrikes++;

                // Bomb has 4+ strikes
                if (actualStrikes >= JAM_STRIKE_LIMIT && !ZenModeActive && !TimeModeActive) {
                    Debug.LogFormat("[Mission Control #{0}] Strike limit reached! Detonating bomb.", moduleId);
                    StartCoroutine(DetonateBomb(JAM_STRIKE_LIMIT));
                }

                else {
                    Debug.LogFormat("[Mission Control #{0}] Strike detected on another module! Entering countdown mode.", moduleId);
                    Debug.LogFormat("[Mission Control #{0}] To remove the effects of the strike, press the button when the timer displays {1}.", moduleId, storedNumber);

                    freezeTimer = true;
                    StartCoroutine(FreezeTimer());
                    storedTime = Bomb.GetTime();

                    acceptingStrikes = false;
                    readyToChange = false;
                    flickerText = false;
                    franticMode = true;
                    enteredTimerNumber = false;
                    enteredSecond = 0;

                    for (int i = 0; i < jamModule.Length; i++) {
                        if (i == jamModule.Length - 1)
                            StartCoroutine(HideJamModule(i, true, true));

                        else
                            StartCoroutine(HideJamModule(i, true, false));
                    }

                    StartCoroutine(ChangeBorderColor(false));
                    StartCoroutine(StartTimer());
                }
            }
            break;

        case 6: // For No Eyes Only
            if (!bombSolved && Bomb.GetSolvedModuleNames().Count() >= BLINDMODS) {
                StartCoroutine(FadeOutBlack(20.0f));
                bombSolved = true;
            }
            break;

        case 8: // Flyer's Manual Curse / Flyer's Alterative Manual Curse
            var toleratedStrikeLimit = solveCount / 5 + 1;
                if (toleratedStrikeLimit < 8)
                    ButtonText.text = string.Format("{0} / {1}\n{2}", solveCount, toleratedStrikeLimit * 5, toleratedStrikeLimit.ToString("0x"));
                else
                    ButtonText.text = "MAX REACHED\n8x";
            if (Bomb.GetStrikes() >= toleratedStrikeLimit && !(ZenModeActive || TimeModeActive))
                {
                    Debug.LogFormat("[Mission Control #{0}] Say goodbye to that attempt. At {1}, you solved {2} module(s) and struck {3} time(s). This mission cannot tolerate that many strikes in this state.", moduleId, Bomb.GetFormattedTime(), Bomb.GetSolvedModuleNames().Count, Bomb.GetStrikes());
                    TimeRemaining.FromModule(Module, 0f);
                }
            break;
        
        case 9: // The Father of the Abyss
            if (!bombSolved && Bomb.GetSolvedModuleNames().Count() >= Bomb.GetModuleNames().Count()) {
                StartCoroutine(FadeOutBlack(10.0f));
                bombSolved = true;
            }
            break;

        case 10: //The Mountain / The Mountain B-Side
            if (solveCount == 1 && !goldenActive) {
                Debug.Log(Bomb.GetSolvedModuleIDs()[0]);
                if (Bomb.GetSolvedModuleIDs()[0] == "MissionControl") {
                    goldenActive = true;
                    TimeRemaining.FromModule(Module, Bomb.GetTime() + 3600f);
                }
                else {
                    goldenPresent = false;
                    GoldenSlot.sprite = null;
                    ButtonTransform.localEulerAngles = new Vector3(0f, 0f, 90f);
                }
            }
            if (solveCount == Bomb.GetSolvableModuleIDs().Count() && goldenActive && goldenPresent) {
                goldenPresent = false;
                StartCoroutine(GoldenCollect());
            }
            if (Bomb.GetStrikes() > 0 && goldenActive) {
                Debug.LogFormat("[Mission Control #{0}] Struck with the golden strawberry. Detonating bomb.", moduleId);
                StartCoroutine(DetonateBomb(5));
            }
            if (Bomb.GetTime() < 3600f && goldenActive) {
                Debug.LogFormat("[Mission Control #{0}] Ran out of time with the golden strawberry. Detonating bomb.", moduleId);
                StartCoroutine(DetonateBomb(5));
            }
            break;
        case 11: // Command Prompt
            if (processingCmd)
                processingLED.SetActive(true);
            else
                processingLED.SetActive(false);
            if (overlayText.text != "")
                textBacking.SetActive(true);
            else
            {
                textBacking.SetActive(false);
                textBackingImg.color = textBackingColor;
            }
            break;
        }
    }

    // Lost To Time
    private IEnumerator InitLostToTimeBomb()
    {
        if (ZenModeActive || TimeModeActive) {
            ButtonText.text = "FATAL ERROR";
            StartCoroutine(FlickerTextRoutine());
            yield break;
        };
        /*
         * This entire section is denoted to be similar to how Time Mode handles scoring for each of the modules in this mission.
         * The major exception is due to fact that Time Mode has more precision when it comes to scoring.
         */
        var solvedModIDs = Bomb.GetSolvedModuleIDs();
        var timeGainModTiersAll = new Dictionary<int, IEnumerable<string>>() {
            { 0, new[]
            {   "MissionControl", "AnagramsModule", "whoOF", "BigButton", "Numpath",
                "NotMemory", "LightBulbs", "modulo", "cruelDigitalRootModule" } },
            { 1, new[]
            {   "gemDivision", "fourOperands", "colorCycleButton", "Password", "diffusion",
                "EncryptedDice", "daylightDirections", "DoubleOhModule", "LabelPrioritiesModule", "ColourFlash",
                "nonverbalSimon", "VCRCS", "booleanVennModule", "theRule", "YellowButtonModule", "factoryCubes" } },
            { 2, new[]
            {   "ColoredSwitchesModule", "SetModule", "triamonds", "ChordQualities", "yellowArrowsModule",
                "artPricing", "MysticSquareModule", "YahtzeeModule", "binaryTango", "masyuModule" } },
            { 3, new[]
            {   "sqlCruel", "digisibility", "loopover", "spillingPaint",
                "klaxon", "shikaku", "simonSelectsModule", "squeeze",
                "TheHypercubeModule", "MahjongQuizHard"} },
            { 4, new[]
            {   "KudosudokuModule", "unfairsRevenge", "WalkingCubeModule", "TripleTraversalModule",
                "notX01", "violetCipher", "synesthesia", "buttonGrid", "coralCipher",
                "notreDameCipher", "memoryPoker", "AzureButtonModule", "SouvenirModule", "soulscream" } },

        };
        //Debug.LogFormat("<Mission Control #{0}> DEBUG: Tier Distributions:\n{1}", moduleId,timeGainModTiersAll.Select(a => string.Format("[{0}: {1}]", a.Key, a.Value.Join(", "))).Join("\n"));
        while (!bombSolved && isActiveAndEnabled)
        {
            var curSolves = Bomb.GetSolvedModuleIDs();
            foreach (string modName in solvedModIDs)
                curSolves.Remove(modName);
            if (curSolves.Any())
            {
                var totalTimeToGain = 0f;
                var timeGainsPerTier = new[] { 22.5f, 45f, 90f, 180f, 360f };
                var timeMultipliersPerStrike = new[] { 1f, 1f, 0.5f, 0.5f, 0.25f, 0f };
                //Debug.LogFormat("<Mission Control #{0}> DEBUG MODS SOLVED AT {2} REMAINING: {1}", moduleId, curSolves.Join(), Bomb.GetFormattedTime());
                foreach (string nextSolve in curSolves)
                {
                    var lowestTierObtained = timeGainModTiersAll.Keys.FirstOrDefault(a => timeGainModTiersAll[a].Contains(nextSolve));
                    //Debug.LogFormat("<Mission Control #{0}> DEBUG: {1} considered as tier {2}.", moduleId, nextSolve, lowestTierObtained + 1);
                    totalTimeToGain += timeGainsPerTier[lowestTierObtained];
                }
                var timePenalty = Mathf.Max(0f, Bomb.GetStrikes() >= timeMultipliersPerStrike.Length ? timeMultipliersPerStrike.Last() : timeMultipliersPerStrike[Bomb.GetStrikes()]);
                totalTimeToGain *= timePenalty;
                TimeRemaining.FromModule(Module, Bomb.GetTime() + totalTimeToGain);
                solvedModIDs.AddRange(curSolves);
            }
            TimerRate.SetFromModule(Module, Mathf.Max(1f, 1f + Bomb.GetStrikes() - 4));
            yield return null;
            bombSolved = Bomb.GetSolvedModuleIDs().Count >= Bomb.GetSolvableModuleIDs().Count;
        }
    }


    // Strikes the bomb until it explodes
    private IEnumerator DetonateBomb(int n) {
        while (Bomb.GetStrikes() < n) {
            Module.HandleStrike();
            yield return new WaitForSeconds(0.02f);
        }
    }


    // Initiates the bomb for Precise Instability
    private IEnumerator InitJamBomb() {
        while (!readyToChange) { }
        readyToChange = false;
        StartCoroutine(AnimateTimer());

        for (int i = 0; i < jamModule.Length; i++) {
            StartCoroutine(RevealJamModule(i, false));
            yield return new WaitForSeconds(0.07f);
        }

        Debug.LogFormat(@"<Mission Control #{0}> Revealing module: Mission Control", moduleId);
        var duration = 2.0f;
        var elapsed = 0.0f;
        while (elapsed < duration) {
            yield return null;
            elapsed += Time.deltaTime;
            Module.transform.localScale = Vector3.Lerp(new Vector3(0.0f, 0.0f, 0.0f), missionControlScale, elapsed / duration);
        }

        Module.transform.localScale = missionControlScale;
        readyToChange = true;
        canPressButton = true;
        yield return null;
    }

    // Animates the timer for Precise Instability
    private IEnumerator AnimateTimer() {
        float delayTime = 1.0f / 60.0f;
        float startTime = 120.0f;
        float endTime = JAM_BOMB_TIME + storedNumber;

        if (ZenModeActive) {
            startTime = 0.0f;
            endTime = storedNumber;
        }

        else if (TimeModeActive) {
            startTime = Bomb.GetTime();
            endTime = Bomb.GetTime() + storedNumber;
        }

        yield return new WaitForSeconds(2.0f);

        for (float i = 0.0f; i < 105.0f; i++) {
            TimeRemaining.FromModule(Module, (endTime - startTime) * (i / 105.0f) + startTime);
            yield return new WaitForSeconds(delayTime);
        }

        TimeRemaining.FromModule(Module, endTime + 0.04f);

        freezeTimer = true;
        StartCoroutine(FreezeTimer());

        yield return new WaitForSeconds(2.0f);
        freezeTimer = false;
        acceptingStrikes = true;
        yield return null;
    }

    // Freezes the timer in place
    private IEnumerator FreezeTimer() {
        float frozenTime = Bomb.GetTime();
        while (freezeTimer) {
            TimeRemaining.FromModule(Module, frozenTime);
            yield return new WaitForSeconds(0.02f);
        }

        yield return null;
    }
    

    //Animates the golden strawberry for The Mountain
    private IEnumerator AnimateGolden() {
        int goldenFrame = 0;
        while (goldenPresent) {
            yield return new WaitForSeconds(0.066f);
            goldenFrame = (goldenFrame + 1) % 6;
            GoldenSlot.sprite = GoldenSprites[goldenFrame];
            GoldenSlot.transform.localPosition = new Vector3(0f, 0.03f, (float)Math.Sin(Bomb.GetTime() % 6.2831853f) * 0.005f - 0.0025f);
            yield return null;
        }
    }

    private IEnumerator GoldenCollect() {
        for (int g = 6; g < 20; g++) {
            yield return new WaitForSeconds(g != 15 ? 0.09f : 0.6f);
            GoldenSlot.sprite = GoldenSprites[g];
            if (g == 13) { StartCoroutine(GoldenFlash()); }
            yield return null;
        }
        GoldenSlot.sprite = null;
    }

    private IEnumerator GoldenFlash() {
        bool flashBool = false;
        while (true) {
            flashBool = !flashBool;
            GoldenSlot.color = new Color(1f, flashBool ? 1f : 0.69f, 0.69f);
            yield return new WaitForSeconds(0.06f);
        }
    }


    // Changes the color of the button and border
    private IEnumerator ChangeBorderColor(bool direction) {
        if (!direction) {
            StartCoroutine(FadeIn());

            for (float i = 89.0f; i >= 0.0f; i--) {
                BorderMaterial.color = new Color(1.0f, BORDER_GREEN * (i / 90.0f), 0.0f);
                PlanetMaterial.color = new Color(1.0f, BUTTON_GREEN * (i / 90.0f), BUTTON_BLUE * (i / 90.0f));
                yield return new WaitForSeconds(0.02f);
            }

            BorderMaterial.color = new Color(1.0f, 0.0f, 0.0f);
            PlanetMaterial.color = new Color(1.0f, 0.0f, 0.0f);
        }

        else {
            StartCoroutine(FadeOut());

            for (float i = 0.0f; i < 90.0f; i++) {
                BorderMaterial.color = new Color(1.0f, BORDER_GREEN * (i / 90.0f), 0.0f);
                PlanetMaterial.color = new Color(1.0f, BUTTON_GREEN * (i / 90.0f), BUTTON_BLUE * (i / 90.0f));
                yield return new WaitForSeconds(0.02f);
            }

            BorderMaterial.color = new Color(1.0f, BORDER_GREEN, 0.0f);
            PlanetMaterial.color = new Color(1.0f, BUTTON_GREEN, BUTTON_BLUE);
            acceptingStrikes = true;
        }

        yield return null;
    }

    // Starts the countdown timer
    private IEnumerator StartTimer() {
        Audio.PlaySoundAtTransform("missionControl_MechanismsClock", transform);
        ButtonBigText.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);

        for (displayedSecond = 30; displayedSecond > 0; displayedSecond--) {
            ButtonBigText.text = displayedSecond.ToString();
            yield return new WaitForSeconds(1.0f);
        }

        ButtonBigText.text = "0";
        if (enteredSecond == storedNumber) {
            Audio.PlaySoundAtTransform("missionControl_goodChime", transform);
            Debug.LogFormat("[Mission Control #{0}] You pressed at the correct time! One strike removed!", moduleId);

            // Code by Emik (currently bugged)
            var bomb = GetComponentInParent<KMBomb>();
            int strikes = bomb.GetStrikes();
            strikes -= 1;
            bomb.SetStrikes(strikes);
        }

        else {
            Audio.PlaySoundAtTransform("missionControl_badChime", transform);
            Debug.LogFormat("[Mission Control #{0}] You pressed the button at the wrong time.", moduleId);
            bombStrikes++;
        }

        storedNumber = ((int) storedTime) % 20;
        storedNumber = storedNumber == 0 ? 20 : storedNumber;
        Debug.LogFormat("[Mission Control #{0}] The number to press for the next countdown is {1}.", moduleId, storedNumber);

        yield return new WaitForSeconds(2.0f);
        freezeTimer = false;
        ButtonBigText.color = new Color(1.0f, 1.0f, 1.0f, 0.0f);
        ButtonBigText.text = actualStrikes.ToString();
        Debug.LogFormat("[Mission Control #{0}] The bomb currently has {1} internal strikes.", moduleId, actualStrikes);
        
        flickerText = true;
        transparency = 0.0f;
        StartCoroutine(FlickerTextRoutine());

        franticMode = false;
        StartCoroutine(ChangeBorderColor(true));

        while (!readyToChange) { }

        for (int i = 0; i < jamModule.Length; i++) {
            if (i == jamModule.Length - 1)
                StartCoroutine(RevealJamModule(i, true));

            else
                StartCoroutine(RevealJamModule(i, false));
        }

        yield return null;
    }


    // Lights turn on
    private void OnActivate() {
        if (missionFound) {
            if (canPlayIntro && Settings.IntroSound) {
                canPlayIntro = false;
                Audio.PlaySoundAtTransform("missionControl_inEffect", transform);
            }

            switch (mode) {
                case 5: // Precise Instability
                    StartCoroutine(InitJamBomb());
                    break;
                case 7: // Lost To Time
                    StartCoroutine(InitLostToTimeBomb());
                    break;
                case 8: // Flyer's Manual Curse
                    flickerText = true;
                    StartCoroutine(FlickerTextRoutine());
                    break;
                case 9: // The Father of the Abyss
                    StartCoroutine(FadeInBlack(10.0f, true));
                    break;
                case 11: // Command Prompt
                    InitCmdPrompt();
                    break;
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


    // Rotates the button so the texture animates
    private IEnumerator AnimateButton() {
        while (true) {
            if (franticMode)
                ButtonTransform.Rotate(new Vector3(0.25f, 0.0f, 0.0f));

            else
                ButtonTransform.Rotate(new Vector3(0.05f, 0.0f, 0.0f));
            yield return new WaitForSeconds(0.02f);
        }
    }

    // Makes the text fade in and out
    private IEnumerator FlickerTextRoutine() {
        while (flickerText) {
            transparency += 0.01f;
            transparency %= 2.0f;

            if (transparency > 1.0f) { // Down
                ButtonText.color = new Color(1.0f, 1.0f, 1.0f, 2.0f - transparency);
                ButtonBigText.color = new Color(1.0f, 1.0f, 1.0f, 2.0f - transparency);
            }

            else { // Up
                ButtonText.color = new Color(1.0f, 1.0f, 1.0f, transparency);
                ButtonBigText.color = new Color(1.0f, 1.0f, 1.0f, transparency);
            }

            yield return new WaitForSeconds(0.02f);
        }

        ButtonText.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        ButtonBigText.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
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


    // Fades in the vignette - code from Art Appreciation
    private IEnumerator FadeIn(float speed = 0.67f) {
        if (postProcess != null) {
            DestroyImmediate(postProcess);
        }

        postProcess = cameraPos.gameObject.AddComponent<CameraPostProcess>();
        postProcess.PostProcessMaterial = new Material(VignetteMaterial);

        for (float progress = 0.0f; progress < 1.0f; progress += Time.deltaTime * speed) {
            postProcess.Vignette = progress * 1.6f;
            postProcess.Grayscale = progress * 0.35f;

            yield return null;
        }

        postProcess.Vignette = 1.6f;
        postProcess.Grayscale = 0.35f;
    }

    // Fades out the vignette - code from Art Appreciation
    private IEnumerator FadeOut(float speed = 0.67f) {
        for (float progress = 1.0f - Time.deltaTime * speed; progress >= 0.0f; progress -= Time.deltaTime * speed) {
            postProcess.Vignette = progress * 1.6f;
            postProcess.Grayscale = progress * 0.35f;

            yield return null;
        }

        if (postProcess != null) {
            DestroyImmediate(postProcess);
            postProcess = null;
        }
    }

    // Makes the screen completely black
    private void SetBlackScreen() {
        if (postProcess != null) {
            DestroyImmediate(postProcess);
        }

        postProcess = cameraPos.gameObject.AddComponent<CameraPostProcess>();
        postProcess.PostProcessMaterial = new Material(VignetteMaterial);

        postProcess.Vignette = 10000.0f;
    }

    // Fades in the black screen
    private IEnumerator FadeInBlack(float amplifier, bool exponential, float speed = 1.0f) {
        if (postProcess != null) {
            DestroyImmediate(postProcess);
        }

        if (mode == 9)
            speed = 1.0f / abyssTime;

        postProcess = cameraPos.gameObject.AddComponent<CameraPostProcess>();
        postProcess.PostProcessMaterial = new Material(VignetteMaterial);

        if (exponential) {
            for (float progress = 0.0f; progress < 1.0f; progress += Time.deltaTime * speed) {
                postProcess.Vignette = (float)(Math.Pow(progress, 2) * amplifier);

                yield return null;
            }
        }

        else {
            for (float progress = 0.0f; progress < 1.0f; progress += Time.deltaTime * speed) {
                postProcess.Vignette = progress * amplifier;

                yield return null;
            }
        }

        postProcess.Vignette = amplifier;
    }

    // Fades out the black screen
    private IEnumerator FadeOutBlack(float amplifier, float speed = 1.0f) {
        for (float progress = 1.0f - Time.deltaTime * speed; progress >= 0.0f; progress -= Time.deltaTime * speed) {
            postProcess.Vignette = progress * amplifier;

            yield return null;
        }

        if (postProcess != null) {
            DestroyImmediate(postProcess);
            postProcess = null;
        }
    }


    // Reading the serial number
    private IEnumerator ReadSerialNumber() {
        string serialNumber = Bomb.GetSerialNumber();
        yield return new WaitForSeconds(4.0f);

        ButtonBigText.color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        for (int i = 0; i < serialNumber.Length; i++) {
            ButtonBigText.text = serialNumber[i].ToString();
            switch (serialNumber[i]) {
                case 'Z': Audio.PlaySoundAtTransform("missionControl_-26", transform); break;
                case 'Y': Audio.PlaySoundAtTransform("missionControl_-25", transform); break;
                case 'X': Audio.PlaySoundAtTransform("missionControl_-24", transform); break;
                case 'W': Audio.PlaySoundAtTransform("missionControl_-23", transform); break;
                case 'V': Audio.PlaySoundAtTransform("missionControl_-22", transform); break;
                case 'U': Audio.PlaySoundAtTransform("missionControl_-21", transform); break;
                case 'T': Audio.PlaySoundAtTransform("missionControl_-20", transform); break;
                case 'S': Audio.PlaySoundAtTransform("missionControl_-19", transform); break;
                case 'R': Audio.PlaySoundAtTransform("missionControl_-18", transform); break;
                case 'Q': Audio.PlaySoundAtTransform("missionControl_-17", transform); break;
                case 'P': Audio.PlaySoundAtTransform("missionControl_-16", transform); break;
                case 'O': Audio.PlaySoundAtTransform("missionControl_-15", transform); break;
                case 'N': Audio.PlaySoundAtTransform("missionControl_-14", transform); break;
                case 'M': Audio.PlaySoundAtTransform("missionControl_-13", transform); break;
                case 'L': Audio.PlaySoundAtTransform("missionControl_-12", transform); break;
                case 'K': Audio.PlaySoundAtTransform("missionControl_-11", transform); break;
                case 'J': Audio.PlaySoundAtTransform("missionControl_-10", transform); break;
                case 'I': Audio.PlaySoundAtTransform("missionControl_-09", transform); break;
                case 'H': Audio.PlaySoundAtTransform("missionControl_-08", transform); break;
                case 'G': Audio.PlaySoundAtTransform("missionControl_-07", transform); break;
                case 'F': Audio.PlaySoundAtTransform("missionControl_-06", transform); break;
                case 'E': Audio.PlaySoundAtTransform("missionControl_-05", transform); break;
                case 'D': Audio.PlaySoundAtTransform("missionControl_-04", transform); break;
                case 'C': Audio.PlaySoundAtTransform("missionControl_-03", transform); break;
                case 'B': Audio.PlaySoundAtTransform("missionControl_-02", transform); break;
                case 'A': Audio.PlaySoundAtTransform("missionControl_-01", transform); break;
                case '9': Audio.PlaySoundAtTransform("missionControl_-36", transform); break;
                case '8': Audio.PlaySoundAtTransform("missionControl_-35", transform); break;
                case '7': Audio.PlaySoundAtTransform("missionControl_-34", transform); break;
                case '6': Audio.PlaySoundAtTransform("missionControl_-33", transform); break;
                case '5': Audio.PlaySoundAtTransform("missionControl_-32", transform); break;
                case '4': Audio.PlaySoundAtTransform("missionControl_-31", transform); break;
                case '3': Audio.PlaySoundAtTransform("missionControl_-30", transform); break;
                case '2': Audio.PlaySoundAtTransform("missionControl_-29", transform); break;
                case '1': Audio.PlaySoundAtTransform("missionControl_-28", transform); break;
                default: Audio.PlaySoundAtTransform("missionControl_-27", transform); break;
            }

            yield return new WaitForSeconds(2.0f);
        }

        ButtonBigText.text = "";
        yield return new WaitForSeconds(2.0f);
        Solve();
        canPressButton = true;
    }


    // Button is pressed
    private void ButtonPressed() {
        ButtonSelectable.AddInteractionPunch(0.5f);

        // Precise Instability
        if (franticMode && !enteredTimerNumber) {
            enteredSecond = displayedSecond;
            enteredTimerNumber = true;
            Audio.PlaySoundAtTransform("missionControl_buttonPress", transform);
            Debug.LogFormat("[Mission Control #{0}] You pressed the button when the countdown displayed {1}.", moduleId, enteredSecond);
        }

        else if (franticMode) {
            Debug.LogFormat("[Mission Control #{0}] You pressed the button again during the countdown. Only the first press is registered.", moduleId);
        }

        // Wish
        else if (mode == 4 && canPressButton && !moduleSolved) {
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

        // For No Eyes Only
        else if (mode == 6 && canPressButton) {
            canPressButton = false;
            Audio.PlaySoundAtTransform("missionControl_buttonPress", transform);
            Audio.PlaySoundAtTransform("missionControl_edgeworkRead", transform);
            Debug.LogFormat("[Mission Control #{0}] You pressed the button. Reading the serial number now.", moduleId);
            StartCoroutine(ReadSerialNumber());
        }

        // The Mountain
        else if (mode == 10) {
            Solve();
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
    
    // Removes gimmick effects when the the bomb isn't active
    private void OnDestroy() {
        canPlayIntro = true;
        BorderMaterial.color = new Color(1.0f, BORDER_GREEN, 0.0f);
        PlanetMaterial.color = new Color(1.0f, BUTTON_GREEN, BUTTON_BLUE);

        if (postProcess != null) {
            postProcess.Vignette = 0.0f;
            postProcess.Grayscale = 0.0f;
            DestroyImmediate(postProcess);
            postProcess = null;
        }

        CloseDictationEngine();
    }

    // Module solves
    private void Solve() {
        if (!moduleSolved) {
            Debug.LogFormat("[Mission Control #{0}] Module solved!", moduleId);
            moduleSolved = true;
            Module.HandlePass();
        }
    }

    // Module strikes
    private void Strike() {
        Debug.LogFormat("[Mission Control #{0}] Strike!", moduleId);
        Module.HandleStrike();
    }


    // Variables set by Tweaks for Zen/Time mode detection
    #pragma warning disable 414
    private bool ZenModeActive;
    private bool TimeModeActive;
    private bool TwitchPlaysActive;
    #pragma warning restore 414

    // Twitch Plays command handler - by eXish
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press (##) [Presses the button (optionally when the seconds digits of the bomb's timer are '##')] | !{0} countdown <1-20> [Presses the button when the countdown timer is the specified number on Precise Instability]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*countdown\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
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
                    yield return "sendtochaterror!f The specified number '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                if (time < 1 || time > 20)
                {
                    yield return "sendtochaterror The specified number '" + parameters[1] + "' is out of range 1-20!";
                    yield break;
                }
                if (!franticMode)
                {
                    yield return "sendtochaterror The countdown timer is not currently active!";
                    yield break;
                }
                yield return null;
                while (time != displayedSecond) yield return "trycancel Halted waiting to press the button due to a cancel request.";
                ButtonSelectable.OnInteract();
            }
            yield break;
        }
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length == 1)
            {
                if (!canPressButton)
                {
                    yield return "sendtochaterror The button cannot be pressed right now!";
                    yield break;
                }
                yield return null;
                ButtonSelectable.OnInteract();
            }
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
                    yield return "sendtochaterror The button cannot be pressed right now!";
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
        switch (mode)
        {
            case 0:
            case 1:
            case 3:
            case 7:
            case 8:
            case 9:
            case 11:
                while (Bomb.GetSerialNumberNumbers().Sum() != Math.Floor(Bomb.GetTime()) % 60) yield return true;
                ButtonSelectable.OnInteract();
                break;
            case 2:
                while (!moduleSolved) yield return true;
                break;
            case 4:
                while (!moduleSolved)
                {
                    if (WISH_THRESHOLDS[buttonPresses] > Bomb.GetSolvedModuleNames().Count())
                        yield return true;
                    else
                    {
                        ButtonSelectable.OnInteract();
                        yield return new WaitForSeconds(.1f);
                    }
                }
                break;
            case 5:
                while (franticMode || Bomb.GetSerialNumberNumbers().Sum() != Math.Floor(Bomb.GetTime()) % 60) yield return true;
                ButtonSelectable.OnInteract();
                break;
            case 6:
                if (canPressButton)
                    ButtonSelectable.OnInteract();
                while (!moduleSolved) yield return true;
                break;
            case 10:
                ButtonSelectable.OnInteract();
                yield return new WaitForSeconds(.1f);
                break;
        }
    }
}