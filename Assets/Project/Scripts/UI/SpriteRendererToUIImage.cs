using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SpriteRendererToUIImage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer sourceRenderer;
    [SerializeField] private Image targetImage;

    private Sprite _lastSprite;

    private void Awake()
    {
        ApplySprite();
    }

    private void LateUpdate()
    {
        ApplySprite();
    }

    private void ApplySprite()
    {
        if (sourceRenderer == null || targetImage == null)
            return;

        Sprite currentSprite = sourceRenderer.sprite;

        if (currentSprite == _lastSprite)
            return;

        _lastSprite = currentSprite;
        targetImage.sprite = currentSprite;
    }
}