using UnityEngine;
using UnityEngine.UI;

public class CustomCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    public Texture2D cursorTexture;
    public Vector2 hotSpot = Vector2.zero; // (0,0) es esquina superior-izquierda; usa textura/2 para centrar
    public CursorMode cursorMode = CursorMode.Auto;

    [Header("Crosshair Settings")]
    public bool useCrosshairUI = true; // Usa UI Canvas en lugar de cursor del sistema
    public Canvas crosshairCanvas;
    public Image crosshairImage;

    void Start()
    {
        // Opción 1: Usar cursor personalizado del sistema
        if (!useCrosshairUI && cursorTexture != null)
        {
            // Centra el hotspot en la textura para que aparezca en el centro de la pantalla
            // Si tu textura es 32x32, hotSpot debería ser (16, 16)
            Vector2 centeredHotSpot = new Vector2(cursorTexture.width / 2f, cursorTexture.height / 2f);
            Cursor.SetCursor(cursorTexture, centeredHotSpot, cursorMode);
            Cursor.visible = true;
        }
        
        // Opción 2: Usar crosshair de UI (recomendado para FPS)
        else if (useCrosshairUI && crosshairCanvas != null && crosshairImage != null)
        {
            // Asegúrate de que el Canvas esté en Screen Space - Overlay
            // y que la imagen del crosshair esté en el centro (RectTransform posición 0,0)
            crosshairImage.gameObject.SetActive(true);
        }
    }
}