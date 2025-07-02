using UnityEngine;
using System.Collections.Generic;

public class RandomSceneItemSelector : MonoBehaviour
{
    [Header("設置")]
    [Tooltip("是否在啟動時立即隨機選擇物件")]
    public bool activateOnStart = true;

    void Start()
    {
        if (activateOnStart)
        {
            SelectRandomSceneItem();
        }
    }

    public void SelectRandomSceneItem()
    {
        // 獲取所有符合條件的孫物件
        List<GameObject> validSceneItems = new List<GameObject>();
        
        // 遍歷所有子物件
        foreach (Transform child in transform)
        {
            // 遍歷每個子物件的子物件（孫物件）
            foreach (Transform grandchild in child)
            {
                if (grandchild.CompareTag("SceneItem"))
                {
                    validSceneItems.Add(grandchild.gameObject);
                    // 先全部隱藏
                    grandchild.gameObject.SetActive(false);
                }
            }
        }

        // 隨機選擇一個啟用
        if (validSceneItems.Count > 0)
        {
            int randomIndex = Random.Range(0, validSceneItems.Count);
            validSceneItems[randomIndex].SetActive(true);
            Debug.Log($"已隨機啟用: {validSceneItems[randomIndex].name}");
        }
        else
        {
            Debug.LogWarning($"未找到任何帶有'SceneItem'標籤的孫物件！", this);
        }
    }
}