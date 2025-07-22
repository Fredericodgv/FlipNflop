using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject painelMenuInicial;
    // A referência ao painelSobre não é mais necessária se você vai usar uma nova cena
    // [SerializeField] private GameObject painelSobre;
    
    [SerializeField] private string menu;
    [SerializeField] private string nomeDoLevel;
    [SerializeField] private string nextLevel;
    [SerializeField] private string levelAtual;
    
    // --- ADICIONE ESTA LINHA ---
    [Header("Nomes das Cenas")]
    [Tooltip("Coloque aqui o nome EXATO do arquivo da sua cena de opções.")]
    [SerializeField] private string nomeCenaOpcoes;

    public void Jogar()
    {
        SceneManager.LoadScene(nomeDoLevel);
    }

    // --- FUNÇÃO MODIFICADA ---
    // Renomeamos para AbrirOpcoes e mudamos a lógica
    public void AbrirOpcoes()
    {
        // A linha abaixo carrega a cena de opções.
        // O nome da cena deve ser exatamente o mesmo do arquivo da cena.
        SceneManager.LoadScene(nomeCenaOpcoes);
    }

    // A função FecharSobre não é mais necessária, pois o botão "Voltar"
    // na sua cena de opções terá seu próprio MenuManager para voltar à cena principal.
    /*
    public void FecharSobre()
    {
        painelMenuInicial.SetActive(true);
        painelSobre.SetActive(false);
    }
    */

    public void Sair()
    {
        Application.Quit();
    }

    // Funções do Level Manager
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