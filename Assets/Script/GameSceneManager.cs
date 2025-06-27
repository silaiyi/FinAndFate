using UnityEngine;

public class GameSceneManager : MonoBehaviour
{
    void Start()
    {
        // 确保OutlineManager存在
        if (OutlineManager.Instance == null)
        {
            GameObject outlineManagerObj = new GameObject("OutlineManager");
            outlineManagerObj.AddComponent<OutlineManager>();
        }
    }
}