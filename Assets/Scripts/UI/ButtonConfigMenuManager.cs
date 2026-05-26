// using UnityEngine;
// using UnityEngine.UIElements; // Namespace do UI Toolkit

// public class ButtonConfigMenuManager : MonoBehaviour
// {
//     private UIDocument _uiDocument;
//     private Button _btnMenu;

//     void OnEnable()
//     {
//         // 1. Pega o componente UI Document do GameObject da HUD
//         _uiDocument = GetComponent<UIDocument>();
//         var root = _uiDocument.rootVisualElement;

//         // 2. Busca o botão pelo Name que demos no UI Builder
//         _btnMenu = root.Q<Button>("BtnOpenMenu");

//         // 3. Inscreve a função no evento de clique
//         if (_btnMenu != null)
//         {
//             _btnMenu.clicked += OpenConfigMenu;
//         }
//     }

//     void OnDisable()
//     {
//         // Boa prática: remove a inscrição ao desativar
//         if (_btnMenu != null)
//         {
//             _btnMenu.clicked -= OpenConfigMenu;
//         }
//     }

//     private void OpenConfigMenu()
//     {
//         // Chama o ConfigMenuManager para abrir o quadro-negro
//         if (ConfigMenuManager.Instance != null)
//         {
//             ConfigMenuManager.Instance.OpenMenuConfig();
//         }

//         // Esconde a HUD inteira (e este botão) enquanto o menu de configuração estiver aberto
//         _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
//     }

//     // Função pública para o ConfigMenuManager mandar a HUD reaparecer depois
//     public void ShowHUD()
//     {
//         if (_uiDocument != null)
//         {
//             _uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
//         }
//     }
// }