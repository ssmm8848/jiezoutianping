using System;
using System.Collections.Generic;
using UnityEngine;

public enum WeightType
{
    Normal,
    Heavy,
    Light,
    Stabilize
}

public enum NoteType
{
    Tap
}

[Serializable]
public class NoteData
{
    public float time;
    public int lane;
    public NoteType type;
    public string weightType;

    public WeightType GetWeightType()
    {
        WeightType parsedWeight;
        if (Enum.TryParse(weightType, true, out parsedWeight) && Enum.IsDefined(typeof(WeightType), parsedWeight))
        {
            return parsedWeight;
        }

        return WeightType.Normal;
    }
}

[Serializable]
public class ChartJsonData
{
    public string songId;
    public string title;
    public string artist;
    public string audioFile;
    public string difficulty;
    public int level;
    public float bpm;
    public float offset;
    public float spawnAheadTime;
    public List<NoteData> notes;
}

public class ChartData : MonoBehaviour
{
    [Header("Chart File")]
    public TextAsset chartJsonFile;

    [Header("Chart Info")]
    public string songId;
    public string title;
    public string artist;
    public string audioFile;
    public string difficulty;
    public int level;
    public float bpm;
    public float offset;
    public float spawnAheadTime = 2f;

    [Header("Notes")]
    public List<NoteData> notes = new List<NoteData>();

    private void Start()
    {
        LoadChartFromJson();
    }

    public void LoadChartFromJson()
    {
        notes.Clear();

        if (SelectedSongData.Instance != null)
        {
            Debug.Log(
                $"收到选曲数据：difficulty={SelectedSongData.Instance.selectedDifficulty}, " +
                $"chart={SelectedSongData.Instance.selectedChart?.name}"
            );
        }
        else
        {
            Debug.LogWarning("没有找到 SelectedSongData.Instance，将使用默认谱面");
        }

        if (SelectedSongData.Instance != null && SelectedSongData.Instance.selectedChart != null)
        {
            chartJsonFile = SelectedSongData.Instance.selectedChart;
            Debug.Log(
    $"ChartData 最终使用文件：{chartJsonFile.name}\n" +
    $"文件内容开头：{chartJsonFile.text.Substring(0, Mathf.Min(200, chartJsonFile.text.Length))}"
);
        }

        if (chartJsonFile == null)
        {
            Debug.LogError("ChartData 没有绑定谱面 JSON 文件");
            return;
        }

        ChartJsonData chartJsonData = JsonUtility.FromJson<ChartJsonData>(chartJsonFile.text);

        if (chartJsonData == null)
        {
            Debug.LogError("谱面 JSON 解析失败");
            return;
        }

        songId = chartJsonData.songId;
        title = chartJsonData.title;
        artist = chartJsonData.artist;
        audioFile = chartJsonData.audioFile;
        difficulty = chartJsonData.difficulty;
        level = chartJsonData.level;
        bpm = chartJsonData.bpm;
        offset = chartJsonData.offset;
        spawnAheadTime = chartJsonData.spawnAheadTime;

        if (chartJsonData.notes != null)
        {
            notes = chartJsonData.notes;
        }
        else
        {
            notes = new List<NoteData>();
        }

        Debug.Log($"谱面读取成功：{title} / {difficulty} / Notes={notes.Count}");
        LogWeightDistribution();
    }

    private void LogWeightDistribution()
    {
        int normalCount = 0;
        int heavyCount = 0;
        int lightCount = 0;
        int stabilizeCount = 0;

        for (int i = 0; i < notes.Count; i++)
        {
            NoteData note = notes[i];
            if (note == null)
            {
                continue;
            }

            switch (note.GetWeightType())
            {
                case WeightType.Heavy: heavyCount++; break;
                case WeightType.Light: lightCount++; break;
                case WeightType.Stabilize: stabilizeCount++; break;
                default: normalCount++; break;
            }
        }

        Debug.Log($"重量谱面统计：Normal={normalCount}, Heavy={heavyCount}, Light={lightCount}, Stabilize={stabilizeCount}");
    }

    public void PrintChart()
    {
        Debug.Log($"当前谱面：{title} - {difficulty}");
        Debug.Log($"BPM={bpm}, Offset={offset}, SpawnAheadTime={spawnAheadTime}");
        Debug.Log($"音符数量：{notes.Count}");

        for (int i = 0; i < notes.Count; i++)
        {
            NoteData note = notes[i];
            Debug.Log($"音符 {i}: time={note.time}, lane={note.lane}, type={note.type}, weight={note.GetWeightType()}");
        }
    }
}
