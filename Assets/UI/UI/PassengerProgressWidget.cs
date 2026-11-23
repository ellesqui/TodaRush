using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// PASAHERO progress widget. Recreates the HTML reference (.cartoon-panel) in uGUI.
// Counts unique passengers that successfully boarded this level.
// A passenger who falls out (patience) turns their seat gray again.
// The seat hierarchy is saved in the scene. Initialize() rebuilds it from scratch.
public class PassengerProgressWidget : MonoBehaviour
{
    [Header("Person Icons (existing TodaRushUI sprites)")]
    public Sprite greenPersonSprite;
    public Sprite grayPersonSprite;

    [Header("Generated UI Shapes (Assets/UI/PassengerProgress)")]
    public Sprite panelBorderSprite;
    public Sprite seatRingSprite;

    [Header("Font")]
    public TMP_FontAsset bungeeFont;

    [Header("Palette")]
    public Color warmWhite = new Color32(255, 248, 235, 255);
    public Color charcoal = new Color32(37, 39, 43, 255);

    [Header("Targets")]
    public int minSeats = 3;
    public int maxSeats = 5;

    private readonly HashSet<PassengerPickup> counted = new HashSet<PassengerPickup>();
    private readonly Dictionary<PassengerPickup, int> slotOf = new Dictionary<PassengerPickup, int>();
    private readonly List<RectTransform> seatRects = new List<RectTransform>();
    private readonly List<Image> seatIcons = new List<Image>();
    private int targetSeats;
    private int filledCount;
    private bool built;
    private readonly List<Image> seatBorders = new List<Image>();
    private readonly List<PassengerPickup> pendingPax = new List<PassengerPickup>();
    private readonly List<Image> pendingBorders = new List<Image>();

    [Header("Pending Delivery Glow")]
    public Color pendingGlowColor = new Color32(255, 194, 71, 255);
    public float pendingGlowSpeed = 2.5f;

    public int TargetSeats { get { return targetSeats; } }
    public int FilledCount { get { return filledCount; } }

    // Called by UIManager once the level's GameManager and Queue are known.
    public void Initialize(GameManager gameManager, Queue queue)
    {
        if (built)
        {
            return;
        }

        int wanted = Mathf.Clamp(gameManager != null ? gameManager.CalculateRequiredPassengers() : minSeats, minSeats, maxSeats);
        targetSeats = wanted;
        var existing = transform.Find("Seats");

        if (existing != null && existing.childCount == wanted)
        {
            // Reuse the hierarchy already saved in the scene so Play mode matches what you set up.
            seatRects.Clear();
            seatIcons.Clear();
            seatBorders.Clear();
            for (int i = 0; i < existing.childCount; i++)
            {
                var seat = (RectTransform)existing.GetChild(i);
                seatRects.Add(seat);
                seatIcons.Add(seat.Find("PersonIcon").GetComponent<Image>());
                seatBorders.Add(seat.Find("SeatBorder").GetComponent<Image>());
            }
        }
        else
        {
            // Rebuild from scratch only when the scene has no matching hierarchy.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            var hlgOld = GetComponent<HorizontalLayoutGroup>(); if (hlgOld != null) DestroyImmediate(hlgOld);
            var fitOld = GetComponent<ContentSizeFitter>(); if (fitOld != null) DestroyImmediate(fitOld);
            seatRects.Clear();
            seatIcons.Clear();
            seatBorders.Clear();
            BuildPanel();
        }
        UpdateCounter();

        // Borders start hidden; each one appears when its passenger is picked up.
        foreach (var b in seatBorders) b.gameObject.SetActive(false);

        if (true) // passengers are found in the scene, not from the Queue list
        {
            foreach (PassengerPickup pax in FindObjectsByType<PassengerPickup>(FindObjectsInactive.Include))
            {
                if (pax == null)
                {
                    continue;
                }

                PassengerPickup captured = pax;
                // Passenger UnityEvent fires once, at the moment the passenger has boarded.
                captured.Passenger.AddListener(() => OnPassengerBoarded(captured));
            }
        }

        built = true;
    }

    public void OnPassengerBoarded(PassengerPickup pax)
    {
        if (pax == null || !counted.Add(pax))
        {
            return; // duplicate event or same passenger: count once only
        }

        int slot = FirstFreeSlot();
        if (slot < 0)
        {
            return; // no free seat
        }

        slotOf[pax] = slot;
        SetSeatFilled(slot, true, true);
        Image border = seatBorders[slot];
        border.gameObject.SetActive(true);
        pendingPax.Add(pax);
        pendingBorders.Add(border);
        filledCount = slotOf.Count;
        UpdateCounter();
    }

    // PATIENCE: a passenger fell out. Their seat turns gray and the count goes down.
    public void OnPassengerLost(PassengerPickup pax)
    {
        if (pax == null || !counted.Remove(pax))
        {
            return;
        }

        if (!slotOf.TryGetValue(pax, out int slot))
        {
            return;
        }

        slotOf.Remove(pax);
        SetSeatFilled(slot, false, false);
        seatBorders[slot].gameObject.SetActive(false);
        filledCount = slotOf.Count;
        UpdateCounter();
    }

