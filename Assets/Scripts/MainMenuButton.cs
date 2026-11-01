using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Botão "voltar ao menu principal" que funciona sozinho, sem o tool e sem alterar outros scripts.
//
// Dois modos de uso:
//  1) Põe este script num objeto que JÁ tenha um Button: o clique volta ao menu.
//  2) Põe este script no painel (GameOverPanel / WinPanel): quando o painel aparecer,
//     cria sozinho um botão "MENU PRINCIPAL" dentro dele (se ainda não existir).
public class MainMenuButton : MonoBehaviour
{
    [Tooltip("Índice da cena do menu principal nas Build Settings.")]
    [SerializeField] private int mainMenuScene = 0;

    [Header("Só no modo 2 (botão criado automaticamente)")]
    [SerializeField] private string label = "MENU PRINCIPAL";
    [Tooltip("Posição do canto inferior esquerdo do botão, relativa ao centro do painel.")]
    [SerializeField] private Vector2 position = new Vector2(-120f, -280f);
    [SerializeField] private Vector2 size = new Vector2(240f, 60f);
    [SerializeField] private int fontSize = 22;

    private const string ButtonName = "Btn_MainMenu";

    private void Awake()
    {
        Button button = GetComponent<Button>();

        if (button == null)
        {
            Transform existing = transform.Find(ButtonName);
            button = existing != null ? existing.GetComponent<Button>() : CreateButton();
        }

        if (button != null) button.onClick.AddListener(GoToMainMenu);
    }

    public void GoToMainMenu()
    {
        AudioListener.pause = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    private Button CreateButton()
    {
        var go = new GameObject(ButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
        var rt = (RectTransform)go.transform;
        rt.SetParent(transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        var image = go.GetComponent<Image>();
        image.color = new Color(0.12f, 0.12f, 0.12f, 0.9f);

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;

        var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        var textRt = (RectTransform)textGo.transform;
        textRt.SetParent(rt, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = textRt.offsetMax = Vector2.zero;

        var text = textGo.GetComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.raycastTarget = false;
#if UNITY_2022_2_OR_NEWER
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif

        return button;
    }
}
