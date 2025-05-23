using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class MenuManager : MonoBehaviour
{
    [SerializeField] private string nomeDoLevel;
    [SerializeField] private GameObject painelMenuInicial;
    [SerializeField] private GameObject painelSobre;

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
        UnityEditor.EditorApplication.isPlaying = false;
        //Application.Quit();
    }
    
}
