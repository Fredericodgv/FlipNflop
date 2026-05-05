using UnityEngine;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;

public class UploadMenuManager : MonoBehaviour
{
    [SerializeField] private string customSceneName = "Custom";
    [SerializeField] private GameObject feedbackErro;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void UploadJSON(string objectName, string callbackMethod);
#endif

    public void OnClickUpload()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UploadJSON(gameObject.name, nameof(OnJSONReceived));
#else
        Debug.LogWarning("File picker só funciona no build WebGL. Use o Inspector para testar.");
#endif
    }

    // Chamado pelo JS via SendMessage
    public void OnJSONReceived(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            if (feedbackErro != null) feedbackErro.SetActive(true);
            return;
        }

        UploadedLevelJson.Content = jsonContent;
        MenuManager.LevelToLoadJSON = ""; // garante que o loader use Content, não Resources
        SceneManager.LoadScene(customSceneName);
    }
}