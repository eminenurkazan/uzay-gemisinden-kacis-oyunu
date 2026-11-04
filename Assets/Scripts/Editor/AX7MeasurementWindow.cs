using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// Observes manual experiments. Never starts a task, moves an actor or changes navigation/sensor settings.
public sealed class AX7MeasurementWindow : EditorWindow
{
    private static readonly string[] Cases = { "A1 — Normal başlangıç", "A2 — Alternatif başlangıç", "B1 — Geçici engel", "B2 — Kalıcı engel" };
    private static readonly string[] Codes = { "A1", "A2", "B1", "B2" };
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private const string Header = "trial_id,scenario,started_local,scene,start_x,start_y,start_z,previous_state,end_state,expected_state,task_succeeded,expected_result_met,duration_game_s,duration_wall_s,obstacle_wait_game_s,obstacle_stop_count,arrival_game_s,panel_start_game_s,door_closed_at_start,door_open_at_end,test_obstacle_at_start,agent_speed,agent_radius,agent_height,obstacle_timeout_s,time_scale_start,paused_during_trial,valid_for_stats,invalid_reason,failure_reason,state_sequence,git_commit,notes";

    [SerializeField] private int selectedCase;
    [SerializeField] private int sequence;
    [SerializeField] private string notes = "";
    [SerializeField] private string sessionPath = "";
    [SerializeField] private string lastMessage = "Play'i başlat; ardından Game alanında AX-7'ye E ile komut ver.";
    [SerializeField] private string pendingLine = "";
    [SerializeField] private string pendingPath = "";
    private AX7Brain brain;
    private bool recording;
    private bool paused;
    private bool timeScaleChanged;
    private int activeCase;
    private int obstacleStops;
    private double startedGame, startedWall, waitStarted = -1, waitTotal, arrival = -1, panelStart = -1;
    private string trialId, startedLocal, previousState, commit, activeNotes;
    private Vector3 startPosition;
    private float speed, radius, height, obstacleTimeout, initialTimeScale;
    private bool doorClosed, testObstacleEnabled;
    private readonly List<string> states = new List<string>();

    [MenuItem("Uzay Oyunu/AX-7 Ölçüm Penceresi")]
    public static void Open() => GetWindow<AX7MeasurementWindow>("AX-7 Ölçümleri");

    private void OnEnable()
    {
        minSize = new Vector2(390, 360);
        EditorApplication.playModeStateChanged += PlayModeChanged;
        EditorApplication.pauseStateChanged += PauseChanged;
        EditorApplication.delayCall += TryAttach;
    }

    private void OnDisable()
    {
        if (recording) Finish("Interrupted", "Ölçüm penceresi kapandı veya kod yeniden yüklendi.");
        Detach();
        EditorApplication.playModeStateChanged -= PlayModeChanged;
        EditorApplication.pauseStateChanged -= PauseChanged;
        EditorApplication.delayCall -= TryAttach;
    }

