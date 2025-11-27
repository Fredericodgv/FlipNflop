using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simulates various flip-flop types (JK, SR, D, T) with synchronous and asynchronous inputs.
/// Provides timeline computation and event generation for level path verification.
/// </summary>
public static class FlipFlopSimulator
{
    #region JK Flip-Flop Simulation

    /// <summary>
    /// Computes per-tile timeline for JK flip-flop with async preset/clear.
    /// JK evaluated at clock edges; async inputs take effect immediately and suppress next edge.
    /// </summary>
    /// <param name="asyncActiveHigh">If true, preset/clear active when signal=1; if false, active when signal=0</param>
    public static bool[] ComputeJKTimeline(bool[] jSignal, bool[] kSignal, bool[] presetSignal, bool[] clearSignal,
        int clockStep, int diagramLength, bool asyncActiveHigh = true)
    {
        if (diagramLength <= 0) return null;
        int totalLength = diagramLength + 1;

        var timeline = new bool[totalLength];
        bool q = false;
        bool asyncSincePrevEdge = false;

        for (int i = 0; i < totalLength; i++)
        {
            bool presetValue = GetAt(presetSignal, i);
            bool clearValue = GetAt(clearSignal, i);

            // If asyncActiveHigh=true: active when signal=1
            // If asyncActiveHigh=false: active when signal=0 (inverted)
            bool hasPreset = asyncActiveHigh ? presetValue : !presetValue;
            bool hasClear = asyncActiveHigh ? clearValue : !clearValue;

            bool isEdge = (clockStep > 0 && i > 0 && (i % clockStep) == 0);
            bool hasAsyncCurrent = (hasPreset || hasClear);

            // 1) Apply JK at clock edge (sampling J/K at i-1)
            if (isEdge)
            {
                bool suppressEdge = asyncSincePrevEdge && !hasAsyncCurrent;
                if (!suppressEdge)
                {
                    bool j = GetAt(jSignal, i - 1);
                    bool k = GetAt(kSignal, i - 1);
                    if (j && !k) q = true;
                    else if (!j && k) q = false;
                    else if (j && k) q = !q;
                }
                asyncSincePrevEdge = false;
            }

            // 2) Apply asynchronous preset/clear immediately
            if (hasPreset && hasClear)
            {
                q = false; // Clear priority
                asyncSincePrevEdge = true;
            }
            else if (hasClear)
            {
                q = false;
                asyncSincePrevEdge = true;
            }
            else if (hasPreset)
            {
                q = true;
                asyncSincePrevEdge = true;
            }

            timeline[i] = q;
        }
        return timeline;
    }

