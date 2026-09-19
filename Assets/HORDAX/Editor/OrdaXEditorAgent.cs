#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using HORDAX.Core;
using HORDAX.Enemies;

namespace HORDAX.EditorTools
{
    [InitializeOnLoad]
    public static class OrdaXEditorAgent
    {
        private const string ProtocolVersion = "1";
        private const string PrototypeScenePath = "Assets/HORDAX/Scenes/Prototype.unity";
        private const string CaptureActiveKey = "HORDAX_ORDAX_AGENT_CAPTURE_ACTIVE";
        private const string CaptureExitPendingKey = "HORDAX_ORDAX_AGENT_CAPTURE_EXIT_PENDING";
        private const string CaptureResponsePathKey = "HORDAX_ORDAX_AGENT_CAPTURE_RESPONSE";
        private const string CaptureCommandIdKey = "HORDAX_ORDAX_AGENT_CAPTURE_COMMAND";
        private const string CaptureOutputKey = "HORDAX_ORDAX_AGENT_CAPTURE_OUTPUT";
        private const string CapturePreviousSceneKey = "HORDAX_ORDAX_AGENT_CAPTURE_PREVIOUS_SCENE";
        private const string CaptureWidthKey = "HORDAX_ORDAX_AGENT_CAPTURE_WIDTH";
        private const string CaptureHeightKey = "HORDAX_ORDAX_AGENT_CAPTURE_HEIGHT";
        private const string CaptureWarmupKey = "HORDAX_ORDAX_AGENT_CAPTURE_WARMUP";
        private const string CaptureWarmupSecondsKey = "HORDAX_ORDAX_AGENT_CAPTURE_WARMUP_SECONDS";
        private const string CaptureStartGameTimeKey = "HORDAX_ORDAX_AGENT_CAPTURE_START_GAME_TIME";
        private const string CaptureFrameKey = "HORDAX_ORDAX_AGENT_CAPTURE_FRAME";
        private const string CaptureTimeScaleKey = "HORDAX_ORDAX_AGENT_CAPTURE_TIME_SCALE";
        private const string CaptureOnTerminalKey = "HORDAX_ORDAX_AGENT_CAPTURE_ON_TERMINAL";
        private const string CaptureOnBossKey = "HORDAX_ORDAX_AGENT_CAPTURE_ON_BOSS";

        private static double nextPollAt;

        static OrdaXEditorAgent()
        {
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EnsureDirectories();
            WritePresence();
        }

        [Serializable]
        private sealed class AgentCommand
        {
            public string id;
            public string action;
            public string outputPath;
            public int width = 1280;
            public int height = 720;
            public int warmupFrames = 120;
            public float warmupSeconds = 0f;
            public float timeScale = 1f;
            public bool captureOnTerminal = true;
            public bool captureOnBoss = false;
        }

        [Serializable]
        private sealed class AgentResponse
        {
            public string id;
            public bool ok;
            public string summary;
            public string unityVersion;
            public bool compiling;
            public bool playing;
            public int errorCount;
            public int warningCount;
            public string[] validationErrors;
            public string[] validationWarnings;
            public string artifact;
            public string snapshotPath;
        }

        private static string ProjectRoot =>
            Directory.GetParent(Application.dataPath)?.FullName ?? Environment.CurrentDirectory;

        private static string Root => Path.Combine(ProjectRoot, "Library", "OrdaXAgent");
        private static string Inbox => Path.Combine(Root, "inbox");
        private static string Responses => Path.Combine(Root, "responses");
        private static string PresencePath => Path.Combine(Root, "editor-presence.json");

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory(Inbox);
            Directory.CreateDirectory(Responses);
        }

        private static void WritePresence()
        {
            try
            {
                AgentResponse response = new AgentResponse
                {
                    id = "presence",
                    ok = true,
                    summary = "HORDAX Unity Editor companion v" + ProtocolVersion + " ready",
                    unityVersion = Application.unityVersion,
                    compiling = EditorApplication.isCompiling,
                    playing = EditorApplication.isPlaying
                };
                File.WriteAllText(PresencePath, JsonUtility.ToJson(response, true));
            }
            catch
            {
                // Presence is best effort. Command responses remain authoritative.
            }
        }

