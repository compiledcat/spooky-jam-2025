using PrimeTween;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class RaceStartHandler : MonoBehaviour
{
    public static UnityEvent OnCountdownBegin = new();
    public static UnityEvent OnCountdownEnd = new();

    [SerializeField] private RectTransform _lapTimer;
    [SerializeField] private RectTransform _leaderboard;

    [SerializeField] private Transform _title;
    [SerializeField] private Transform _pressStart;

    [SerializeField] private Transform _one;
    [SerializeField] private Transform _two;
    [SerializeField] private Transform _three;

    private InputAction _moveAction;

    private float _lapTimerStartingY;
    private float _leaderboardStartingX;

    private void Start()
    {
        _moveAction = InputSystem.actions.FindAction("Move");

        _lapTimerStartingY = _lapTimer.anchoredPosition.y;
        _lapTimer.anchoredPosition = new Vector2(_lapTimer.anchoredPosition.x, _lapTimer.sizeDelta.y);

        _leaderboardStartingX = _leaderboard.anchoredPosition.x;
        _leaderboard.anchoredPosition = new Vector2(-_leaderboard.sizeDelta.x, _leaderboard.anchoredPosition.y);

        _one.localScale = Vector3.zero;
        _two.localScale = Vector3.zero;
        _three.localScale = Vector3.zero;

        OnCountdownBegin.AddListener(AnimateCountdown);
    }

    private void Update()
    {
        var anyKeyPressed = Keyboard.current.anyKey.wasPressedThisFrame || Mouse.current.leftButton.wasPressedThisFrame;

        if (anyKeyPressed)
        {
            OnCountdownBegin.Invoke();
            OnCountdownBegin.RemoveListener(AnimateCountdown);
        }
    }

    [ContextMenu("StartCountdown")] // for debugging
    private void AnimateCountdown()
    {
        Tween.Scale(_pressStart, 0.0f, 0.5f, Ease.InOutCubic);

        Sequence.Create()
            .Chain(Tween.Scale(_three, 1.0f, 0.5f, Ease.InOutCubic))
            .Group(Tween.LocalEulerAngles(_three, new Vector3(0, 180, 0), new Vector3(0, 0, 0), 0.5f, Ease.InOutCubic))
            .ChainDelay(0.25f)
            .Chain(Tween.Scale(_three, 0.0f, 0.25f, Ease.InOutCubic))
            .Chain(Tween.Scale(_two, 1.0f, 0.5f, Ease.InOutCubic))
            .Group(Tween.LocalEulerAngles(_two, new Vector3(0, 180, 0), new Vector3(0, 0, 0), 0.5f, Ease.InOutCubic))
            .ChainDelay(0.25f)
            .Chain(Tween.Scale(_two, 0.0f, 0.25f, Ease.InOutCubic))
            .Chain(Tween.Scale(_one, 1.0f, 0.5f, Ease.InOutCubic))
            .Group(Tween.LocalEulerAngles(_one, new Vector3(0, 180, 0), new Vector3(0, 0, 0), 0.5f, Ease.InOutCubic))
            .ChainDelay(0.25f)
            .Chain(Tween.Scale(_one, 0.0f, 0.25f, Ease.InOutCubic))
            .Group(Tween.Scale(_title, 0.0f, 0.25f, Ease.InOutCubic))
            .Group(Tween.UIAnchoredPositionY(_lapTimer, _lapTimerStartingY, 0.25f, Ease.InOutCubic))
            .Group(Tween.UIAnchoredPositionX(_leaderboard, _leaderboardStartingX, 0.25f, Ease.InOutCubic))
            .OnComplete(() => OnCountdownEnd.Invoke());
    }
}