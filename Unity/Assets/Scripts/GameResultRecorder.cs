using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

internal static class GameResultRecorder
{
    private static bool recorded;
    private static ulong sceneHandle;
    internal static void Reset() { recorded = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize() { recorded = false; sceneHandle = 0; }

    [Serializable]
    private sealed class Result
    {
        public string id, game, player;
        public float score;
    }

    internal static void Record(float score, string player = null)
    {
        MotionSession session = MotionSession.Instance;
        var scene = SceneManager.GetActiveScene();
        if (session == null || !SceneNavigator.IsMinigame(scene.name) || float.IsNaN(score) || float.IsInfinity(score)) return;
        if (sceneHandle != scene.handle.GetRawData()) { sceneHandle = scene.handle.GetRawData(); recorded = false; }
        if (recorded) return;
        recorded = true;
        var result = new Result { id = Guid.NewGuid().ToString(), game = scene.name, score = score, player = player ?? "" };
        session.StartCoroutine(Send(session.RoomCode, JsonUtility.ToJson(result)));
    }

    private static IEnumerator Send(string room, string json)
    {
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
        for (int attempt = 0; attempt < 3; attempt++)
        {
            using (var request = new UnityWebRequest("http://127.0.0.1:8788/results?room=" + UnityWebRequest.EscapeURL(room), "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 3;
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success) yield break;
            }
            yield return new WaitForSecondsRealtime(1f);
        }
        Debug.LogWarning("No se pudo enviar el resultado al perfil del celular.");
    }
}

