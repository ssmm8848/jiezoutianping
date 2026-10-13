using System.Collections.Generic;
using UnityEngine;

public class RhythmInput : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public NoteSpawner noteSpawner;
    public JudgeSystem judgeSystem;
    public JudgeLineController judgeLineController;

    [Header("Input Keys")]
    public KeyCode lane0Key = KeyCode.S;
    public KeyCode lane1Key = KeyCode.D;
    public KeyCode lane2Key = KeyCode.J;
    public KeyCode lane3Key = KeyCode.K;
    public KeyCode calibrationKey = KeyCode.Space;

    [Header("Search Settings")]
    public float searchWindow = 0.2f;


    private void Update()
    {
        if (gameManager == null || !gameManager.IsPlaying())
        {
            return;
        }

        if (Input.GetKeyDown(lane0Key))
        {
            TryHitLane(0);
        }

        if (Input.GetKeyDown(lane1Key))
        {
            TryHitLane(1);
        }

        if (Input.GetKeyDown(lane2Key))
        {
            TryHitLane(2);
        }

        if (Input.GetKeyDown(lane3Key))
        {
            TryHitLane(3);
        }

        if (Input.GetKeyDown(calibrationKey))
        {
            TryUseCalibration();
        }
    }

    private void TryUseCalibration()
    {
        if (judgeLineController == null)
        {
            Debug.LogWarning("RhythmInput 缺少 JudgeLineController 引用");
            return;
        }

        float bpm = gameManager.chartData != null ? gameManager.chartData.bpm : 0f;
        float beatDuration = bpm > 0f ? 60f / bpm : 0.5f;
        judgeLineController.TryUseCalibration(beatDuration * 4f);
    }

    private void TryHitLane(int lane)
    {
        Note nearestNote = FindNearestNoteInLane(lane);

        if (nearestNote == null)
        {
            Debug.Log($"Lane {lane}: 当前轨道没有可判定音符");
            return;
        }

        if (judgeSystem == null)
        {
            Debug.LogWarning("RhythmInput 缺少 JudgeSystem 引用");
            return;
        }

        judgeSystem.Judge(nearestNote);
    }

    private Note FindNearestNoteInLane(int lane)
    {
        if (noteSpawner == null)
        {
            return null;
        }

        List<Note> notes = noteSpawner.GetSpawnedNotes();

        Note nearestNote = null;
        float nearestTimeDiff = float.MaxValue;
        float currentMusicTime = judgeSystem != null
            ? judgeSystem.GetAdjustedMusicTime()
            : gameManager.GetMusicTime();

        for (int i = 0; i < notes.Count; i++)
        {
            Note note = notes[i];

            if (note == null)
            {
                continue;
            }

            if (note.hasBeenHit || note.hasMissed)
            {
                continue;
            }

            if (note.lane != lane)
            {
                continue;
            }

            float timeDiff = Mathf.Abs(currentMusicTime - note.hitTime);

            if (timeDiff > searchWindow)
            {
                continue;
            }

            if (timeDiff < nearestTimeDiff)
            {
                nearestTimeDiff = timeDiff;
                nearestNote = note;
            }
        }

        return nearestNote;
    }
}
