#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace HORDAX.EditorTools
{
    [InitializeOnLoad]
    public static class RemoteAutoSync
    {
        private const string TargetBranch = "dev/unity6-gameplay-pass-1";
        private const string StableBranch = "upgrade/unity-6000.6.1f1";
        private const string EnabledKey = "HORDAX_REMOTE_AUTO_SYNC_ENABLED";
        private const double PollIntervalSeconds = 20d;

        private static double nextPollAt;
        private static Task<SyncResult> syncTask;
        private static string status = "waiting";

        public static string Status => status;

        static RemoteAutoSync()
        {
            if (!EditorPrefs.HasKey(EnabledKey))
                EditorPrefs.SetBool(EnabledKey, true);

            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            nextPollAt = EditorApplication.timeSinceStartup + 4d;
        }

        [MenuItem("HORDAX/Automation/Remote Auto Sync")]
        private static void Toggle()
        {
            bool enabled = !EditorPrefs.GetBool(EnabledKey, true);
            EditorPrefs.SetBool(EnabledKey, enabled);
            Menu.SetChecked("HORDAX/Automation/Remote Auto Sync", enabled);
            status = enabled ? "enabled" : "disabled";
            Debug.Log($"[HORDAX Auto Sync] {status}.");
        }

        [MenuItem("HORDAX/Automation/Remote Auto Sync", true)]
        private static bool ValidateToggle()
        {
            Menu.SetChecked(
                "HORDAX/Automation/Remote Auto Sync",
                EditorPrefs.GetBool(EnabledKey, true));
            return true;
        }

        [MenuItem("HORDAX/Automation/Sync Now")]
        private static void SyncNow()
        {
            nextPollAt = 0d;
        }

        [MenuItem("HORDAX/Automation/Print Sync Status")]
        private static void PrintStatus()
        {
            Debug.Log($"[HORDAX Auto Sync] {status}");
        }

        private static void Tick()
        {
            if (Application.isBatchMode ||
                !EditorPrefs.GetBool(EnabledKey, true) ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating)
                return;

            if (syncTask != null)
            {
                if (!syncTask.IsCompleted)
                    return;

                Task<SyncResult> completed = syncTask;
                syncTask = null;

                if (completed.IsFaulted)
                {
                    Exception error = completed.Exception?.GetBaseException();
                    status = "error: " + (error?.Message ?? "unknown");
                    Debug.LogWarning($"[HORDAX Auto Sync] {status}");
                    return;
                }

                SyncResult result = completed.Result;
                status = result.Message;

                if (result.Changed)
                {
                    Debug.Log($"[HORDAX Auto Sync] {result.Message}");
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                }

                return;
            }

            double now = EditorApplication.timeSinceStartup;
            if (now < nextPollAt)
                return;

            nextPollAt = now + PollIntervalSeconds;
            status = "checking remote";

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            syncTask = Task.Run(() => SyncOnce(projectRoot));
        }

        private static SyncResult SyncOnce(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot) ||
                !Directory.Exists(Path.Combine(projectRoot, ".git")))
            {
                return new SyncResult(false, "paused: project is not a Git checkout");
            }

            GitResult trackedStatus = Git(projectRoot, "status --porcelain --untracked-files=no");
            if (!trackedStatus.Ok)
                return new SyncResult(false, "paused: could not read Git status");

            if (!string.IsNullOrWhiteSpace(trackedStatus.Output))
                return new SyncResult(false, "paused: local tracked changes exist");

            GitResult currentResult = Git(projectRoot, "rev-parse --abbrev-ref HEAD");
            if (!currentResult.Ok)
                return new SyncResult(false, "paused: could not determine branch");

            string currentBranch = currentResult.Output.Trim();
            if (!string.Equals(currentBranch, TargetBranch, StringComparison.Ordinal) &&
                !string.Equals(currentBranch, StableBranch, StringComparison.Ordinal))
            {
                return new SyncResult(
                    false,
                    $"paused: branch '{currentBranch}' is not managed");
            }

            GitResult fetch = Git(projectRoot, $"fetch --quiet origin {Quote(TargetBranch)}");
            if (!fetch.Ok)
                return new SyncResult(false, "offline: remote fetch failed");

            GitResult remoteResult = Git(projectRoot, $"rev-parse origin/{TargetBranch}");
            if (!remoteResult.Ok)
                return new SyncResult(false, "paused: remote development branch is unavailable");

            string remoteSha = remoteResult.Output.Trim();

            if (string.Equals(currentBranch, StableBranch, StringComparison.Ordinal))
            {
                GitResult checkout = Git(
                    projectRoot,
                    $"checkout -B {Quote(TargetBranch)} origin/{TargetBranch}");

                if (!checkout.Ok)
                    return new SyncResult(false, "paused: could not enter managed development branch");

                return new SyncResult(
                    true,
                    $"updated to {ShortSha(remoteSha)} and switched to {TargetBranch}");
            }

            GitResult localResult = Git(projectRoot, "rev-parse HEAD");
            if (!localResult.Ok)
                return new SyncResult(false, "paused: could not read local revision");

            string localSha = localResult.Output.Trim();
            if (string.Equals(localSha, remoteSha, StringComparison.OrdinalIgnoreCase))
                return new SyncResult(false, $"current: {ShortSha(localSha)}");

            GitResult ancestor = Git(
                projectRoot,
                $"merge-base --is-ancestor {Quote(localSha)} {Quote(remoteSha)}");

            if (!ancestor.Ok)
                return new SyncResult(false, "paused: local branch diverged from remote");

            GitResult merge = Git(projectRoot, $"merge --ff-only --quiet origin/{TargetBranch}");
            if (!merge.Ok)
                return new SyncResult(false, "paused: fast-forward update failed");

            return new SyncResult(
                true,
                $"updated {ShortSha(localSha)} -> {ShortSha(remoteSha)}");
        }

        private static GitResult Git(string projectRoot, string arguments)
        {
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "-C " + Quote(projectRoot) + " " + arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using (Process process = Process.Start(start))
            {
                if (process == null)
                    return new GitResult(-1, string.Empty, "git process did not start");

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (!process.WaitForExit(15000))
                {
                    try { process.Kill(); } catch { }
                    return new GitResult(-1, output, "git command timed out");
                }

                return new GitResult(process.ExitCode, output, error);
            }
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        private static string ShortSha(string sha)
        {
            if (string.IsNullOrWhiteSpace(sha))
                return "unknown";

            return sha.Length <= 7 ? sha : sha.Substring(0, 7);
        }

        private readonly struct SyncResult
        {
            public SyncResult(bool changed, string message)
            {
                Changed = changed;
                Message = message;
            }

            public bool Changed { get; }
            public string Message { get; }
        }

        private readonly struct GitResult
        {
            public GitResult(int exitCode, string output, string error)
            {
                ExitCode = exitCode;
                Output = output?.Trim() ?? string.Empty;
                Error = error?.Trim() ?? string.Empty;
            }

            public int ExitCode { get; }
            public string Output { get; }
            public string Error { get; }
            public bool Ok => ExitCode == 0;
        }
    }
}
#endif