        private static void Tick()
        {
            if (SessionState.GetBool(CaptureActiveKey, false))
            {
                TickCapture();
                return;
            }

            if (SessionState.GetBool(CaptureExitPendingKey, false))
            {
                FinishCapture();
                return;
            }

            if (EditorApplication.timeSinceStartup < nextPollAt)
                return;

            nextPollAt = EditorApplication.timeSinceStartup + 0.35d;
            WritePresence();

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
                return;

            string[] commands;
            try
            {
                EnsureDirectories();
                commands = Directory.GetFiles(Inbox, "*.json");
                Array.Sort(commands, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return;
            }

            if (commands.Length == 0)
                return;

            ProcessCommand(commands[0]);
        }

        private static void ProcessCommand(string commandPath)
        {
            AgentCommand command = null;
            try
            {
                string json = File.ReadAllText(commandPath);
                command = JsonUtility.FromJson<AgentCommand>(json);
                if (command == null || string.IsNullOrWhiteSpace(command.id))
                    throw new InvalidOperationException("Invalid OrdaX editor-agent command.");

                File.Delete(commandPath);

                switch (command.action)
                {
                    case "health":
                        WriteResponse(command, true, "Unity Editor companion ready");
                        break;

                    case "validate":
                        Validate(command);
                        break;

                    case "capture":
                        BeginCapture(command);
                        break;

                    case "play_start":
                        StartPlay(command);
                        break;

                    case "play_stop":
                        StopPlay(command);
                        break;

                    default:
                        WriteResponse(command, false, "Unsupported Unity editor-agent action: " + command.action);
                        break;
                }
            }
            catch (Exception error)
            {
                try
                {
                    if (File.Exists(commandPath))
                        File.Delete(commandPath);
                }
                catch { }

                if (command != null)
                    WriteResponse(command, false, error.GetType().Name + ": " + error.Message);
                else
                    Debug.LogException(error);
            }
        }

        private static void Validate(AgentCommand command)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                WriteResponse(command, false, "Unity Editor is compiling or importing.");
                return;
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ProjectValidator.ValidationReport report = ProjectValidator.RunValidation();
            ProjectValidator.LogReport(report);

            AgentResponse response = BaseResponse(command);
            response.ok = report.IsValid;
            response.summary = report.IsValid
                ? "Unity Editor validation passed."
                : $"Validation failed with {report.ErrorCount} error(s).";
            response.errorCount = report.ErrorCount;
            response.warningCount = report.WarningCount;
            response.validationErrors = new List<string>(report.Errors).ToArray();
            response.validationWarnings = new List<string>(report.Warnings).ToArray();
            SaveResponse(response);
        }

