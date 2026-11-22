using UnityEngine;
using UnityEngine.UI;

// BRYN-18: Tlačidlo Nastavenia v hornom paneli. Panel sa vytvorí z prefabu pri prvom otvorení
// (bez Resources.Load, prefab je priradený v Inspectore) a potom sa už len zapína a vypína.
// Panel má vlastný Canvas, preto sa nevkladá pod žiadneho rodiča.
[RequireComponent(typeof(Button))]
public class SettingsButton : MonoBehaviour
{
    [SerializeField] private SettingsPanel panelPrefab;

    private Button button;
    private SettingsPanel panel;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        button.onClick.AddListener(OnClick);
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(OnClick);
    }

    private void OnDestroy()
    {
        if (panel != null)
        {
            Destroy(panel.gameObject);
        }
    }

    private void OnClick()
    {
        if (panel == null)
        {
            if (panelPrefab == null)
            {
                Debug.LogWarning("SettingsButton: nie je priradený prefab SettingsPanel.", this);
                return;
            }

            panel = Instantiate(panelPrefab);
            panel.name = "SettingsPanel";
        }

        panel.Open();
    }
}
