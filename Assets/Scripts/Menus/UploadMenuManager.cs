using UnityEngine;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;
using UnityEngine.UIElements; // Adicionado para o UI Toolkit

public class UploadMenuManager : MonoBehaviour
{
    [SerializeField] private string customSceneName = "Custom";

    // Novo: Variável para o UI Toolkit no lugar do GameObject
    private VisualElement _feedbackErro;

    private void OnEnable()
    {
        // Busca o UIDocument no mesmo GameObject
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument != null)
        {
            // Busca o elemento de erro pelo ID (você precisa criar isso no UI Builder)
            _feedbackErro = uiDocument.rootVisualElement.Q<VisualElement>("ErrorFeedback");

            // Garante que a mensagem comece escondida
            if (_feedbackErro != null)
                _feedbackErro.style.display = DisplayStyle.None;
        }
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void UploadJSON(string objectName, string callbackMethod);
#endif

    public void OnClickUpload()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        // Passa o nome deste GameObject para o JS saber para onde enviar o JSON de volta
        UploadJSON(gameObject.name, nameof(OnJSONReceived));
#else
        Debug.LogWarning("File picker só funciona no build WebGL. Use o Inspector para testar.");
#endif
    }

    /// <summary>
    /// Callback do JavaScript com o conteúdo do JSON.
    /// </summary>
    public void OnJSONReceived(string jsonContent)
    {
        if (string.IsNullOrWhiteSpace(jsonContent))
        {
            // Mostra o feedback de erro no UI Toolkit
            if (_feedbackErro != null)
                _feedbackErro.style.display = DisplayStyle.Flex;

            return;
        }

        // Esconde o erro (caso estivesse aparecendo de uma tentativa anterior)
        if (_feedbackErro != null)
            _feedbackErro.style.display = DisplayStyle.None;

        UploadedLevelJson.Content = jsonContent;
        MenuManager.LevelToLoadJSON = ""; // garante que o loader use Content, não Resources
        SceneManager.LoadScene(customSceneName);
    }
}