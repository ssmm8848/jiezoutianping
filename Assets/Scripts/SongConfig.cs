using UnityEngine;

[CreateAssetMenu(fileName = "NewSongConfig", menuName = "Gravity Beat/Song Config")]
public class SongConfig : ScriptableObject
{
    [Header("Song Info")]
    public string songId;
    public string title;
    public string artist;

    [Header("Visual")]
    public Sprite coverSprite;

    [Header("Audio")]
    public AudioClip audioClip;

    [Min(0f)]
    [Tooltip("选曲试听开始的位置（秒），建议设在歌曲副歌或高潮段落。")]
    public float previewStartTime = 0f;

    [Min(1f)]
    [Tooltip("选曲试听片段的长度（秒）。")]
    public float previewDuration = 16f;

    [Header("Charts")]
    public TextAsset easyChart;
    public TextAsset normalChart;
    public TextAsset hardChart;
}
