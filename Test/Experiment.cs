using System;
using System.Collections;
using System.IO;
using System.Text.Json;
using UnityEngine;

namespace BHR.Test;

public interface IExperiment
{
    string Name { get; }

    IEnumerator Run();
    ExperimentResult Result { get; set; }
}

public class ScheduledExperiment(string name, List<ScheduledAction> actions) : IExperiment
{
    public string Name { get; } = name;

    private readonly List<ScheduledAction> _actions = actions;
    private long _startTime;

    public IEnumerator Run()
    {
        ExperimentManager.Record("ExperimentStart", Name);
        _startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var action in _actions)
        {
            var delay = action.Time - (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - _startTime);
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay / 1000f);
            }
            action.Action();
        }
    }

    public ExperimentResult Result { get; set; } = ExperimentResult.None;
}

public interface IExperimentStep
{
    IEnumerator Run();
}

public record ScheduledAction(
    float Time,
    Action Action
);

public enum ExperimentResult
{
    None,
    Running,
    Passed,
    Kicked,
    Disconnected,
    TimedOut
}

public record TestEvent(
    DateTime Time,
    string Category,
    string Message
);

public static class ExperimentManager
{
    public static readonly Dictionary<string, IExperiment> Experiments = [
        
    ];

    public static readonly Dictionary<string, List<int>> RecordFrequencies = [];

    public static readonly Dictionary<string, int> LastSecondRecords = [];

    public static readonly List<TestEvent> Events = [];

    private static float _timeSinceLastFrequencyUpdate;

    public static void OnFixedUpdate(float deltaTime)
    {
        _timeSinceLastFrequencyUpdate += deltaTime;
        if (_timeSinceLastFrequencyUpdate >= 1f)
        {
            foreach (var category in LastSecondRecords.Keys)
            {
                if (!RecordFrequencies.ContainsKey(category))
                    RecordFrequencies[category] = [];
                RecordFrequencies[category].Add(LastSecondRecords.TryGetValue(category, out var count) ? count : 0);
            }
            LastSecondRecords.Clear();
            _timeSinceLastFrequencyUpdate = 0f;
        }
    }

    public static void RunExperiment(string name)
    {
        Experiments.TryGetValue(name, out var experiment);
        if (experiment == null)
        {
            Debug.LogError($"Experiment {name} not found.");
            return;
        }

        if (experiment.Result != ExperimentResult.None)
        {
            Debug.LogError($"Experiment {name} is already running or has completed.");
            return;
        }

        experiment.Result = ExperimentResult.Running;
        Record("ExperimentScheduled", name);
        Main.Instance.StartCoroutine(experiment.Run());
    }

    public static void Record(
        string category,
        string message,
        bool log = true)
    {

        if (log)
            Events.Add(
                new(
                    DateTime.UtcNow,
                    category,
                    message
                )
            );
        
        LastSecondRecords[category] = LastSecondRecords.TryGetValue(category, out var count) ? count + 1 : 1;
    }

    public static void OnKicked()
    {
        foreach (var experiment in Experiments.Values)
        {
            if (experiment.Result == ExperimentResult.Running)
            {
                experiment.Result = ExperimentResult.Kicked;
            }
        }

        Record("ExperimentResult", "Kicked");
        PrintResults();
    }

    public static void OnDisconnected()
    {
        foreach (var experiment in Experiments.Values)
        {
            if (experiment.Result == ExperimentResult.Running)
            {
                experiment.Result = ExperimentResult.Disconnected;
            }
        }

        Record("ExperimentResult", "Disconnected");
        PrintResults();
    }

    public static void OnTimedOut()
    {
        foreach (var experiment in Experiments.Values)
        {
            if (experiment.Result == ExperimentResult.Running)
            {
                experiment.Result = ExperimentResult.TimedOut;
            }
        }

        Record("ExperimentResult", "TimedOut");
        PrintResults();
    }

    private static void PrintResults()
    {
        var results = new Dictionary<string, ExperimentResult>();
        foreach (var experiment in Experiments.Values)
            results[experiment.Name] = experiment.Result;

        File.WriteAllText(
            "last_run.json",
            "[" +
            JsonSerializer.Serialize(
                Events.TakeLast(200)
            ) + Environment.NewLine + JsonSerializer.Serialize(results)
            + Environment.NewLine + 
            JsonSerializer.Serialize(RecordFrequencies)
            + "]"
        );
    }
}