    /// <summary>
    /// Computes JK timeline with operation labels per tile (keep, set_sync, reset_sync, preset_async, etc).
    /// </summary>
    /// <param name="asyncActiveHigh">If true, preset/clear active when signal=1; if false, active when signal=0</param>
    public static bool[] ComputeJKTimelineWithOps(bool[] jSignal, bool[] kSignal, bool[] presetSignal, bool[] clearSignal,
        int clockStep, int diagramLength, out string[] ops, bool asyncActiveHigh = true)
    {
        ops = null;
        if (diagramLength <= 0) return null;
        int totalLength = diagramLength + 1;

        var timeline = new bool[totalLength];
        var opArr = new string[totalLength];
        bool q = false;
        bool asyncSincePrevEdge = false;

        for (int i = 0; i < totalLength; i++)
        {
            bool prevQ = q;
            bool presetValue = GetAt(presetSignal, i);
            bool clearValue = GetAt(clearSignal, i);

            // If asyncActiveHigh=true: active when signal=1
            // If asyncActiveHigh=false: active when signal=0 (inverted)
            bool hasPreset = asyncActiveHigh ? presetValue : !presetValue;
            bool hasClear = asyncActiveHigh ? clearValue : !clearValue;

            bool isEdge = (clockStep > 0 && i > 0 && (i % clockStep) == 0);
            bool hasAsyncCurrent = (hasPreset || hasClear);

            bool syncApplied = false;
            string syncToken = null;

            // 1) Apply JK at edge
            if (isEdge)
            {
                bool suppressEdge = asyncSincePrevEdge && !hasAsyncCurrent;
                if (!suppressEdge)
                {
                    bool j = GetAt(jSignal, i - 1);
                    bool k = GetAt(kSignal, i - 1);
                    bool beforeSyncQ = q;
                    if (j && !k) { q = true; syncApplied = true; syncToken = beforeSyncQ != q ? "set_sync" : "hold_sync"; }
                    else if (!j && k) { q = false; syncApplied = true; syncToken = beforeSyncQ != q ? "reset_sync" : "hold_sync"; }
                    else if (j && k) { q = !q; syncApplied = true; syncToken = "switch_sync"; }
                    else { syncApplied = true; syncToken = "hold_sync"; }
                }
                else
                {
                    syncApplied = false;
                    syncToken = "sync_ignored";
                }
                asyncSincePrevEdge = false;
            }

            // 2) Apply async
            string asyncToken = null;
            if (hasPreset && hasClear)
            {
                bool changed = q != false;
                q = false;
                asyncToken = changed ? "clear_async" : "clear_async_noop";
                asyncSincePrevEdge = true;
            }
            else if (hasClear)
            {
                bool changed = q != false;
                q = false;
                asyncToken = changed ? "clear_async" : "clear_async_noop";
                asyncSincePrevEdge = true;
            }
            else if (hasPreset)
            {
                bool changed = q != true;
                q = true;
                asyncToken = changed ? "preset_async" : "preset_async_noop";
                asyncSincePrevEdge = true;
            }

            timeline[i] = q;

            // 3) Decide final operation label
            string finalToken;
            bool asyncIsNoop = (asyncToken != null && asyncToken.EndsWith("_noop"));
            if (syncApplied && syncToken != null && syncToken != "hold_sync")
            {
                finalToken = (!asyncIsNoop && asyncToken != null) ? (syncToken + "_then_" + asyncToken) : syncToken;
            }
            else if (asyncToken != null)
            {
                finalToken = asyncToken;
            }
            else
            {
                finalToken = (prevQ == q) ? "keep" : (q ? "set_initial" : "reset_initial");
            }
            opArr[i] = finalToken;
        }
        ops = opArr;
        return timeline;
    }

    /// <summary>
    /// Generates signal events from JK simulation: sync at X=i, async at X=i+0.5.
    /// Used for PathVerifier reference path generation.
    /// </summary>
    /// <param name="asyncActiveHigh">If true, preset/clear active when signal=1; if false, active when signal=0</param>
    public static List<SignalEvent> ComputeJKEvents(bool[] jSignal, bool[] kSignal, bool[] presetSignal, bool[] clearSignal,
        int clockStep, int diagramLength, bool asyncActiveHigh = true)
    {
        if (diagramLength <= 0) return null;
        int totalLength = diagramLength + 1;

        var events = new List<SignalEvent>();
        bool q = false;
        bool prev = q;
        bool asyncSincePrevEdge = false;

        for (int i = 0; i < totalLength; i++)
        {
            bool isEdge = (clockStep > 0 && i > 0 && (i % clockStep) == 0);
            bool presetValue = GetAt(presetSignal, i);
            bool clearValue = GetAt(clearSignal, i);

            // If asyncActiveHigh=true: active when signal=1
            // If asyncActiveHigh=false: active when signal=0 (inverted)
            bool hasPreset = asyncActiveHigh ? presetValue : !presetValue;
            bool hasClear = asyncActiveHigh ? clearValue : !clearValue;

            bool hasAsyncCurrent = (hasPreset || hasClear);

            // 1) Synchronous edge effect at integer X=i
            if (isEdge)
            {
                bool suppressEdge = asyncSincePrevEdge && !hasAsyncCurrent;
                if (!suppressEdge)
                {
                    bool j = GetAt(jSignal, i - 1);
                    bool k = GetAt(kSignal, i - 1);
                    bool qSync = q;
                    if (j && !k) qSync = true;
                    else if (!j && k) qSync = false;
                    else if (j && k) qSync = !qSync;

                    if (qSync != prev)
                    {
                        events.Add(new SignalEvent(i, qSync));
                        prev = qSync;
                    }
                    q = qSync;
                }
                asyncSincePrevEdge = false;
            }

            // 2) Asynchronous immediate effect at half tile X=i+0.5
            if (hasPreset || hasClear)
            {
                bool qBefore = q;
                q = hasClear ? false : true; // Clear priority
                bool asyncChanged = (q != qBefore);
                if (asyncChanged)
                {
                    float xPos = i + 0.5f;
                    events.Add(new SignalEvent(xPos, q));
                    prev = q;
                }
                asyncSincePrevEdge = asyncChanged;
            }
        }
        return events;
    }

