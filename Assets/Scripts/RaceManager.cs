using System.Collections.Generic;
using UnityEngine;

public enum RacePhase { Countdown, Racing, Finished }

/// <summary>Kartopia GP: 3-lap loop race, countdown, live positions, results.</summary>
public class RaceManager : MonoBehaviour
{
    public Transform player;
    public List<Transform> aiCars = new();
    public TrackPath track;
    public int totalLaps = 3;
    public float countdownSeconds = 3f;
    public AudioClip musicClip;

    public RacePhase Phase { get; private set; } = RacePhase.Countdown;
    public float RaceTime { get; private set; }
    public float CountdownLeft => Mathf.Max(0f, countdownSeconds - _countdownElapsed);
    public bool PlayerFinished { get; private set; }
    public float PlayerFinishTime { get; private set; }
    public float PlayerProgressMeters => _playerProgress;

    float _countdownElapsed;
    int _playerIndexHint = -1, _playerLap;
    float _playerProgress;
    readonly Dictionary<Transform, float> _finishTimes = new();

    AudioSource _music;

    void Start()
    {
        if (player != null && player.TryGetComponent<KartController>(out var kc)) kc.enabled = false;
        _music = gameObject.AddComponent<AudioSource>();
        _music.clip = musicClip; _music.loop = true; _music.volume = 0.45f;
    }

    void Update()
    {
        if (track != null && player != null)
            _playerProgress = track.ProgressOf(player.position, ref _playerIndexHint, ref _playerLap);

        if (Phase == RacePhase.Countdown)
        {
            _countdownElapsed += Time.deltaTime;
            if (_countdownElapsed >= countdownSeconds)
            {
                Phase = RacePhase.Racing;
                if (_music.clip != null) _music.Play();
                if (player.TryGetComponent<KartController>(out var kc)) kc.enabled = true;
                foreach (var ai in aiCars)
                    if (ai.TryGetComponent<AiDriver>(out var d)) d.racing = true;
            }
            return;
        }
        if (Phase != RacePhase.Racing) return;
        RaceTime += Time.deltaTime;

        float goal = totalLaps * (track != null ? track.totalLength : 400f);
        if (!PlayerFinished && _playerProgress >= goal)
        {
            PlayerFinished = true;
            PlayerFinishTime = RaceTime;
            _finishTimes[player] = RaceTime;
            Phase = RacePhase.Finished;
        }
        foreach (var ai in aiCars)
        {
            if (_finishTimes.ContainsKey(ai)) continue;
            if (ai.TryGetComponent<AiDriver>(out var d) && d.ProgressMeters >= goal)
                _finishTimes[ai] = RaceTime;
        }
    }

    public int PlayerPosition()
    {
        int pos = 1;
        foreach (var ai in aiCars)
            if (ai.TryGetComponent<AiDriver>(out var d) && d.ProgressMeters > _playerProgress) pos++;
        return pos;
    }

    public int RacerCount => 1 + aiCars.Count;
    public int PlayerLapDisplay => Mathf.Min(_playerLap + 1, totalLaps);

    public string ResultsText()
    {
        var rows = new List<(string name, float time, bool done)> { ("YOU", PlayerFinishTime, PlayerFinished) };
        var names = new[] { "RUBY", "MINT", "SUNNY" };
        for (int i = 0; i < aiCars.Count; i++)
        {
            bool done = _finishTimes.TryGetValue(aiCars[i], out var t);
            rows.Add((i < names.Length ? names[i] : $"AI{i + 1}", done ? t : RaceTime, done));
        }
        rows.Sort((a, b) => a.done != b.done ? (a.done ? -1 : 1) : a.time.CompareTo(b.time));
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < rows.Count; i++)
            sb.AppendLine($"{i + 1}. {rows[i].name}  {(rows[i].done ? FormatTime(rows[i].time) : "DNF")}");
        return sb.ToString();
    }

    public static string FormatTime(float t)
    {
        int m = Mathf.FloorToInt(t / 60f);
        return $"{m:00}:{(t - m * 60f):00.000}";
    }
}
