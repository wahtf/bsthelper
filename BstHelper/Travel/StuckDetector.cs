using System;
using System.Numerics;

namespace BstHelper.Travel;

public class StuckDetector
{
    private const float ProgressTolerance = 2.5f;

    private DateTime lastProgressAt = DateTime.MinValue;
    private Vector3 lastPosition;

    public void Reset(Vector3 position)
    {
        lastPosition = position;
        lastProgressAt = DateTime.Now;
    }

    public bool IsStuck(Vector3 position, TimeSpan threshold)
    {
        if (lastProgressAt == DateTime.MinValue)
        {
            Reset(position);
            return false;
        }

        if (Vector3.Distance(position, lastPosition) > ProgressTolerance)
        {
            Reset(position);
            return false;
        }

        return DateTime.Now - lastProgressAt > threshold;
    }
}
