using UnityEngine;

public class MainCurrencyHudVisibility : MonoBehaviour
{
    [SerializeField] private GameObject mainCurrencyHud;

    private void OnEnable()
    {
        if (mainCurrencyHud != null)
            mainCurrencyHud.SetActive(false);
    }

    private void OnDisable()
    {
        if (mainCurrencyHud != null)
            mainCurrencyHud.SetActive(true);
    }
}