    private void PlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode) TryAttach();
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            if (recording) Finish("Interrupted", "Görev bitmeden Play durduruldu.");
            Detach();
        }
    }

    private void PauseChanged(PauseState state)
    {
        if (recording && state == PauseState.Paused) paused = true;
    }

    private void TryAttach()
    {
        if (!EditorApplication.isPlaying || brain != null) return;
        var candidates = UnityEngine.Object.FindObjectsByType<AX7Brain>(FindObjectsSortMode.None)
            .Where(b => b.gameObject.scene.name == "EscapeScene" && b.isActiveAndEnabled).ToArray();
        if (candidates.Length != 1)
        {
            lastMessage = "Play modunda EscapeScene içinde tek bir etkin AX-7 bulunmalı.";
            return;
        }
        brain = candidates[0];
        brain.MeasurementStarted += Started;
        brain.MeasurementFinished += Finished;
        brain.StateChanged += StateChanged;
        lastMessage = "Kayıt hazır. Game alanına tıkla; AX-7'ye yaklaş ve E'ye bas.";
    }

    private void Detach()
    {
        if (brain != null)
        {
            brain.MeasurementStarted -= Started;
            brain.MeasurementFinished -= Finished;
            brain.StateChanged -= StateChanged;
        }
        brain = null;
    }

    private void OnInspectorUpdate()
    {
        if (recording && !Mathf.Approximately(Time.timeScale, initialTimeScale)) timeScaleChanged = true;
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("AX-7 görev ölçümleri", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Bu pencere açıkken E ile kabul edilen görevler otomatik kaydedilir. Her bağımsız deneme için Play'i yeniden başlat. Kael ile konuşman gerekmez.", MessageType.Info);
        using (new EditorGUI.DisabledScope(recording))
        {
            selectedCase = EditorGUILayout.Popup("Test", selectedCase, Cases);
            EditorGUILayout.LabelField("Deneme notu (isteğe bağlı)");
            notes = EditorGUILayout.TextArea(notes, GUILayout.Height(40));
        }
        string setup = selectedCase == 1
            ? "A2: X=0, Z=-10; Y yaklaşık 0,025. Test engeli kapalı."
            : selectedCase >= 2 ? "B: Normal başlangıç X=1,5, Z=-12. SensorTestEngeli açık."
            : "A1: Normal başlangıç X=1,5, Z=-12. SensorTestEngeli kapalı.";
        EditorGUILayout.HelpBox(setup + "\nBu pencere başlangıç konumunu veya engeli değiştirmez.", MessageType.None);
        if (selectedCase == 2) EditorGUILayout.LabelField("Engel algılandıktan sonra 5 oyun saniyesi bekle; engeli kaldır.", EditorStyles.wordWrappedLabel);
        if (selectedCase == 3) EditorGUILayout.LabelField("Engeli kaldırma; yaklaşık 20 saniyelik bekleme sonrasında başarısızlık beklenir.", EditorStyles.wordWrappedLabel);
        if (recording)
        {
            EditorGUILayout.LabelField("Ölçülen deneme", trialId);
            EditorGUILayout.LabelField("Görev süresi", Seconds(Time.timeAsDouble - startedGame) + " sn");
            EditorGUILayout.LabelField("Engelde bekleme", Seconds(waitTotal + (waitStarted >= 0 ? Time.timeAsDouble - waitStarted : 0)) + " sn");
        }
        EditorGUILayout.HelpBox(lastMessage, MessageType.None);
        if (!string.IsNullOrEmpty(pendingLine) && GUILayout.Button("Yazılamayan kaydı yeniden dene")) WritePending();
        if (GUILayout.Button("Kayıt klasörünü aç"))
        {
            string directory = ResultsDirectory;
            Directory.CreateDirectory(directory);
            EditorUtility.RevealInFinder(directory);
        }
        if (!string.IsNullOrEmpty(sessionPath)) EditorGUILayout.SelectableLabel(sessionPath, EditorStyles.wordWrappedLabel, GUILayout.Height(40));
    }

    private static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
    private static string ResultsDirectory => Path.Combine(ProjectRoot, "Docs", "AX7Measurements", "Raw");
    private static GameObject Root(AX7Brain target, string name) => target.gameObject.scene.GetRootGameObjects().FirstOrDefault(go => go.name == name);

    private void Started()
    {
        if (!string.IsNullOrEmpty(pendingLine))
        {
            lastMessage = "Önce yazılamayan önceki kaydı yeniden dene. Bu görev ölçülmedi.";
            return;
        }
        if (recording) Finish("Interrupted", "Yeni bir kabul edilmiş görev önceki kaydı kesti.");
        activeCase = selectedCase;
        sequence++;
        trialId = Codes[activeCase] + "-" + sequence.ToString("D2", Invariant);
        startedGame = Time.timeAsDouble;
        startedWall = EditorApplication.timeSinceStartup;
        startedLocal = DateTimeOffset.Now.ToString("o", Invariant);
        previousState = brain.CurrentState.ToString();
        startPosition = brain.transform.position;
        var agent = brain.GetComponent<NavMeshAgent>();
        speed = agent != null ? agent.speed : 0;
        radius = agent != null ? agent.radius : 0;
        height = agent != null ? agent.height : 0;
        obstacleTimeout = new SerializedObject(brain).FindProperty("obstacleWaitTimeout").floatValue;
        var door = Root(brain, "FizikselKapi");
        doorClosed = door != null && door.GetComponent<DoorController>() != null && door.GetComponent<DoorController>().IsClosed;
        var obstacle = Root(brain, "SensorTestEngeli");
        testObstacleEnabled = obstacle != null && obstacle.activeInHierarchy;
        initialTimeScale = Time.timeScale;
        activeNotes = notes;
        commit = ReadCommit();
        waitStarted = -1; waitTotal = 0; arrival = -1; panelStart = -1; obstacleStops = 0;
        paused = EditorApplication.isPaused; timeScaleChanged = false;
        states.Clear(); states.Add(previousState);
        if (string.IsNullOrEmpty(sessionPath)) sessionPath = Path.Combine(ResultsDirectory, "ax7-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", Invariant) + ".csv");
        recording = true;
        lastMessage = trialId + " ölçülüyor. Görev bitene kadar Play'i durdurma.";
    }

    private void StateChanged(AX7State state)
    {
        if (!recording) return;
        double now = Time.timeAsDouble;
        if (waitStarted >= 0 && state != AX7State.WaitingForObstacle)
        {
            waitTotal += now - waitStarted;
            waitStarted = -1;
        }
        if (state == AX7State.WaitingForObstacle && waitStarted < 0)
        {
            waitStarted = now;
            obstacleStops++;
        }
        if (state == AX7State.AtPanel && arrival < 0) arrival = now - startedGame;
        if (state == AX7State.InteractingWithPanel && panelStart < 0) panelStart = now - startedGame;
        states.Add(state.ToString());
    }

    private void Finished(AX7State state, string reason) => Finish(state.ToString(), reason);

    private void Finish(string endState, string reason)
    {
        if (!recording) return;
        recording = false;
        double gameSeconds = Time.timeAsDouble - startedGame;
        double wallSeconds = EditorApplication.timeSinceStartup - startedWall;
        if (waitStarted >= 0) waitTotal += Time.timeAsDouble - waitStarted;
        waitStarted = -1;
        var door = brain != null ? Root(brain, "FizikselKapi") : null;
        bool doorOpen = door != null && door.GetComponent<DoorController>() != null && door.GetComponent<DoorController>().IsOpen;
        bool succeeded = endState == "Succeeded" && doorOpen;
        string expected = activeCase == 3 ? "Failed:ObstacleTimeout" : "Succeeded";
        bool expectedMet = activeCase == 3
            ? endState == "Failed" && obstacleStops > 0 && waitTotal >= obstacleTimeout - .1 && !doorOpen && (reason ?? "").Contains("engel uzun süre")
            : succeeded && (activeCase != 2 || obstacleStops > 0);
        var invalid = new List<string>();
        if (endState == "Interrupted") invalid.Add("interrupted");
        if (paused) invalid.Add("editor_paused");
        if (timeScaleChanged || !Mathf.Approximately(initialTimeScale, 1f) || !Mathf.Approximately(Time.timeScale, 1f)) invalid.Add("time_scale_changed");
        if (!doorClosed) invalid.Add("door_not_closed_at_start");
        if (previousState != "Waiting") invalid.Add("not_a_fresh_play");
        float x = activeCase == 1 ? 0 : 1.5f;
        float z = activeCase == 1 ? -10 : -12;
        if (Mathf.Abs(startPosition.x - x) > .15f || Mathf.Abs(startPosition.z - z) > .15f) invalid.Add("start_position_mismatch");
        if (testObstacleEnabled != (activeCase >= 2)) invalid.Add("test_obstacle_setup_mismatch");
        if (activeCase >= 2 && obstacleStops == 0) invalid.Add("sensor_wait_not_observed");
        var row = new[] { trialId, Codes[activeCase], startedLocal, brain != null ? brain.gameObject.scene.path : "EscapeScene", Number(startPosition.x), Number(startPosition.y), Number(startPosition.z), previousState, endState, expected,
            succeeded ? "1" : "0", expectedMet ? "1" : "0", Seconds(gameSeconds), Seconds(wallSeconds), Seconds(waitTotal), obstacleStops.ToString(Invariant), arrival >= 0 ? Seconds(arrival) : "", panelStart >= 0 ? Seconds(panelStart) : "",
            doorClosed ? "1" : "0", doorOpen ? "1" : "0", testObstacleEnabled ? "1" : "0", Number(speed), Number(radius), Number(height), Number(obstacleTimeout), Number(initialTimeScale), paused ? "1" : "0", invalid.Count == 0 ? "1" : "0", string.Join(";", invalid), reason ?? "", string.Join(">", states), commit, activeNotes };
        pendingLine = string.Join(",", row.Select(Csv));
        pendingPath = sessionPath;
        lastMessage = trialId + ": " + endState + ", " + Seconds(gameSeconds) + " sn. " + (invalid.Count == 0 ? "Ölçüm geçerli." : "İstatistik dışı: " + string.Join(", ", invalid));
        if (!string.IsNullOrEmpty(reason)) lastMessage += "\n" + reason;
        WritePending();
        Debug.Log("AX-7 ÖLÇÜM — " + lastMessage);
    }

    private void WritePending()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(pendingPath));
            bool empty = !File.Exists(pendingPath) || new FileInfo(pendingPath).Length == 0;
            File.AppendAllText(pendingPath, (empty ? Header + "\n" : "") + pendingLine + "\n", new UTF8Encoding(false));
            pendingLine = ""; pendingPath = "";
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            lastMessage += "\nDosyaya yazılamadı; 'Yazılamayan kaydı yeniden dene' düğmesini kullan. " + ex.Message;
        }
    }

    private static string ReadCommit()
    {
        try
        {
            string git = Path.Combine(ProjectRoot, ".git");
            string head = File.ReadAllText(Path.Combine(git, "HEAD")).Trim();
            if (!head.StartsWith("ref: ", StringComparison.Ordinal)) return head;
            string reference = head.Substring(5);
            string path = Path.Combine(git, reference.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path)) return File.ReadAllText(path).Trim();
            string packed = Path.Combine(git, "packed-refs");
            if (File.Exists(packed)) return File.ReadLines(packed).FirstOrDefault(line => line.EndsWith(" " + reference, StringComparison.Ordinal))?.Split(' ')[0] ?? "unknown";
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
        return "unknown";
    }

    private static string Csv(string value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
    private static string Seconds(double value) => value.ToString("0.000", Invariant);
    private static string Number(float value) => value.ToString("0.#####", Invariant);
}
