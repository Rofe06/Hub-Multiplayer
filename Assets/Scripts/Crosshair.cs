using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    [Header("Apparence")]
    public Color crosshairColor = Color.white;
    public float size = 20f;   // taille des branches
    public float thickness = 2f;    // épaisseur des branches
    public float gap = 5f;    // espace au centre

    // Les 4 branches du crosshair
    private Image _top, _bottom, _left, _right, _dot;

    void Awake()
    {
        // Crée les 4 branches + point central
        _top = CreateBranch("Top");
        _bottom = CreateBranch("Bottom");
        _left = CreateBranch("Left");
        _right = CreateBranch("Right");
        _dot = CreateBranch("Dot");

        UpdateCrosshair();
    }

    private Image CreateBranch(string name)
    {
        GameObject go = new GameObject(name, typeof(Image));
        go.transform.SetParent(transform, false);
        return go.GetComponent<Image>();
    }

    private void UpdateCrosshair()
    {
        // Couleur
        _top.color = _bottom.color = _left.color = _right.color = crosshairColor;
        _dot.color = crosshairColor;

        // Taille et position de chaque branche
        SetBranch(_top, new Vector2(thickness, size), new Vector2(0, gap + size * 0.5f));
        SetBranch(_bottom, new Vector2(thickness, size), new Vector2(0, -gap - size * 0.5f));
        SetBranch(_left, new Vector2(size, thickness), new Vector2(-gap - size * 0.5f, 0));
        SetBranch(_right, new Vector2(size, thickness), new Vector2(gap + size * 0.5f, 0));

        // Point central (optionnel)
        SetBranch(_dot, new Vector2(thickness, thickness), Vector2.zero);
    }

    private void SetBranch(Image img, Vector2 sizeDelta, Vector2 anchoredPos)
    {
        RectTransform rt = img.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeDelta;
        rt.anchoredPosition = anchoredPos;
    }

    // Appelable depuis d'autres scripts pour animer le crosshair (ex: recul)
    public void Pulse(float expandAmount = 5f)
    {
        gap += expandAmount;
        UpdateCrosshair();
        Invoke(nameof(ResetGap), 0.1f);
    }

    private float _originalGap;
    private void ResetGap()
    {
        gap = _originalGap > 0 ? _originalGap : 5f;
        UpdateCrosshair();
    }

    // Permet de modifier les valeurs en temps réel dans l'Inspector
    void OnValidate() => UpdateCrosshair();
}