    // First seat that is not used by a passenger yet
    private int FirstFreeSlot()
    {
        for (int i = 0; i < seatRects.Count; i++)
        {
            if (!slotOf.ContainsValue(i)) return i;
        }
        return -1;
    }

    // Pending-delivery glow: loops while the passenger is onboard. Stops when the passenger is delivered.
    private void Update()
    {
        for (int i = pendingPax.Count - 1; i >= 0; i--)
        {
            PassengerPickup pax = pendingPax[i];
            Image border = pendingBorders[i];
            if (pax == null || pax.targetDestination == null)
            {
                // Delivered (or fell out): stop the glow and hide the border again.
                if (border != null) border.gameObject.SetActive(false);
                pendingPax.RemoveAt(i);
                pendingBorders.RemoveAt(i);
                continue;
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pendingGlowSpeed * Mathf.PI * 2f);
            border.color = Color.Lerp(charcoal, pendingGlowColor, pulse);
        }
    }

    private void BuildPanel()
    {
        // Anchor/pivot/position are set in the scene, not overridden here.

        // Root holds the content row: padding 16 / 24, gap 24 (HTML .cartoon-panel).
        var hlg = gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(24, 24, 16, 16);
        hlg.spacing = 24;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        var fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Gradient outline only (the black 50% fill and drop shadow were removed on purpose).

        // Title
        var title = CreateText(transform, "PassengerTitle", "PASAHERO", 24, warmWhite);
        title.characterSpacing = 1f;
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        title.gameObject.AddComponent<LayoutElement>();

        // Seats row (gap 12)
        var seatsGo = new GameObject("Seats", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        seatsGo.transform.SetParent(transform, false);
        var seatsHlg = seatsGo.GetComponent<HorizontalLayoutGroup>();
        seatsHlg.spacing = 12;
        seatsHlg.childAlignment = TextAnchor.MiddleCenter;
        seatsHlg.childControlWidth = true;
        seatsHlg.childControlHeight = true;
        seatsHlg.childForceExpandWidth = false;
        seatsHlg.childForceExpandHeight = false;
        for (int i = 0; i < targetSeats; i++)
        {
            CreateSeat(seatsGo.transform, i);
        }

    }

    private void CreateBackground(string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(transform, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.color = color;
        img.raycastTarget = false;
        go.GetComponent<LayoutElement>().ignoreLayout = true;
        go.transform.SetAsFirstSibling();
    }

    private void CreateSeat(Transform parent, int index)
    {
        var seat = new GameObject("Seat_" + (index + 1), typeof(RectTransform), typeof(LayoutElement));
        seat.transform.SetParent(parent, false);
        var seatRect = (RectTransform)seat.transform;
        seatRect.sizeDelta = new Vector2(54, 54);
        var le = seat.GetComponent<LayoutElement>();
        le.preferredWidth = 54;
        le.preferredHeight = 54;

        // Person icon. Original sprite colors are kept (only the sprite reference changes).
        Image icon = CreateImage(seat.transform, "PersonIcon", grayPersonSprite, Color.white, new Vector2(54, 54), Vector2.zero, true).GetComponent<Image>();
        // Charcoal 4px border.
        seatBorders.Add(CreateImage(seat.transform, "SeatBorder", seatRingSprite, charcoal, new Vector2(54, 54), Vector2.zero).GetComponent<Image>());

        seatRects.Add(seatRect);
        seatIcons.Add(icon);
        SetSeatFilled(index, false, false); // all slots start gray
    }

    private void SetSeatFilled(int index, bool filled, bool animate)
    {
        if (index < 0 || index >= seatRects.Count)
        {
            return;
        }

        seatIcons[index].sprite = filled ? greenPersonSprite : grayPersonSprite;

        if (animate && filled)
        {
            StartCoroutine(PopIn(seatRects[index])); // only the newly activated seat animates
        }
    }

    // scale 0.8 -> 1 over 0.3s with a slight overshoot (HTML popIn)
    private IEnumerator PopIn(RectTransform rect)
    {
        const float duration = 0.3f;
        const float c1 = 1.70158f, c3 = c1 + 1f;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float u = t - 1f;
            float eased = 1f + c3 * u * u * u + c1 * u * u;
            float s = Mathf.Lerp(0.8f, 1f, eased);
            rect.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rect.localScale = Vector3.one;
    }

    private void UpdateCounter()
    {
    }

    private GameObject CreateImage(Transform parent, string name, Sprite sprite, Color color, Vector2 size, Vector2 offset, bool preserve = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = offset;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.preserveAspect = preserve;
        img.raycastTarget = false;
        return go;
    }

    private TMP_Text CreateText(Transform parent, string name, string text, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.font = bungeeFont;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.richText = true;
        // Charcoal outline (~2px) and a small hard shadow, matching the HTML stroke + text-shadow.
        if (bungeeFont != null)
        {
            Material mat = tmp.fontMaterial;
            mat.SetColor(ShaderUtilities.ID_OutlineColor, charcoal);
            mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.12f);
            mat.EnableKeyword("UNDERLAY_ON");
            mat.SetColor(ShaderUtilities.ID_UnderlayColor, charcoal);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.08f);
            mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.08f);
        }
        tmp.text = text;
        return tmp;
    }
}
