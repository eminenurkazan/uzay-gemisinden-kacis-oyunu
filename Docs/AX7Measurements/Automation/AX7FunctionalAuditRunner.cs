#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

[InitializeOnLoad]
public static class AX7FunctionalAuditEntry
{
    static AX7FunctionalAuditEntry()
    {
        EditorApplication.playModeStateChanged += state => {
            if (!SessionState.GetBool("AX7AuditActive", false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
                new GameObject("AX7FunctionalAuditRunner").AddComponent<AX7FunctionalAuditRunner>();
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                bool ok = SessionState.GetBool("AX7AuditPassed", false);
                SessionState.SetBool("AX7AuditActive", false);
                EditorApplication.Exit(ok ? 0 : 1);
            }
        };
        Application.logMessageReceived += (message, trace, type) => {
            if (trace.Contains("UnityEditor.Search.SearchDatabase")) return;
            if (SessionState.GetBool("AX7AuditActive", false)
                && (type == LogType.Error || type == LogType.Exception || message.Contains("Failed to create agent")))
                SessionState.SetInt("AX7AuditErrors", SessionState.GetInt("AX7AuditErrors", 0) + 1);
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/EscapeScene.unity");
        foreach (var intro in UnityEngine.Object.FindObjectsByType<EscapeIntroView>(FindObjectsSortMode.None))
            intro.gameObject.SetActive(false);
        foreach (var hud in UnityEngine.Object.FindObjectsByType<CompanionHUD>(FindObjectsSortMode.None)) hud.enabled = false;
        foreach (var input in UnityEngine.Object.FindObjectsByType<AstronautController>(FindObjectsSortMode.None)) input.enabled = false;
        foreach (var input in UnityEngine.Object.FindObjectsByType<CompanionCommandInput>(FindObjectsSortMode.None)) input.enabled = false;
        var obstacle = Array.Find(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects(), go => go.name == "SensorTestEngeli");
        if (obstacle != null) obstacle.SetActive(false);
        SessionState.SetBool("AX7AuditActive", true);
        SessionState.SetBool("AX7AuditPassed", false);
        SessionState.SetInt("AX7AuditErrors", 0);
        EditorApplication.EnterPlaymode();
    }
}

public sealed class AX7FunctionalAuditRunner : MonoBehaviour
{
    private readonly List<string> results = new List<string>();
    private AX7Brain brain;
    private AX7FollowPlayer follow;
    private AX7Farewell farewell;
    private Transform player;
    private CharacterController body;
    private int taskStarts;
    private int successCount;
    private int doorOpenCount;
    private bool crossedDoor;
    private bool finished;
    private float deadline;
    private const string Output = "AX7-functional-audit.txt";

    private void Start()
    {
        deadline = Time.realtimeSinceStartup + 180f;
        brain = GameObject.Find("AX-7").GetComponent<AX7Brain>();
        follow = brain.GetComponent<AX7FollowPlayer>();
        farewell = brain.GetComponent<AX7Farewell>();
        player = GameObject.Find("Astronot").transform;
        body = player.GetComponent<CharacterController>();
        brain.StateChanged += state => { if (state == AX7State.MovingToPanel) taskStarts++; if (state == AX7State.Succeeded) successCount++; };
        Application.logMessageReceived += CaptureLog;
        StartCoroutine(Drive());
    }
    private void CaptureLog(string message, string trace, LogType type)
    {
        if (message == "Fiziksel kapı tamamen açıldı.") doorOpenCount++;
    }
    private void Update()
    {
        if (!finished && Time.realtimeSinceStartup > deadline) Finish(false, "Global timeout");
    }
    private void MovePlayer(Vector3 position)
    {
        body.enabled = false;
        player.position = position;
        body.enabled = true;
        Physics.SyncTransforms();
        Debug.Log("AX7_AUDIT_FIXTURE player=" + position);
    }
    private void Check(bool value, string label)
    {
        results.Add((value ? "PASS " : "FAIL ") + label);
        File.WriteAllLines(Output, results);
        Debug.Log("AX7_AUDIT " + results[results.Count-1]);
        if (!value) throw new Exception(label);
    }
    private IEnumerator Until(Func<bool> predicate, float seconds, string label)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (!predicate() && Time.realtimeSinceStartup < end) yield return null;
        Check(predicate(), label);
    }
    private IEnumerator Drive()
    {
        var work = Run();
        while (true)
        {
            object next = null;
            bool hasNext = false;
            try { hasNext = work.MoveNext(); if (hasNext) next = work.Current; }
            catch (Exception ex) { Finish(false, ex.Message); yield break; }
            if (!hasNext) break;
            // Run nested wait enumerators here so their assertions are caught too.
            if (next is IEnumerator nested)
            {
                while (true)
                {
                    bool more = false;
                    object item = null;
                    try { more = nested.MoveNext(); if (more) item = nested.Current; }
                    catch (Exception ex) { Finish(false, ex.Message); yield break; }
                    if (!more) break;
                    yield return item;
                }
            }
            else yield return next;
        }
        Finish(true, "All functional checks completed");
    }
    private IEnumerator Run()
    {
        for (int i = 0; i < 8; i++) yield return null;
        var robotAgent = brain.GetComponent<NavMeshAgent>();
        var kael = GameObject.Find("Kael");
        Check(robotAgent.enabled && robotAgent.isOnNavMesh && kael.GetComponent<NavMeshAgent>().isOnNavMesh, "AX-7 and Kael startup bound to typed NavMesh");
        Check(SessionState.GetInt("AX7AuditErrors",0) == 0, "No startup agent warnings/errors");
        Check(brain.CurrentState == AX7State.Waiting, "Robot waits before command");
        var command = player.GetComponent<CompanionCommandInput>();
        Check(command.CanIssueCommand, "Command eligible when player is nearby and robot idle");

        brain.MoveToPanel();
        Check(brain.CurrentState == AX7State.MovingToPanel && !command.CanIssueCommand, "Busy task rejects input eligibility");
        for (int i = 0; i < 20; i++) { brain.MoveToPanel(); yield return null; }
        Check(taskStarts == 1 && brain.CurrentState != AX7State.Failed, "Twenty repeated task calls do not restart or fail running task");
        yield return Until(() => brain.CurrentState == AX7State.Succeeded || brain.CurrentState == AX7State.Failed, 20f, "Panel task reaches terminal state");
        Check(brain.CurrentState == AX7State.Succeeded && GameObject.Find("FizikselKapi").GetComponent<DoorController>().IsOpen, "Actual panel task opens physical door");
        for (int i = 0; i < 10; i++) brain.MoveToPanel();
        Check(taskStarts == 1 && successCount == 1 && doorOpenCount == 1, "Completed task is not reissued and door opens once");

        MovePlayer(new Vector3(0f,1f,-8f));
        float lastZ = brain.transform.position.z;
        float end = Time.realtimeSinceStartup + 20f;
        while (Time.realtimeSinceStartup < end && follow.CurrentState != AX7FollowState.WaitingNearPlayer)
        {
            Vector3 pos = brain.transform.position;
            if (lastZ > -4f && pos.z <= -4f && Mathf.Abs(pos.x) < 2f) crossedDoor = true;
            lastZ = pos.z;
            yield return null;
        }
        Check(follow.CurrentState == AX7FollowState.WaitingNearPlayer, "Follows player back to first room and stops nearby");
        Check(crossedDoor, "Return route crosses open physical doorway rather than maintenance passage");
        yield return new WaitForSeconds(.4f);
        Vector3 stopped = brain.transform.position;
        yield return new WaitForSeconds(.6f);
        Debug.Log("AX7_AUDIT_STOP delta=" + Vector3.Distance(stopped, brain.transform.position) + " from=" + stopped + " to=" + brain.transform.position + " state=" + follow.CurrentState);
        Check(Vector3.Distance(stopped, brain.transform.position) < .06f && follow.CurrentState == AX7FollowState.WaitingNearPlayer, "Robot stays stopped near stationary player after settling");
        MovePlayer(new Vector3(0f,1f,-11f));
        yield return Until(() => follow.CurrentState == AX7FollowState.Following, 3f, "Following resumes when player moves away");
        yield return Until(() => follow.CurrentState == AX7FollowState.WaitingNearPlayer, 15f, "Following stops again at near distance");

        var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = "AuditFollowObstacle";
        Vector3 robot = brain.transform.position;
        obstacle.transform.position = new Vector3(robot.x,.4f,robot.z+.7f);
        obstacle.transform.localScale = new Vector3(1.2f,.8f,.25f);
        MovePlayer(new Vector3(0f,1f,1f));
        Physics.SyncTransforms();
        yield return Until(() => follow.CurrentState == AX7FollowState.WaitingForObstacle, 4f, "Raycast obstacle pauses following");
        yield return new WaitForSeconds(.3f);
        stopped = brain.transform.position;
        yield return new WaitForSeconds(.4f);
        Debug.Log("AX7_AUDIT_BLOCKED delta=" + Vector3.Distance(stopped, brain.transform.position) + " from=" + stopped + " to=" + brain.transform.position + " state=" + follow.CurrentState);
        Check(Vector3.Distance(stopped, brain.transform.position) < .06f && follow.CurrentState == AX7FollowState.WaitingForObstacle, "Blocked follower remains stopped after settling");
        obstacle.SetActive(false);
        Physics.SyncTransforms();
        yield return Until(() => follow.CurrentState == AX7FollowState.Following, 4f, "Following resumes after obstacle removal");
        Destroy(obstacle);
        yield return Until(() => follow.CurrentState == AX7FollowState.WaitingNearPlayer, 20f, "Robot reaches player in security room");

        // Explicit editor mechanics fixture: no LLM request or fabricated model response.
        var gate = GameObject.Find("BiyometrikKapi").GetComponent<BiometricDoorLink>();
        gate.OpenForEditorTest();
        yield return Until(() => gate.CurrentState == BiometricDoorState.Open, 10f, "Editor fixture opens biometric door after completed physical task");
        MovePlayer(new Vector3(-1f,1f,10.8f));
        var target = kael.GetComponent<KaelAtmosphereResponse>();
        yield return Until(() => target.IsFullyInsideAirlock, 25f, "Kael follows into airlock with entire body inside");
        var panel = FindFirstObjectByType<AtmosphereControlPanel>();
        Check(panel.CanActivate(player) && panel.TryActivate(player), "Actual atmosphere panel accepts authorized activation");
        yield return Until(() => target.IsIncapacitated, 7f, "Atmosphere event incapacitates Kael");
        yield return Until(() => farewell.HasStarted && !follow.enabled, 3f, "Farewell takes control and disables following");
        yield return Until(() => farewell.IsWaitingAtExit, 30f, "AX-7 reaches farewell point with real NavMesh and fallen-body obstacle");
        yield return new WaitForSeconds(.4f);
        stopped = brain.transform.position;
        MovePlayer(new Vector3(0f,1f,12.1f));
        var ship = FindFirstObjectByType<ShipAccessController>();
        Check(ship.TryOpenHatch(player), "Ship transfer hatch opens after real atmosphere prerequisites");
        yield return Until(() => ship.CurrentState == ShipAccessState.Open, 10f, "Ship transfer hatch fully open");
        MovePlayer(new Vector3(0f,1f,16.5f));
        yield return new WaitForSecondsRealtime(2f);
        Debug.Log("AX7_AUDIT_FAREWELL inside=" + ship.IsPlayerInside + " waiting=" + farewell.IsWaitingAtExit + " followingEnabled=" + follow.enabled + " delta=" + Vector3.Distance(stopped,brain.transform.position));
        Check(ship.IsPlayerInside && farewell.IsWaitingAtExit && !follow.enabled
            && Vector3.Distance(stopped, brain.transform.position) < .06f, "AX-7 stays outside while player is inside own ship");
        var filter = new NavMeshQueryFilter { agentTypeID = robotAgent.agentTypeID, areaMask = robotAgent.areaMask };
        Check(!NavMesh.SamplePosition(new Vector3(0f,.025f,16.5f),out _, .2f,filter), "Own ship cabin excluded from AX-7 NavMesh");
        Check(SessionState.GetInt("AX7AuditErrors",0) == 0, "No runtime errors or native agent warnings during functional run");
    }
    private void Finish(bool ok, string reason)
    {
        if (finished) return;
        finished = true;
        results.Add("FINAL " + (ok ? "PASS " : "FAIL ") + reason);
        File.WriteAllLines(Output, results);
        Debug.Log("AX7_AUDIT_FINISHED " + ok + " " + reason);
        SessionState.SetBool("AX7AuditPassed", ok);
        EditorApplication.isPlaying = false;
    }
    private void OnDestroy() => Application.logMessageReceived -= CaptureLog;
}
#endif
