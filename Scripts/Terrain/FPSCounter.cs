using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    private float deltaTime = 0.0f;

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        int fps = Mathf.RoundToInt(1.0f / deltaTime);
        GUI.Label(new Rect(10, 10, 150, 30), $"FPS: {fps}", new GUIStyle { fontSize = 20, normal = { textColor = Color.white } });
    }
}
