using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace AirDefense.EditorTools
{
    /// <summary>
    /// Adds packages from the command line, retrying when a download is
    /// interrupted (this PC's connection drops long downloads).
    /// CLI (no -quit; exits by itself):
    ///   -executeMethod AirDefense.EditorTools.PackageTools.AddGltfast
    /// </summary>
    public static class PackageTools
    {
        private const string Gltfast = "com.unity.cloud.gltfast";
        private const int MaxAttempts = 8;

        private static AddRequest _request;
        private static int _attempt;

        public static void AddGltfast()
        {
            _attempt = 0;
            Start();
            EditorApplication.update += Poll;
        }

        private static void Start()
        {
            _attempt++;
            Debug.Log($"[PackageTools] Adding {Gltfast} (attempt {_attempt}/{MaxAttempts})");
            _request = Client.Add(Gltfast);
        }

        private static void Poll()
        {
            if (_request == null || !_request.IsCompleted) return;

            if (_request.Status == StatusCode.Success)
            {
                Debug.Log($"[PackageTools] Installed {_request.Result.packageId}");
                Finish(0);
            }
            else if (_attempt < MaxAttempts)
            {
                Debug.LogWarning($"[PackageTools] Failed: {_request.Error?.message}. Retrying...");
                Start();
            }
            else
            {
                Debug.LogError($"[PackageTools] Giving up: {_request.Error?.message}");
                Finish(1);
            }
        }

        private static void Finish(int code)
        {
            EditorApplication.update -= Poll;
            if (Application.isBatchMode) EditorApplication.Exit(code);
        }
    }
}
