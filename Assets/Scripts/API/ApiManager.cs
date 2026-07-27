using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class ApiManager : MonoBehaviour
{
    private const string ApiUrl = "http://localhost:5000/levels";

    /// <summary>
    /// Busca todos os níveis (fases) disponíveis na API.
    /// </summary>
    public void FetchAllLevels(Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(FetchAllLevelsRoutine(onSuccess, onError));
    }

    private IEnumerator FetchAllLevelsRoutine(Action<string> onSuccess, Action<string> onError)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(ApiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }

    /// <summary>
    /// Busca uma fase específica através do seu identificador (UUID).
    /// </summary>
    public void GetLevelById(string id, Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(GetLevelByIdRoutine(id, onSuccess, onError));
    }

    private IEnumerator GetLevelByIdRoutine(string id, Action<string> onSuccess, Action<string> onError)
    {
        string url = $"{ApiUrl}/{id}";
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }

    /// <summary>
    /// Envia o JSON de uma nova fase para ser salva na API.
    /// </summary>
    public void CreateLevel(string levelJson, Action onSuccess, Action<string> onError)
    {
        StartCoroutine(CreateLevelRoutine(levelJson, onSuccess, onError));
    }

    private IEnumerator CreateLevelRoutine(string levelJson, Action onSuccess, Action<string> onError)
    {
        using (UnityWebRequest request = new UnityWebRequest(ApiUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(levelJson);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke();
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }

    /// <summary>
    /// Remove uma fase da API através do seu identificador (UUID).
    /// </summary>
    public void DeleteLevel(string id, Action onSuccess, Action<string> onError)
    {
        StartCoroutine(DeleteLevelRoutine(id, onSuccess, onError));
    }

    private IEnumerator DeleteLevelRoutine(string id, Action onSuccess, Action<string> onError)
    {
        string url = $"{ApiUrl}/{id}";
        using (UnityWebRequest request = UnityWebRequest.Delete(url))
        {
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onSuccess?.Invoke();
            }
            else
            {
                onError?.Invoke(request.error);
            }
        }
    }
}