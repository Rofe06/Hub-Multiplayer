using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    [Header("Apparence")]
    public Color crosshairColor = Color.white;
    public float size = 20f;
    public float thickness = 2f;
    public float gap = 5f;

    private Image _top, _bottom, _left, _right, _dot;

    void Awake()
    {
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
        // Vérifie que les branches existent avant de les modifier
        if (_top == null || _bottom == null || _left == null || _right == null || _dot == null)
            return;

        _top.color = _bottom.color = _left.color = _right.color = _dot.color = crosshairColor;

        SetBranch(_top, new Vector2(thickness, size), new Vector2(0, gap + size * 0.5f));
        SetBranch(_bottom, new Vector2(thickness, size), new Vector2(0, -gap - size * 0.5f));
        SetBranch(_left, new Vector2(size, thickness), new Vector2(-gap - size * 0.5f, 0));
        SetBranch(_right, new Vector2(size, thickness), new Vector2(gap + size * 0.5f, 0));
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

    // OnValidate ne tourne que dans l'éditeur — on ignore si les branches ne sont pas créées
    void OnValidate()
    {
        if (_top != null)
            UpdateCrosshair();
    }
}