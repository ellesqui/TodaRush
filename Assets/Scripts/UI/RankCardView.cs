using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Connects an EXISTING Rank UI to the GLOBAL Tsuper profile (ScoreManager.TotalXP).
// Two styles, detected from the children that exist:
// - Section style: Win / Game Over panels ("RankSection" with RankHeader, RankBarBg, NextRankText).
// - Card style: LevelSelection Tsuper Status card ("TsuperStatusCard" with RankTitle, XPBarBorder, NextRankLabel).
// On the result panels (isResultPanel = true), the bar animates once per committed run.
public class RankCardView : MonoBehaviour
{
    public bool isResultPanel = false;   // true on Win and Game Over rank sections only
    public float animSeconds = 1.2f;     // total time of the bar animation

    private bool isSection;
    private RectTransform barFill;
    private float fullBarWidth;
    private TMP_Text titleText, barText, nextText;
    private RectTransform medalRt;
    private Coroutine animRoutine;

    void Awake()
    {
        if (transform.Find("RankBarBg") != null)
        {
            isSection = true;
            Transform bar = transform.Find("RankBarBg");
            barFill = bar.Find("RankBarFill") as RectTransform;
            fullBarWidth = 394f;   // 400 wide minus the 3px border on each side
            barText = bar.Find("RankBarText").GetComponent<TMP_Text>();
            titleText = transform.Find("RankHeader").GetComponent<TMP_Text>();
            medalRt = transform.Find("RankHeader/MedalIcon") as RectTransform;
            nextText = transform.Find("NextRankText").GetComponent<TMP_Text>();
        }
        else
        {
            isSection = false;
            barFill = transform.Find("XPBarBorder/XPBarTrack/XPBarFill") as RectTransform;
            fullBarWidth = 276f;   // track inner width
            barText = transform.Find("XPBarBorder/XPBarTrack/XPText").GetComponent<TMP_Text>();
            titleText = transform.Find("RankTitle").GetComponent<TMP_Text>();
            medalRt = transform.Find("MedalIcon") as RectTransform;
            nextText = transform.Find("NextRankLabel").GetComponent<TMP_Text>();
        }
    }

    void OnEnable()
    {
        if (isResultPanel)
        {
            ScoreManager sm = FindAnyObjectByType<ScoreManager>();
            if (sm != null)
            {
                // The run must be committed before we read the profile (idempotent)
                sm.CommitRunIfEnded();

                if (sm.hasPendingRankResult)
                {
                    sm.hasPendingRankResult = false;   // play this run's animation only once
                    if (animRoutine != null) StopCoroutine(animRoutine);
                    animRoutine = StartCoroutine(AnimateRun(sm.runPreviousTotalXP, sm.runNewTotalXP));
                    return;
                }
            }
        }

        ShowTotal(ScoreManager.GetTotalXP());
    }

    void OnDisable()
    {
        if (animRoutine != null)
        {
            StopCoroutine(animRoutine);
            animRoutine = null;
        }
    }

    // Shows the given TotalXP with no animation
    void ShowTotal(int totalXP)
    {
        int rank = ScoreManager.GetRankIndex(totalXP);
        float p = ScoreManager.RankProgress(totalXP, out int cur, out int req);
        ApplyState(rank, p, cur, req);
    }

    // Animates the bar from the XP before the run to the XP after it. Crossing a rank shows a Rank Up.
    IEnumerator AnimateRun(int prevTotal, int newTotal)
    {
        int prevRank = ScoreManager.GetRankIndex(prevTotal);
        int newRank = ScoreManager.GetRankIndex(newTotal);

        float startP = ScoreManager.RankProgress(prevTotal, out int startCur, out int startReq);
        float endP = ScoreManager.RankProgress(newTotal, out int endCur, out int endReq);

        int rank = prevRank;
        float p = startP;
        ApplyState(rank, p, startCur, startReq);

        // Each rank crossed: fill the old rank's bar, then show the new rank from zero
        for (int r = prevRank + 1; r <= newRank; r++)
        {
            yield return Tween(rank, p, 1f, animSeconds * 0.5f);
            rank = r;
            p = 0f;
            ApplyState(rank, p, 0, 0);
            ShowRankUp(rank);
            yield return new WaitForSecondsRealtime(0.8f);
        }

        // Last part: the current rank from its starting progress (or zero) to the final progress
        float fromP = (prevRank == newRank) ? startP : 0f;
        float segment = (prevRank == newRank) ? animSeconds : animSeconds * 0.5f;
        yield return Tween(rank, fromP, endP, segment, true);

        // Final, exact values from the profile
        ShowTotal(newTotal);
        animRoutine = null;
    }

    IEnumerator Tween(int rank, float from, float to, float seconds, bool isFinal = false)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / seconds);
            float p = Mathf.Lerp(from, to, k);
            ApplyState(rank, p, 0, 0, true);
            yield return null;
        }
    }

    // Writes the title, medal position, bar width, and texts.
    // When 'fromTween' is true, the XP text is calculated from the progress (so it moves with the bar).
    void ApplyState(int rank, float progress, int cur, int req, bool fromTween = false)
    {
        bool isMax = ScoreManager.IsMaxRank(rank);

        if (titleText != null)
        {
            titleText.text = ScoreManager.RankTitles[rank].ToUpperInvariant();
        }

        // Medal sits to the left of the title (section style), same spacing as the original design
        if (isSection && medalRt != null && titleText != null)
        {
            float w = titleText.GetPreferredValues(titleText.text).x;
            medalRt.anchoredPosition = new Vector2(-(w / 2f) - 16f, medalRt.anchoredPosition.y);
        }

        if (barFill != null)
        {
            barFill.sizeDelta = new Vector2(fullBarWidth * Mathf.Clamp01(progress), barFill.sizeDelta.y);
        }

        if (barText != null)
        {
            if (isMax)
            {
                barText.text = "MAX RANK";
            }
            else
            {
                int shownCur = fromTween ? Mathf.RoundToInt(progress * (ScoreManager.RankThresholds[rank + 1] - ScoreManager.RankThresholds[rank])) : cur;
                int shownReq = ScoreManager.RankThresholds[rank + 1] - ScoreManager.RankThresholds[rank];
                barText.text = shownCur.ToString("N0", CultureInfo.InvariantCulture) + " / " + shownReq.ToString("N0", CultureInfo.InvariantCulture) + " XP";
            }
        }

        if (nextText != null)
        {
            if (isMax)
            {
                nextText.text = isSection ? "MAX RANK" : "<color=#FFC247>MAX RANK</color>";
            }
            else
            {
                string nextTitle = ScoreManager.RankTitles[rank + 1].ToUpperInvariant();
                nextText.text = isSection
                    ? "NEXT RANK: " + nextTitle
                    : "NEXT: <color=#FFC247>" + nextTitle + "</color>";
            }
        }
    }

    // Short "RANK UP!" text on the next-rank line
    void ShowRankUp(int newRank)
    {
        if (nextText == null) return;
        nextText.text = "RANK UP!";
    }
}
