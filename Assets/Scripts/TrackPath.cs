using UnityEngine;

/// <summary>Storm Coast circuit sampled from the Godot game: center points + road width.</summary>
public class TrackPath : MonoBehaviour
{
    public Vector3[] points;
    public float roadWidth = 20f;
    public float totalLength;

    float[] _cum;

    public int Count => points?.Length ?? 0;

    public void Build(Vector3[] pts, float width)
    {
        points = pts;
        roadWidth = width;
        _cum = new float[points.Length + 1];
        for (int i = 0; i < points.Length; i++)
            _cum[i + 1] = _cum[i] + Vector3.Distance(points[i], points[(i + 1) % points.Length]);
        totalLength = _cum[points.Length];
    }

    public Vector3 PointAt(int index) => points[((index % Count) + Count) % Count];

    public float DistanceAt(int index) => _cum[((index % Count) + Count) % Count];

    /// <summary>Closest sample index; searches a window around the hint first, then globally.</summary>
    public int ClosestIndex(Vector3 pos, int hint = -1)
    {
        if (Count == 0) return 0;
        if (hint >= 0)
        {
            int best = -1; float bestD = float.MaxValue;
            for (int off = -6; off <= 24; off++)
            {
                int i = ((hint + off) % Count + Count) % Count;
                float d = (points[i] - pos).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            if (bestD < 30f * 30f) return best;
        }
        int bi = 0; float bd = float.MaxValue;
        for (int i = 0; i < Count; i++)
        {
            float d = (points[i] - pos).sqrMagnitude;
            if (d < bd) { bd = d; bi = i; }
        }
        return bi;
    }

    /// <summary>Continuous race progress in meters: laps * totalLength + distance along lap.</summary>
    public float ProgressOf(Vector3 pos, ref int indexHint, ref int lap)
    {
        int idx = ClosestIndex(pos, indexHint);
        if (indexHint >= 0)
        {
            if (indexHint > Count - 20 && idx < 20) lap++;
            else if (indexHint < 20 && idx > Count - 20 && lap > 0) lap--;
        }
        indexHint = idx;
        return lap * totalLength + DistanceAt(idx);
    }
}
