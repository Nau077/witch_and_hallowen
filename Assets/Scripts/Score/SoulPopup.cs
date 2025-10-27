// SoulPopup.cs
using UnityEngine;
using TMPro;

public class SoulPopup : MonoBehaviour
{
    public TMP_Text text;
    public float riseSpeed = 1f;
    public float fadeSpeed = 2f;

    void Update()
    {
        transform.Translate(Vector3.up * riseSpeed * Time.deltaTime);
        var color = text.color;
        color.a -= fadeSpeed * Time.deltaTime;
        text.color = color;
        if (color.a <= 0) Destroy(gameObject);
    }

    public static void Create(Vector3 position, int amount)
    {
        var prefab = Resources.Load<SoulPopup>("SoulPopupPrefab");
        var popup = Instantiate(prefab, position, Quaternion.identity);
        popup.text.text = $"+{amount}";
    }
}
