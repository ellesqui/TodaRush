
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public Slider progressBar;
    public TMP_Text progressText;
    public GameObject continueHint;
    public RectTransform progressFill;   // LoadingBarFill, inside the track
    public RectTransform tricycleIcon;   // rides on the right edge of the fill
    public Animator logoAnimator;        // Logo bounce, enabled when loading starts

    private static string targetScene = "MainMap";

    public static void LoadThroughLoadingScreen(string sceneName)
    {
        targetScene = sceneName;
        SceneManager.LoadScene("LoadingScene");
    }

    void LateUpdate()
    {
        if (progressFill == null || tricycleIcon == null) return;

        // World position of the fill's right edge, keeping the icon's own height
        Vector3 edge = progressFill.TransformPoint(new Vector3(progressFill.rect.xMax, progressFill.rect.center.y, 0f));
        tricycleIcon.position = new Vector3(edge.x, tricycleIcon.position.y, tricycleIcon.position.z);
    }

    async void Start()
    {
        if (logoAnimator != null) logoAnimator.enabled = true;
        await LoadSelectedScene();
    }

    private async Task LoadSelectedScene()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetScene);

        operation.allowSceneActivation = false;

        // Smooth fake progress: rises every frame up to 90% (about 9 seconds)
        float shown = 0f;
        while (shown < 0.9f)
        {
            shown = Mathf.MoveTowards(shown, 0.9f, Time.deltaTime * 0.1f);
            progressBar.value = shown;
            progressText.text = Mathf.RoundToInt(shown * 100f).ToString() + "%";

            await Task.Yield();
        }

        while (operation.progress < 0.9f)
        {
            await Task.Yield();
        }

        progressBar.value = 0.99f;
        progressText.text = "99%";

        continueHint.SetActive(true);

        while (!Input.anyKeyDown)
        {
            await Task.Yield();
        }

        operation.allowSceneActivation = true;
    }
}
