using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class MenuManager : MonoBehaviour
{
    [Header("Painéis do Menu Inicial")]
    [SerializeField] private GameObject painelMenuInicial;
    [SerializeField] private GameObject painelSobre;
    [SerializeField] private GameObject painelLevelSelect;

    [Header("Navegação de Cenas")]
    [SerializeField] private string nomeDoLevel;
    [SerializeField] private string customLevelName = "Custom";

    public static string LevelToLoadJSON = "";

    #region Navegação Básica

    public void Jogar() => SceneManager.LoadScene(nomeDoLevel);

    public void Upload() => SceneManager.LoadScene("LevelUpload");

    public void Sair() => Application.Quit();

    #endregion

    #region Controle de Telas (Menu Inicial)

    public void AbrirSobre()
    {
        painelMenuInicial.SetActive(false);
        painelSobre.SetActive(true);
    }

    public void FecharSobre()
    {
        painelMenuInicial.SetActive(true);
        painelSobre.SetActive(false);
    }

    public void AbrirSelecaoDeNiveis()
    {
        painelMenuInicial.SetActive(false);
        painelLevelSelect.SetActive(true);
    }

    public void FecharSelecaoDeNiveis()
    {
        painelLevelSelect.SetActive(false);
        painelMenuInicial.SetActive(true);
    }

    #endregion

    #region Carregamento JSON

    public void SelectLevelAndLoad(string levelJsonName)
    {
        if (string.IsNullOrEmpty(levelJsonName))
        {
            Debug.LogError("Nome do arquivo JSON não pode ser nulo ou vazio");
            return;
        }

        LevelToLoadJSON = levelJsonName;
        Debug.Log("JSON Selecionado: " + levelJsonName);

        SceneManager.LoadScene(customLevelName);
    }

    #endregion
}