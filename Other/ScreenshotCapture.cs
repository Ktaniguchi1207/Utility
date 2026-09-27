#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections;
public class ScreenshotCapture : MonoBehaviour {
    [System.Serializable]
    public class CaptureSettings {
        public Vector2 position = new Vector2(100, 100);
        public Vector2 size = new Vector2(800, 600);
        public Color overlayColor = new Color(0, 1, 0, 0.3f);
    }

    [SerializeField] CaptureSettings settings = new CaptureSettings();
    [SerializeField] KeyCode captureKey = KeyCode.F12;
    [SerializeField] string folderPath = "Assets/Screenshots";
    [SerializeField] bool showOverlay = true;

    bool isCapturing;

    void Update() {
        if (Input.GetKeyDown(captureKey) && !isCapturing) {
            StartCoroutine(CaptureScreenshotCoroutine());
        }
    }

    IEnumerator CaptureScreenshotCoroutine() {
        isCapturing = true;
        yield return new WaitForEndOfFrame();

        if (!Directory.Exists(folderPath)) {
            Directory.CreateDirectory(folderPath);
        }

        int fileNumber = GetNextFileNumber();
        string sceneName = SceneManager.GetActiveScene().name;
        string fileName = $"{folderPath}/{sceneName}_{fileNumber:000}.png";

        Texture2D screenshot = CaptureArea();
        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(fileName, bytes);
        Destroy(screenshot);
        Debug.Log($"スクリーンショット保存: {fileName}");
        isCapturing = false;
    }

    Texture2D CaptureArea() {
        float clampedX = Mathf.Clamp(settings.position.x, 0, Screen.width);
        float clampedY = Mathf.Clamp(settings.position.y, 0, Screen.height);
        float clampedWidth = Mathf.Min(settings.size.x, Screen.width - clampedX);
        float clampedHeight = Mathf.Min(settings.size.y, Screen.height - clampedY);

        if (clampedWidth <= 0 || clampedHeight <= 0) {
            Debug.LogError("スクリーンショット範囲が無効です。範囲を確認してください。");
            return new Texture2D(1, 1);
        }

        Rect readRect = new Rect(
            clampedX,
            Screen.height - clampedY - clampedHeight,
            clampedWidth,
            clampedHeight
        );

        Texture2D texture = new Texture2D((int)clampedWidth, (int)clampedHeight, TextureFormat.RGB24, false);
        texture.ReadPixels(readRect, 0, 0);
        texture.Apply();
        return texture;
    }

    public void CaptureScreenshotImmediate() {
        Camera camera = Camera.main;
        if (camera == null) {
            Debug.LogError("Main Camera not found");
            return;
        }

        if (!Directory.Exists(folderPath)) {
            Directory.CreateDirectory(folderPath);
        }

        int fileNumber = GetNextFileNumber();
        string sceneName = SceneManager.GetActiveScene().name;
        string fileName = $"{folderPath}/{sceneName}_{fileNumber:000}.png";

        int screenWidth = (int)settings.size.x;
        int screenHeight = (int)settings.size.y;

        float clampedX = Mathf.Clamp(settings.position.x, 0, Screen.width);
        float clampedY = Mathf.Clamp(settings.position.y, 0, Screen.height);
        float clampedWidth = Mathf.Min(settings.size.x, Screen.width - clampedX);
        float clampedHeight = Mathf.Min(settings.size.y, Screen.height - clampedY);

        if (clampedWidth <= 0 || clampedHeight <= 0) {
            Debug.LogError("スクリーンショット範囲が無効です。");
            return;
        }

        RenderTexture rt = new RenderTexture(screenWidth, screenHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture prevRT = camera.targetTexture;
        RenderTexture prevActive = RenderTexture.active;

        camera.targetTexture = rt;
        camera.Render();

        RenderTexture.active = rt;

        Texture2D screenshot = new Texture2D((int)clampedWidth, (int)clampedHeight, TextureFormat.RGB24, false);
        screenshot.ReadPixels(new Rect(0, 0, clampedWidth, clampedHeight), 0, 0);
        screenshot.Apply();

        camera.targetTexture = prevRT;
        RenderTexture.active = prevActive;
        rt.Release();

        byte[] bytes = screenshot.EncodeToPNG();
        File.WriteAllBytes(fileName, bytes);
        DestroyImmediate(screenshot);

        Debug.Log($"スクリーンショット保存: {fileName}");
    }

    int GetNextFileNumber() {
        if (!Directory.Exists(folderPath)) {
            return 1;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        string[] files = Directory.GetFiles(folderPath, $"{sceneName}_*.png");

        if (files.Length == 0) {
            return 1;
        }

        int maxNumber = 0;
        foreach (string file in files) {
            string fileName = Path.GetFileNameWithoutExtension(file);
            string[] parts = fileName.Split('_');
            if (parts.Length >= 2) {
                if (int.TryParse(parts[parts.Length - 1], out int number)) {
                    maxNumber = Mathf.Max(maxNumber, number);
                }
            }
        }

        return maxNumber + 1;
    }

    void OnGUI() {
        if (!showOverlay) return;

        Rect overlayRect = new Rect(settings.position.x, settings.position.y, settings.size.x, settings.size.y);

        Color originalColor = GUI.color;
        GUI.color = settings.overlayColor;
        GUI.Box(overlayRect, "");
        GUI.color = originalColor;
    }
}
#endif
