using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [SerializeField] private string nextLevel;
    [SerializeField] private string menu;
    [SerializeField] private string levelAtual;

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
