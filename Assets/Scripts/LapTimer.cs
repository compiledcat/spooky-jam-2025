using System;
using TMPro;
using UnityEngine;

public class LapTimer : MonoBehaviour
{
    private bool _started;
    private float _startTime;

    [SerializeField] private string _suffix;
    [SerializeField] private TextMeshProUGUI _text;

    private void OnValidate()
    {
        _text ??= GetComponent<TextMeshProUGUI>();
    }

    public void Start()
    {
        RaceStartHandler.OnCountdownEnd.AddListener(NewLap);
    }

    private void NewLap()
    {
        _startTime = Time.time;
        _started = true;
    }

    private void Update()
    {
        if (!_started) return;

        // format as 00:00.000
        var elapsedTime = TimeSpan.FromSeconds(Time.time - _startTime);
        _text.text = _suffix + elapsedTime.ToString(@"mm\:ss\.fff");
    }
}