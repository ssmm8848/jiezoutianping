using UnityEngine;

public class NoteVisualTester : MonoBehaviour
{
    public Note note;

    private void Update()
    {
        if (note == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            note.Initialize(1f, 0, WeightType.Normal);
            Debug.Log("切换为 Normal 音符");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            note.Initialize(1f, 0, WeightType.Heavy);
            Debug.Log("切换为 Heavy 音符");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            note.Initialize(1f, 0, WeightType.Light);
            Debug.Log("切换为 Light 音符");
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            note.Initialize(1f, 0, WeightType.Stabilize);
            Debug.Log("切换为 Stabilize 音符");
        }
    }
}