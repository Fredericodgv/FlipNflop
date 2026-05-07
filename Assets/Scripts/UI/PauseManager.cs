using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public Transform pauseMenu;
    [SerializeField] private string nomeMenu;

    public void irMenu()
    {
        SceneManager.LoadScene(nomeMenu);
        Time.timeScale = 1;
    }

    public void FecharPause()
    {
        pauseMenu.gameObject.SetActive(false);
        Time.timeScale = 1;
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
        {
            if(!pauseMenu.gameObject.activeSelf)
            {
                pauseMenu.gameObject.SetActive(true);
                Time.timeScale = 0;
            }
        }
    }
}
