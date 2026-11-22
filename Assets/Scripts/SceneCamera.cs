using UnityEngine;

public class SceneCamera : MonoBehaviour
{
    public static SceneCamera Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void SetActive(bool state)
    {
        gameObject.SetActive(state);
    }
}