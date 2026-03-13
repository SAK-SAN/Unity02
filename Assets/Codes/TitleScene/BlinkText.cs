using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class BlinkText : MonoBehaviour
{
    [Header("点滅の速さ")]
    public float blinkSpeed = 3f;

    private TextMeshProUGUI textMesh;
    private Color originalColor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        originalColor = textMesh.color;
    }

    // Update is called once per frame
    void Update()
    {
        float alpha = Mathf.Abs(Mathf.Sin(Time.time * blinkSpeed));
        textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
    }
}
