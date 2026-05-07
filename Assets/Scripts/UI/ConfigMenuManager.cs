using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ConfigMenuManager : MonoBehaviour
{
    [Header("Painel Principal do Menu de Configurações")]
    [SerializeField] private GameObject mainConfigMenu;

    [Header("Páginas")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject optionsGamePanel;
    [SerializeField] private GameObject audioPanel;
    [SerializeField] private GameObject videoPanel;
    [SerializeField] private GameObject controlsPanel;

    [Header("Referências")]
    [SerializeField] private GameObject buttonConfig; // o botão ⚙️ sempre visível
    [SerializeField] private string nomeMenuInicial;

    
    [Header("Configurações de Controles")]
    [SerializeField] private TMP_Text keyA_Text;
    [SerializeField] private TMP_Text keyW_Text;
    [SerializeField] private TMP_Text keyD_Text;
    [SerializeField] private TMP_Text keySpace_Text;

    public bool IsMenuOpen => mainConfigMenu.activeSelf;

    private string waitingForKey = null; // identifica qual tecla estamos aguardando

    private Dictionary<string, KeyCode> keyBindings = new Dictionary<string, KeyCode>();

    private static ConfigMenuManager _instance;
    public static ConfigMenuManager Instance => _instance;

    private void Awake()
    {
        // Garantir que exista apenas um instance global
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // mantém ao mudar de cena
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Início
    void Start()
    {
        if (mainConfigMenu != null)
            mainConfigMenu.SetActive(false); 

        if (buttonConfig != null)
            buttonConfig.SetActive(true); 

        LoadKeyBindings();
        ShowPage(mainPanel);
    }

    void Update()
    {
        // Captura de tecla para remapeamento
        if (waitingForKey != null)
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    SetNewKey(waitingForKey, key);
                    waitingForKey = null;
                    break;
                }
            }
        }
    }

    #region Controle de Páginas

    private void ShowPage(GameObject page)
    {
        // Desativa todas
        mainPanel.SetActive(false);
        optionsPanel.SetActive(false);
        optionsGamePanel.SetActive(false);
        audioPanel.SetActive(false);
        videoPanel.SetActive(false);
        controlsPanel.SetActive(false);

        // Ativa apenas a desejada
        if (page != null)
            page.SetActive(true);
    }

    public void OpenMenuConfig()
    {
        Time.timeScale = 0f;
        mainConfigMenu.SetActive(true);
        buttonConfig.SetActive(false);
        ShowPage(mainPanel);
    }

    #endregion
    #region Botões da página Principal

    public void ContinueGame()
    {
        Time.timeScale = 1f;
        mainConfigMenu.SetActive(false);
        buttonConfig.SetActive(true);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackInitialMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(nomeMenuInicial);
    }

    public void OpenOptions() => ShowPage(optionsPanel);

    #endregion
    #region Botões da página de Opções

    public void OpenOptionsGame() => ShowPage(optionsGamePanel);
    public void OpenAudio() => ShowPage(audioPanel);
    public void OpenVideo() => ShowPage(videoPanel);
    public void OpenControles() => ShowPage(controlsPanel);
    public void BackToMain() => ShowPage(mainPanel);

    #endregion
    #region Botões de x páginas

    public void RestoreDefault()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("Configurações restauradas aos padrões.");
    }

    public void BackToOptions() => ShowPage(optionsPanel);



    public void ChangeLanguage(int index)
    {
        string idioma = index == 0 ? "Português" : "Inglês";
        PlayerPrefs.SetString("Idioma", idioma);
        Debug.Log($"Idioma alterado para {idioma}");
    }

    public void ChangeGeneralVolume(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat("VolumeGeral", valor);
    }

    public void ChangeBrightness(float valor)
    {
        RenderSettings.ambientLight = Color.white * valor;
        PlayerPrefs.SetFloat("Brilho", valor);
    }

    #endregion
    #region Botões da página de Controle

    public void StartKeyBinding(string action)
    {
        waitingForKey = action;
        Debug.Log($"Aguardando nova tecla para {action}...");
    }

    private void SetNewKey(string action, KeyCode key)
    {
        keyBindings[action] = key;
        PlayerPrefs.SetString($"Key_{action}", key.ToString());
        UpdateKeyTexts();
        Debug.Log($"Tecla de {action} alterada para: {key}");
    }

    private void LoadKeyBindings()
    {
        keyBindings["Backward"] = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Key_Backward", "A"));
        keyBindings["Gravity"] = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Key_Gravity", "W"));
        keyBindings["Forward"] = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Key_Forward", "D"));
        keyBindings["Jump"] = (KeyCode)System.Enum.Parse(typeof(KeyCode), PlayerPrefs.GetString("Key_Jump", "Space"));

        UpdateKeyTexts();
    }

    private void UpdateKeyTexts()
    {
        keyA_Text.text = keyBindings["Backward"].ToString();
        keyW_Text.text = keyBindings["Gravity"].ToString();
        keyD_Text.text = keyBindings["Forward"].ToString();
        keySpace_Text.text = keyBindings["Jump"].ToString();
    }

    public void RestoreDefaultControls()
    {
        PlayerPrefs.DeleteKey("Key_Backward");
        PlayerPrefs.DeleteKey("Key_Gravity");
        PlayerPrefs.DeleteKey("Key_Forward");
        PlayerPrefs.DeleteKey("Key_Jump");
        LoadKeyBindings();
        Debug.Log("Controles restaurados aos padrões.");
    }

    public static KeyCode GetKey(string action)
    {
        if (Instance == null) return KeyCode.None;

        if (Instance.keyBindings.ContainsKey(action))
            return Instance.keyBindings[action];

        return KeyCode.None;
    }

    #endregion
}
