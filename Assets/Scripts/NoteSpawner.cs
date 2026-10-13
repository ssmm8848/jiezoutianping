using System.Collections.Generic;
using UnityEngine;

public class NoteSpawner : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public ChartData chartData;
    public JudgeSystem judgeSystem;

    [Header("Note Prefab")]
    public GameObject notePrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Judge Points")]
    public Transform[] judgePoints;

    [Header("Roots")]
    public Transform noteRoot;

    [Header("Spawn Settings")]
    public bool useChartSpawnAheadTime = true;
    public float spawnAheadTime = 2f;

    [Header("Player Note Speed")]
    public bool useSavedNoteSpeed = true;
    public float noteSpeed = 8f;
    public float minNoteSpeed = 4f;
    public float maxNoteSpeed = 24f;

    private const string NoteSpeedKey = "GB_NoteSpeed";
    private const float PreviousMaxNoteSpeed = 12f;
    private const float PreviousFastestTravelTime = 1f;
    private const float NewFastestTravelTime = 0.55f;

    private int nextNoteIndex = 0;
    private readonly List<Note> spawnedNotes = new List<Note>();


    public List<Note> GetSpawnedNotes()
    {
        return spawnedNotes;
    }

    private void Start()
    {
        ApplyChartSettings();
        LoadSavedNoteSpeed();
    }

    private void ApplyChartSettings()
    {
        if (chartData == null)
        {
            return;
        }

        if (useChartSpawnAheadTime)
        {
            spawnAheadTime = chartData.spawnAheadTime;
        }

        Debug.Log($"NoteSpawner 使用提前生成时间：{spawnAheadTime}");
    }
    private void Update()
    {
        if (gameManager == null || !gameManager.IsPlaying())
        {
            return;
        }

        SpawnNotesByMusicTime();
    }

    private void SpawnNotesByMusicTime()
    {
        if (chartData == null || notePrefab == null)
        {
            return;
        }

        float currentMusicTime = gameManager.GetMusicTime();

        while (nextNoteIndex < chartData.notes.Count)
        {
            NoteData noteData = chartData.notes[nextNoteIndex];

            if (currentMusicTime + spawnAheadTime < noteData.time)
            {
                break;
            }

            SpawnNote(noteData);
            nextNoteIndex++;
        }
    }

    private void SpawnNote(NoteData noteData)
    {
        if (!IsValidLane(noteData.lane))
        {
            Debug.LogWarning($"音符轨道编号无效：lane={noteData.lane}");
            return;
        }

        Transform spawnPoint = spawnPoints[noteData.lane];
        Transform judgePoint = judgePoints[noteData.lane];

        Transform parent = noteRoot != null ? noteRoot : transform;

        GameObject noteObject = Instantiate(
            notePrefab,
            spawnPoint.position,
            Quaternion.identity,
            parent
        );

        Note note = noteObject.GetComponent<Note>();

        if (note == null)
        {
            Debug.LogError("生成的 NotePrefab 上没有 Note.cs 组件");
            Destroy(noteObject);
            return;
        }

        note.Initialize(
            noteData.time,
            noteData.lane,
            noteData.GetWeightType()
        );

        note.SetMovementData(
        spawnPoint.position,
        judgePoint,
        spawnAheadTime
        );
        note.SetGameManager(gameManager);
        note.SetJudgeSystem(judgeSystem);

        spawnedNotes.Add(note);

        Debug.Log($"生成音符：time={noteData.time}, lane={noteData.lane}, weight={noteData.GetWeightType()}");
    }

    private bool IsValidLane(int lane)
    {
        if (spawnPoints == null || judgePoints == null)
        {
            return false;
        }

        if (lane < 0)
        {
            return false;
        }

        if (lane >= spawnPoints.Length || lane >= judgePoints.Length)
        {
            return false;
        }

        if (spawnPoints[lane] == null || judgePoints[lane] == null)
        {
            return false;
        }

        return true;
    }

    public void SetSpawnAheadTime(float value)
    {
        spawnAheadTime = Mathf.Clamp(value, NewFastestTravelTime, 4.0f);
    }

    public float GetSpawnAheadTime()
    {
        return spawnAheadTime;
    }

    public void SetNoteSpeed(float value, bool save)
    {
        noteSpeed = Mathf.Clamp(value, minNoteSpeed, maxNoteSpeed);
        spawnAheadTime = ConvertNoteSpeedToSpawnAheadTime(noteSpeed);

        if (save)
        {
            PlayerPrefs.SetFloat(NoteSpeedKey, noteSpeed);
            PlayerPrefs.Save();
        }

        Debug.Log($"设置流速：{noteSpeed:F1}，SpawnAheadTime={spawnAheadTime:F2}");
    }

    public float GetNoteSpeed()
    {
        return noteSpeed;
    }

    private void LoadSavedNoteSpeed()
    {
        if (!useSavedNoteSpeed)
        {
            noteSpeed = ConvertSpawnAheadTimeToNoteSpeed(spawnAheadTime);
            return;
        }

        if (PlayerPrefs.HasKey(NoteSpeedKey))
        {
            float savedSpeed = PlayerPrefs.GetFloat(NoteSpeedKey, noteSpeed);
            SetNoteSpeed(savedSpeed, false);
        }
        else
        {
            noteSpeed = ConvertSpawnAheadTimeToNoteSpeed(spawnAheadTime);
        }
    }

    private float ConvertNoteSpeedToSpawnAheadTime(float speed)
    {
        if (speed <= PreviousMaxNoteSpeed)
        {
            float legacyT = Mathf.InverseLerp(minNoteSpeed, PreviousMaxNoteSpeed, speed);
            return Mathf.Lerp(3.2f, PreviousFastestTravelTime, legacyT);
        }

        float extendedT = Mathf.InverseLerp(PreviousMaxNoteSpeed, maxNoteSpeed, speed);
        return Mathf.Lerp(PreviousFastestTravelTime, NewFastestTravelTime, extendedT);
    }

    private float ConvertSpawnAheadTimeToNoteSpeed(float travelTime)
    {
        if (travelTime >= PreviousFastestTravelTime)
        {
            float legacyT = Mathf.InverseLerp(3.2f, PreviousFastestTravelTime, travelTime);
            return Mathf.Lerp(minNoteSpeed, PreviousMaxNoteSpeed, legacyT);
        }

        float extendedT = Mathf.InverseLerp(PreviousFastestTravelTime, NewFastestTravelTime, travelTime);
        return Mathf.Lerp(PreviousMaxNoteSpeed, maxNoteSpeed, extendedT);
    }

    public void ResetSpawner()
    {
        nextNoteIndex = 0;

        for (int i = 0; i < spawnedNotes.Count; i++)
        {
            if (spawnedNotes[i] != null)
            {
                Destroy(spawnedNotes[i].gameObject);
            }
        }

        spawnedNotes.Clear();
    }
}
