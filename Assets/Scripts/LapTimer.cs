using System;
using TMPro;
using UnityEngine;

public class LapTimer : MonoBehaviour
{
    private bool _started;
    private float _startTime;

    [Tooltip("Replaces {timer} with timer and {checkpoint}/{checkpointCount} with checkpoint info")] [TextArea] [SerializeField]
    private string _format;

    [SerializeField] private TextMeshProUGUI _text;

    [SerializeField] private Track _track;

    private void OnValidate()
    {
        _text ??= GetComponent<TextMeshProUGUI>();
    }

    public void Start()
    {
        RaceStartHandler.OnCountdownEnd.AddListener(NewLap);
        ApplyFormat();
    }

    private void NewLap()
    {
        _startTime = Time.time;
        _started = true;
    }

    private void ApplyFormat()
    {
        // format as 00:00.000
        var elapsedTime = TimeSpan.FromSeconds(Time.time - _startTime);

        _text.text = _format
            .Replace("{timer}", elapsedTime.ToString(@"mm\:ss\.fff"))
            .Replace("{checkpoint}", "0")
            .Replace("{checkpointCount}", _track.CheckpointPositions.Length.ToString());
    }

    private void Update()
    {
        if (!_started) return;
        ApplyFormat();
    }
}