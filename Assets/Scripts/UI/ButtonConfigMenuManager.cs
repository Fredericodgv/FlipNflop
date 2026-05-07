using UnityEngine;

public class MenuInputListener : MonoBehaviour
{
    private ConfigMenuManager menuManager;

    private void Awake()
    {
        menuManager = ConfigMenuManager.Instance;
    }

    void Update()
    {
        if (menuManager == null) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!menuManager.IsMenuOpen)
            {
                Debug.Log("ESC → Abrindo menu");
                menuManager.OpenMenuConfig();
            }
            else
            {
                Debug.Log("ESC → Fechando menu");
                menuManager.ContinueGame();
            }
        }
    }
}
