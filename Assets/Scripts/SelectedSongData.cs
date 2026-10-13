using UnityEngine;

public class SelectedSongData : MonoBehaviour
{
    public static SelectedSongData Instance;

    public SongConfig selectedSong;
    public TextAsset selectedChart;
    public string selectedDifficulty = "Normal";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetSelection(SongConfig songConfig, TextAsset chart, string difficulty)
    {
        selectedSong = songConfig;
        selectedChart = chart;
        selectedDifficulty = difficulty;

        Debug.Log(
    $"保存选曲结果：song={songConfig?.title}, " +
    $"difficulty={difficulty}, chart={chart?.name}"
);
    }
}