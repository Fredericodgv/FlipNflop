using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject painelMenuInicial;
    [SerializeField] private GameObject painelSobre;
    [SerializeField] private string menu;
    [SerializeField] private string nomeDoLevel;
    [SerializeField] private string nextLevel;
    [SerializeField] private string levelAtual;

    public void Jogar()
    {
        SceneManager.LoadScene(nomeDoLevel);
    }

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

    public void Sair()
    {
        //UnityEditor.EditorApplication.isPlaying = false;
        Application.Quit();
    }

    // Level Manager
    public void NextLevel()
    {
        SceneManager.LoadScene(nextLevel);
    }

    public void Menu()
    {
        SceneManager.LoadScene(menu);
    }

    public void Restart()
    {
        SceneManager.LoadScene(levelAtual);
    }

    
}