        private static void StartPlay(AgentCommand command)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                WriteResponse(command, false, "Unity Editor is compiling or importing.");
                return;
            }

            if (EditorApplication.isPlaying)
            {
                WriteResponse(command, true, "Unity Editor is already in Play Mode.");
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                WriteResponse(command, false, "Unity Editor is already changing Play Mode state.");
                return;
            }

            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.path != PrototypeScenePath && active.isDirty)
            {
                WriteResponse(
                    command,
                    false,
                    "Play Mode start deferred because the active Unity scene has unsaved changes.");
                return;
            }

            if (active.path != PrototypeScenePath)
            {
                if (!File.Exists(Path.Combine(ProjectRoot, PrototypeScenePath)))
                {
                    WriteResponse(command, false, "Prototype scene is missing.");
                    return;
                }

                EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
            }

            WriteResponse(command, true, "Play Mode start requested.");
            EditorApplication.EnterPlaymode();
        }

        private static void StopPlay(AgentCommand command)
        {
            if (!EditorApplication.isPlaying &&
                !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                WriteResponse(command, true, "Unity Editor is already outside Play Mode.");
                return;
            }

            WriteResponse(command, true, "Play Mode stop requested.");
            Time.timeScale = 1f;
            EditorApplication.ExitPlaymode();
        }

        private static void BeginCapture(AgentCommand command)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                WriteResponse(command, false, "Unity Editor is compiling or importing.");
                return;
            }

            if (EditorApplication.isPlaying)
            {
                string liveOutput = ResolveCaptureOutput(command);
                try
                {
                    AutomationCapture.CaptureCameraTo(
                        liveOutput,
                        Mathf.Clamp(command.width <= 0 ? 1280 : command.width, 320, 3840),
                        Mathf.Clamp(command.height <= 0 ? 720 : command.height, 180, 2160));

                    AgentResponse liveResponse = BaseResponse(command);
                    liveResponse.ok = true;
                    liveResponse.summary = "Captured current Unity Play Mode without interrupting it.";
                    liveResponse.artifact = liveOutput;
                    liveResponse.snapshotPath = Path.ChangeExtension(liveOutput, ".json");
                    SaveResponse(liveResponse);
                }
                catch (Exception error)
                {
                    WriteResponse(
                        command,
                        false,
                        error.GetType().Name + ": " + error.Message);
                }
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                WriteResponse(command, false, "Unity Editor is changing Play Mode state.");
                return;
            }

            Scene active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.path != PrototypeScenePath && active.isDirty)
            {
                WriteResponse(
                    command,
                    false,
                    "Capture deferred because the active Unity scene has unsaved changes.");
                return;
            }

            string previousScene = active.IsValid() ? active.path : string.Empty;
            if (active.path != PrototypeScenePath)
            {
                if (!File.Exists(Path.Combine(ProjectRoot, PrototypeScenePath)))
                {
                    WriteResponse(command, false, "Prototype scene is missing.");
                    return;
                }

                EditorSceneManager.OpenScene(PrototypeScenePath, OpenSceneMode.Single);
            }

            string output = ResolveCaptureOutput(command);

            SessionState.SetString(CaptureCommandIdKey, command.id);
            SessionState.SetString(CaptureResponsePathKey, ResponsePath(command.id));
            SessionState.SetString(CaptureOutputKey, output);
            SessionState.SetString(CapturePreviousSceneKey, previousScene ?? string.Empty);
            SessionState.SetInt(CaptureWidthKey, Mathf.Clamp(command.width <= 0 ? 1280 : command.width, 320, 3840));
            SessionState.SetInt(CaptureHeightKey, Mathf.Clamp(command.height <= 0 ? 720 : command.height, 180, 2160));
            SessionState.SetInt(CaptureWarmupKey, Mathf.Clamp(command.warmupFrames <= 0 ? 120 : command.warmupFrames, 1, 1200));
            SessionState.SetFloat(CaptureWarmupSecondsKey, Mathf.Clamp(command.warmupSeconds, 0f, 180f));
            SessionState.SetFloat(CaptureStartGameTimeKey, -1f);
            SessionState.SetInt(CaptureFrameKey, 0);
            SessionState.SetFloat(CaptureTimeScaleKey, Mathf.Clamp(command.timeScale <= 0f ? 1f : command.timeScale, 0.25f, 8f));
            SessionState.SetBool(CaptureOnTerminalKey, command.captureOnTerminal);
            SessionState.SetBool(CaptureOnBossKey, command.captureOnBoss);
            SessionState.SetBool(CaptureExitPendingKey, false);
            SessionState.SetBool(CaptureActiveKey, true);

            Debug.Log("[OrdaX Agent] Entering Play Mode for autonomous HORDAX capture.");
            EditorApplication.EnterPlaymode();
        }

        private static string ResolveCaptureOutput(AgentCommand command)
        {
            string output = string.IsNullOrWhiteSpace(command.outputPath)
                ? Path.Combine(ProjectRoot, "Artifacts", "UnityCaptures", "prototype-agent.png")
                : Path.GetFullPath(command.outputPath);

            string directory = Path.GetDirectoryName(output);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            return output;
        }

        private static void TickCapture()
        {
            if (!EditorApplication.isPlaying)
                return;

            Time.timeScale = SessionState.GetFloat(CaptureTimeScaleKey, 1f);

            float startGameTime = SessionState.GetFloat(CaptureStartGameTimeKey, -1f);
            if (startGameTime < 0f)
            {
                startGameTime = Time.time;
                SessionState.SetFloat(CaptureStartGameTimeKey, startGameTime);
            }

            float elapsedGameTime = Time.time - startGameTime;
            bool captureOnTerminal = SessionState.GetBool(CaptureOnTerminalKey, true);
            bool captureOnBoss = SessionState.GetBool(CaptureOnBossKey, false);

            bool terminalState =
                GameManager.Instance != null &&
                (GameManager.Instance.State == GameState.Won ||
                 GameManager.Instance.State == GameState.Lost);

            bool bossEncounter =
                captureOnBoss &&
                EnemyAgent.ActiveBoss != null &&
                GameManager.Instance != null &&
                GameManager.Instance.TryGetActiveBossBarrier(out _);

            bool warmupReached;
            float warmupSeconds = SessionState.GetFloat(CaptureWarmupSecondsKey, 0f);
            if (warmupSeconds > 0f)
            {
                warmupReached = elapsedGameTime >= warmupSeconds;
            }
            else
            {
                int frame = SessionState.GetInt(CaptureFrameKey, 0) + 1;
                SessionState.SetInt(CaptureFrameKey, frame);
                int warmup = SessionState.GetInt(CaptureWarmupKey, 120);
                warmupReached = frame >= warmup;
            }

            bool earlyTerminalCapture =
                captureOnTerminal &&
                terminalState &&
                elapsedGameTime >= 1f;

            bool earlyBossCapture =
                bossEncounter &&
                elapsedGameTime >= 1f;

            if (!warmupReached && !earlyTerminalCapture && !earlyBossCapture)
                return;

            try
            {
                string output = SessionState.GetString(CaptureOutputKey, string.Empty);
                int width = SessionState.GetInt(CaptureWidthKey, 1280);
                int height = SessionState.GetInt(CaptureHeightKey, 720);
                AutomationCapture.CaptureCameraTo(output, width, height);

                Time.timeScale = 1f;
                SessionState.SetBool(CaptureActiveKey, false);
                SessionState.SetBool(CaptureExitPendingKey, true);
                EditorApplication.ExitPlaymode();
            }
            catch (Exception error)
            {
                Time.timeScale = 1f;
                SessionState.SetBool(CaptureActiveKey, false);
                SessionState.SetBool(CaptureExitPendingKey, false);
                WriteCaptureResponse(false, error.GetType().Name + ": " + error.Message);
                if (EditorApplication.isPlaying)
                    EditorApplication.ExitPlaymode();
            }
        }

        private static void FinishCapture()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying)
                return;

            SessionState.SetBool(CaptureExitPendingKey, false);
            WriteCaptureResponse(true, "Autonomous Unity capture completed.");

            string previousScene = SessionState.GetString(CapturePreviousSceneKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(previousScene) && File.Exists(Path.Combine(ProjectRoot, previousScene)))
            {
                try
                {
                    EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
                }
                catch (Exception error)
                {
                    Debug.LogWarning("[OrdaX Agent] Could not restore prior scene: " + error.Message);
                }
            }
        }

        private static void WriteCaptureResponse(bool ok, string summary)
        {
            string id = SessionState.GetString(CaptureCommandIdKey, string.Empty);
            string output = SessionState.GetString(CaptureOutputKey, string.Empty);
            AgentResponse response = new AgentResponse
            {
                id = id,
                ok = ok,
                summary = summary,
                unityVersion = Application.unityVersion,
                compiling = EditorApplication.isCompiling,
                playing = EditorApplication.isPlaying,
                artifact = ok ? output : string.Empty,
                snapshotPath = ok ? Path.ChangeExtension(output, ".json") : string.Empty
            };

            string responsePath = SessionState.GetString(CaptureResponsePathKey, ResponsePath(id));
            SaveResponse(response, responsePath);
        }

        private static void WriteResponse(AgentCommand command, bool ok, string summary)
        {
            AgentResponse response = BaseResponse(command);
            response.ok = ok;
            response.summary = summary;
            SaveResponse(response);
        }

        private static AgentResponse BaseResponse(AgentCommand command)
        {
            return new AgentResponse
            {
                id = command.id,
                unityVersion = Application.unityVersion,
                compiling = EditorApplication.isCompiling,
                playing = EditorApplication.isPlaying
            };
        }

        private static string ResponsePath(string id)
        {
            EnsureDirectories();
            return Path.Combine(Responses, id + ".json");
        }

        private static void SaveResponse(AgentResponse response, string explicitPath = null)
        {
            try
            {
                string path = string.IsNullOrWhiteSpace(explicitPath)
                    ? ResponsePath(response.id)
                    : explicitPath;
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(response, true));
                if (File.Exists(path))
                    File.Delete(path);
                File.Move(temp, path);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
        }
    }
}
#endif
