using UnityEngine;
using UnityEngine.UI;

public class SoulShopKeeperPopup : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("Корневой объект попапа. Если оставить пустым, будет использоваться этот GameObject.")]
    public GameObject popupRoot;

    [Header("Buttons")]
    [Tooltip("Кнопка 'Войти в лес'.")]
    public Button goToForestButton;

    [Tooltip("Кнопка 'Закрыть'.")]
    public Button closeButton;

    private void Awake()
    {
        // Если корень не задан — считаем, что скрипт висит прямо на корне попапа
        if (popupRoot == null)
            popupRoot = gameObject;

        // На старте попап должен быть выключен
        Hide();

        // Подписываемся на кнопки (если заданы)
        if (goToForestButton != null)
            goToForestButton.onClick.AddListener(OnClickGoToForest);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnClickClose);
    }

    /// <summary>Показать попап.</summary>
    public void Show()
    {
        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    /// <summary>Скрыть попап.</summary>
    public void Hide()
    {
        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    /// <summary>Обработчик кнопки 'Войти в лес'.</summary>
    public void OnClickGoToForest()
    {
        // Логика входа в лес: с базы (0) → на этап 1
        if (RunLevelManager.Instance != null)
        {
            RunLevelManager.Instance.GoDeeper();
        }

        Hide();
    }

    /// <summary>Обработчик кнопки 'Закрыть'.</summary>
    public void OnClickClose()
    {
        Hide();
    }
}
