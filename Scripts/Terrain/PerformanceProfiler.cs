using System.Diagnostics;
using UnityEngine;

public class PerformanceProfiler
{
    private Stopwatch stopwatch = new Stopwatch();
    private float lastExecutionTime = 0f;

    public void StartProfiling()
    {
        stopwatch.Reset();
        stopwatch.Start();
    }

    public void StopProfiling()
    {
        stopwatch.Stop();
        lastExecutionTime = stopwatch.ElapsedMilliseconds / 1000f; // Convert to seconds
    }

    public float GetLastExecutionTime()
    {
        return lastExecutionTime;
    }

    public void DisplayProfilerResults()
    {
        UnityEngine.Debug.Log($"⏳ Graph Execution Time: {lastExecutionTime:F3} seconds");
    }
}
