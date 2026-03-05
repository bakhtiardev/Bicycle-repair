using UnityEngine;
using UnityEngine.UI;

public class IntroFlowController : MonoBehaviour
{
    public GameObject welcomePanel;
    public GameObject descriptionPanel;

    public Button startButton;
    public Button nextButton;

    void Start()
    {
        if (welcomePanel != null) welcomePanel.SetActive(true);
        if (descriptionPanel != null) descriptionPanel.SetActive(false);

        if (startButton != null) startButton.onClick.AddListener(OnStartPressed);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextPressed);
    }

    void OnStartPressed()
    {
        if (welcomePanel != null) welcomePanel.SetActive(false);
        if (descriptionPanel != null) descriptionPanel.SetActive(true);
    }

    void OnNextPressed()
    {
        Debug.Log("Next button pressed! Action applies later.");
    }
}
