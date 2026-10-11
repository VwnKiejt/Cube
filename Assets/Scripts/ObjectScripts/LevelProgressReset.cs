using UnityEngine;

public sealed class LevelProgressReset : MonoBehaviour
{
    [SerializeField] private string levelScenePath = "Assets/Scenes/GameScene.unity";
    [SerializeField] private UnityEngine.UI.Image progressFill;
    [SerializeField] private TMPro.TMP_Text percentageText;

    [ContextMenu("Reset Progress To 0%")]
    public void ResetProgress()
    {
        LevelProgressSave.Reset(levelScenePath);
        if (progressFill != null) progressFill.fillAmount = 0f;
        if (percentageText != null) percentageText.text = "0%";
    }
}
