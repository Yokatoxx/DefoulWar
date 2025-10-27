using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;

public class  SceneButton : MonoBehaviour, IPointerClickHandler
{
    public string sceneName;

    private TextMeshProUGUI tmpText;

    private void Start()
    {
        tmpText = GetComponent<TextMeshProUGUI>();
        tmpText.text = sceneName;
    }
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