    #endregion

    #region SR Flip-Flop Simulation (Future Extension)

    /// <summary>
    /// Computes per-tile timeline for SR flip-flop.
    /// S=1 sets Q, R=1 resets Q, S=R=1 is typically invalid (can be configured).
    /// </summary>
    public static bool[] ComputeSRTimeline(bool[] sSignal, bool[] rSignal, int clockStep, int diagramLength,
        bool invalidStateToZero = true)
    {
        if (diagramLength <= 0) return null;
        int totalLength = diagramLength + 1;

        var timeline = new bool[totalLength];
        bool q = false;

        for (int i = 0; i < totalLength; i++)
        {
            bool isEdge = (clockStep > 0 && i > 0 && (i % clockStep) == 0);

            if (isEdge)
            {
                bool s = GetAt(sSignal, i - 1);
                bool r = GetAt(rSignal, i - 1);

                if (s && r)
                {
                    // Invalid state: can set to 0, 1, or keep (configurable)
                    q = invalidStateToZero ? false : q;
                }
                else if (s) q = true;
                else if (r) q = false;
                // else: no change (hold)
            }

            timeline[i] = q;
        }
        return timeline;
    }

    #endregion

    #region D Flip-Flop Simulation (Future Extension)

    /// <summary>
    /// Computes per-tile timeline for D flip-flop.
    /// Q follows D input at each clock edge.
    /// </summary>
    public static bool[] ComputeDTimeline(bool[] dSignal, int clockStep, int diagramLength)
    {
        if (diagramLength <= 0) return null;
        int totalLength = diagramLength + 1;

        var timeline = new bool[totalLength];
        bool q = false;

        for (int i = 0; i < totalLength; i++)
        {
            bool isEdge = (clockStep > 0 && i > 0 && (i % clockStep) == 0);

            if (isEdge)
            {
                q = GetAt(dSignal, i - 1);
            }

            timeline[i] = q;
        }
        return timeline;
    }

    #endregion

    #region T Flip-Flop Simulation (Future Extension)

    /// <summary>
    /// Computes per-tile timeline for T flip-flop.
    /// Q toggles when T=1 at clock edge, holds when T=0.
    /// </summary>
    public static bool[] ComputeTTimeline(bool[] tSignal, int clockStep, int diagramLength)
    {
        if (diagramLength <= 0) return null;
        int totalLength = diagramLength + 1;

        var timeline = new bool[totalLength];
        bool q = false;

        for (int i = 0; i < totalLength; i++)
        {
            bool isEdge = (clockStep > 0 && i > 0 && (i % clockStep) == 0);

            if (isEdge)
            {
                bool t = GetAt(tSignal, i - 1);
                if (t) q = !q;
            }

            timeline[i] = q;
        }
        return timeline;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Safely gets value from bool array at index (returns false if out of bounds or null).
    /// </summary>
    private static bool GetAt(bool[] arr, int idx)
    {
        return arr != null && idx >= 0 && idx < arr.Length && arr[idx];
    }

    /// <summary>
    /// Returns the maximum length among provided bool arrays.
    /// </summary>
    public static int MaxLen(params bool[][] arrays)
    {
        int max = 0;
        if (arrays == null) return 0;
        for (int i = 0; i < arrays.Length; i++)
        {
            if (arrays[i] != null && arrays[i].Length > max) max = arrays[i].Length;
        }
        return max;
    }

    /// <summary>
    /// Inverts all bits in a bool array (active-high to active-low conversion).
    /// </summary>
    public static bool[] InvertBits(bool[] arr)
    {
        if (arr == null) return null;
        var res = new bool[arr.Length];
        for (int i = 0; i < arr.Length; i++) res[i] = !arr[i];
        return res;
    }

    #endregion

    #region Data Structures

    /// <summary>
    /// Represents a signal transition event at a specific X position.
    /// </summary>
    public struct SignalEvent
    {
        public float x;
        public bool value;

        public SignalEvent(float x, bool value)
        {
            this.x = x;
            this.value = value;
        }
    }

    #endregion
}
