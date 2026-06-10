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
    /// Computes JK flip-flop timeline with asynchronous preset/clear support.
    /// Returns doubled-resolution output: each input tile produces 2 output positions (X.0 and X.5).
    /// This allows both synchronous (at X.0) and asynchronous (at X.5) operations to coexist.
    /// Timeline length = (diagramLength+1) * 2, where even indices are sync, odd indices are async.
    /// </summary>
    /// <param name="asyncActiveHigh">If true, preset/clear active when signal=1; if false, active when signal=0</param>
    public static bool[] SimulateJK(bool[] jSignal, bool[] kSignal, bool[] presetSignal, bool[] clearSignal,
        int clockStep, int diagramLength, out string[] ops, out List<SignalEvent> events, bool asyncActiveHigh = true)
    {
        ops = null;
        events = null;
        if (diagramLength <= 0) return null;

        int inputLength = diagramLength + 1;
        int outputLength = inputLength * 2;
        var timeline = new bool[outputLength];
        var opArr = new string[outputLength];
        var eventList = new List<SignalEvent>();

        bool q = false;
        bool asyncAtPreviousTile = false;

        for (int i = 0; i < inputLength; i++)
        {
            int syncIndex = i * 2;
            int asyncIndex = i * 2 + 1;

            bool isEdge = (clockStep > 0 && i > 0 && (i % clockStep) == 0);

            bool qBeforeSync = q;
            bool syncApplied = false;
            string syncToken = null;

            if (isEdge)
            {
                // Only ignore sync if async happened at the IMMEDIATELY PREVIOUS tile (i-1)
                if (!asyncAtPreviousTile)
                {
                    bool j = GetAt(jSignal, i - 1);
                    bool k = GetAt(kSignal, i - 1);

                    // JK truth table
                    if (j && !k) q = true;
                    else if (!j && k) q = false;
                    else if (j && k) q = !q;

                    syncApplied = true;
                    if (j && k) syncToken = "switch_sync";
                    else if (qBeforeSync != q) syncToken = j ? "set_sync" : "reset_sync";
                    else syncToken = "hold_sync";

                    if (q != qBeforeSync)
                    {
                        eventList.Add(new SignalEvent(i, q));
                    }
                }
                else
                {
                    syncApplied = false;
                    syncToken = "sync_ignored";
                }
            }

            timeline[syncIndex] = q;
            opArr[syncIndex] = syncApplied && syncToken != null && syncToken != "hold_sync"
                ? syncToken
                : (syncToken == "sync_ignored" ? syncToken : (qBeforeSync == q ? "keep" : (q ? "set_initial" : "reset_initial")));

            bool qBeforeAsync = q;
            bool presetValue = GetAt(presetSignal, i);
            bool clearValue = GetAt(clearSignal, i);

            bool hasPreset = presetSignal != null && i < presetSignal.Length && (asyncActiveHigh ? presetValue : !presetValue);
            bool hasClear = clearSignal != null && i < clearSignal.Length && (asyncActiveHigh ? clearValue : !clearValue);

            string asyncToken;

            if (hasPreset && hasClear)
            {
                throw new InvalidOperationException($"Fase com entradas inválidas: Preset e Clear assíncronos não podem estar ativos simultaneamente (Erro no Tile {i}).");
            }

            if (hasPreset || hasClear)
            {
                q = hasClear ? false : true;

                asyncToken = (q != qBeforeAsync)
                    ? (hasClear ? "clear_async" : "preset_async")
                    : (hasClear ? "clear_async_noop" : "preset_async_noop");

                if (q != qBeforeAsync)
                {
                    eventList.Add(new SignalEvent(i + 0.5f, q));
                }

                // Mark that async happened at THIS tile
                asyncAtPreviousTile = true;
            }
            else
            {
                asyncToken = (qBeforeAsync == q) ? "keep" : (q ? "set_initial" : "reset_initial");
                // Reset flag since no async at this tile
                asyncAtPreviousTile = false;
            }

            timeline[asyncIndex] = q;
            opArr[asyncIndex] = asyncToken;
        }

        ops = opArr;
        events = eventList;
        return timeline;
    }

    #endregion

    #region SR Flip-Flop Simulation

    /// <summary>
    /// Computes SR flip-flop timeline with doubled-resolution output.
    /// Returns doubled-resolution output: each input tile produces 2 output positions (X.0 and X.5).
    /// Timeline length = (diagramLength+1) * 2, where even indices are sync, odd indices are async.
    /// S=1 sets Q, R=1 resets Q, S=R=1 is typically invalid (can be configured).
    /// </summary>
    public static bool[] SimulateSR(bool[] sSignal, bool[] rSignal, int clockStep, int diagramLength,
        out string[] ops, out List<SignalEvent> events, bool invalidStateToZero = true)
    {
        ops = null;
        events = null;
        if (diagramLength <= 0) return null;

        int inputLength = diagramLength + 1;
        int outputLength = inputLength * 2;
        var timeline = new bool[outputLength];
        var opArr = new string[outputLength];
        var eventList = new List<SignalEvent>();

        bool q = false;

        for (int i = 0; i < inputLength; i++)
        {
            int syncIndex = i * 2;
            int asyncIndex = i * 2 + 1;

            bool isEdge = (clockStep > 0 && i > 0 && (i % clockStep) == 0);

            bool qBeforeSync = q;
            string syncToken = null;

            if (isEdge)
            {
                bool s = GetAt(sSignal, i - 1);
                bool r = GetAt(rSignal, i - 1);

                if (s && r)
                {
                    throw new InvalidOperationException($"Fase com entradas inválidas: Set (S) e Reset (R) não podem estar ativos simultaneamente (Erro no Tile {i - 1}).");
                }
                else if (s)
                {
                    q = true;
                    syncToken = qBeforeSync != q ? "set_sync" : "hold_sync";
                }
                else if (r)
                {
                    q = false;
                    syncToken = qBeforeSync != q ? "reset_sync" : "hold_sync";
                }
                else
                {
                    syncToken = "hold_sync";
                }

                if (q != qBeforeSync)
                {
                    eventList.Add(new SignalEvent(i, q));
                }
            }

            timeline[syncIndex] = q;
            opArr[syncIndex] = syncToken ?? (qBeforeSync == q ? "keep" : (q ? "set_initial" : "reset_initial"));

            timeline[asyncIndex] = q;
            opArr[asyncIndex] = "keep";
        }

        ops = opArr;
        events = eventList;
        return timeline;
    }

    #endregion

    #region D Flip-Flop Simulation

    /// <summary>
    /// Computes D flip-flop timeline with doubled-resolution output.
    /// Returns doubled-resolution output: each input tile produces 2 output positions (X.0 and X.5).
    /// Timeline length = (diagramLength+1) * 2, where even indices are sync, odd indices are async.
    /// Q follows D input at each clock edge.
    /// </summary>
    public static bool[] SimulateD(bool[] dSignal, bool[] presetSignal, bool[] clearSignal,
        int clockStep, int diagramLength, out string[] ops, out List<SignalEvent> events, bool asyncActiveHigh = true)
    {
        ops = null;
        events = null;
        if (diagramLength <= 0) return null;

        int inputLength = diagramLength + 1;
        int outputLength = inputLength * 2;
        var timeline = new bool[outputLength];
        var opArr = new string[outputLength];
        var eventList = new List<SignalEvent>();

        bool q = false;
        bool asyncSincePrevEdge = false;

        for (int i = 0; i < inputLength; i++)
        {
            int syncIndex = i * 2;
            int asyncIndex = i * 2 + 1;

            bool isEdge = (clockStep > 0 && i > 0 && (i % clockStep) == 0);

            bool qBeforeSync = q;
            bool syncApplied = false;
            string syncToken = null;

            if (isEdge)
            {
                if (!asyncSincePrevEdge)
                {
                    bool d = GetAt(dSignal, i - 1);
                    q = d;

                    syncApplied = true;
                    syncToken = qBeforeSync != q ? (d ? "set_sync" : "reset_sync") : "hold_sync";

                    if (q != qBeforeSync)
                    {
                        eventList.Add(new SignalEvent(i, q));
                    }
                }
                else
                {
                    syncApplied = false;
                    syncToken = "sync_ignored";
                }

                asyncSincePrevEdge = false;
            }

            timeline[syncIndex] = q;
            opArr[syncIndex] = syncApplied && syncToken != null && syncToken != "hold_sync"
                ? syncToken
                : (syncToken == "sync_ignored" ? syncToken : (qBeforeSync == q ? "keep" : (q ? "set_initial" : "reset_initial")));

            bool qBeforeAsync = q;
            bool presetValue = GetAt(presetSignal, i);
            bool clearValue = GetAt(clearSignal, i);

            bool hasPreset = presetSignal != null && i < presetSignal.Length && (asyncActiveHigh ? presetValue : !presetValue);
            bool hasClear = clearSignal != null && i < clearSignal.Length && (asyncActiveHigh ? clearValue : !clearValue);

            string asyncToken;

            if (hasPreset || hasClear)
            {
                q = hasClear ? false : true;

                asyncToken = (q != qBeforeAsync)
                    ? (hasClear ? "clear_async" : "preset_async")
                    : (hasClear ? "clear_async_noop" : "preset_async_noop");

                if (q != qBeforeAsync)
                {
                    eventList.Add(new SignalEvent(i + 0.5f, q));
                }

                asyncSincePrevEdge = true;
            }
            else
            {
                asyncToken = (qBeforeAsync == q) ? "keep" : (q ? "set_initial" : "reset_initial");
            }

            timeline[asyncIndex] = q;
            opArr[asyncIndex] = asyncToken;
        }

        ops = opArr;
        events = eventList;
        return timeline;
    }

    #endregion

    #region T Flip-Flop Simulation

    /// <summary>
    /// Computes T flip-flop timeline with doubled-resolution output.
    /// Returns doubled-resolution output: each input tile produces 2 output positions (X.0 and X.5).
    /// Timeline length = (diagramLength+1) * 2, where even indices are sync, odd indices are async.
    /// Q toggles when T=1 at clock edge, holds when T=0.
    /// </summary>
    public static bool[] SimulateT(bool[] tSignal, bool[] presetSignal, bool[] clearSignal,
        int clockStep, int diagramLength, out string[] ops, out List<SignalEvent> events, bool asyncActiveHigh = true)
    {
        ops = null;
        events = null;
        if (diagramLength <= 0) return null;

        int inputLength = diagramLength + 1;
        int outputLength = inputLength * 2;
        var timeline = new bool[outputLength];
        var opArr = new string[outputLength];
        var eventList = new List<SignalEvent>();

        bool q = false;
        bool asyncSincePrevEdge = false;

        for (int i = 0; i < inputLength; i++)
        {
            int syncIndex = i * 2;
            int asyncIndex = i * 2 + 1;

            bool isEdge = (clockStep > 0 && i > 0 && (i % clockStep) == 0);

            bool qBeforeSync = q;
            bool syncApplied = false;
            string syncToken = null;

            if (isEdge)
            {
                if (!asyncSincePrevEdge)
                {
                    bool t = GetAt(tSignal, i - 1);
                    if (t) q = !q;

                    syncApplied = true;
                    syncToken = t ? (qBeforeSync != q ? "toggle_sync" : "hold_sync") : "hold_sync";

                    if (q != qBeforeSync)
                    {
                        eventList.Add(new SignalEvent(i, q));
                    }
                }
                else
                {
                    syncApplied = false;
                    syncToken = "sync_ignored";
                }

                asyncSincePrevEdge = false;
            }

            timeline[syncIndex] = q;
            opArr[syncIndex] = syncApplied && syncToken != null && syncToken != "hold_sync"
                ? syncToken
                : (syncToken == "sync_ignored" ? syncToken : (qBeforeSync == q ? "keep" : (q ? "set_initial" : "reset_initial")));

            bool qBeforeAsync = q;
            bool presetValue = GetAt(presetSignal, i);
            bool clearValue = GetAt(clearSignal, i);

            bool hasPreset = presetSignal != null && i < presetSignal.Length && (asyncActiveHigh ? presetValue : !presetValue);
            bool hasClear = clearSignal != null && i < clearSignal.Length && (asyncActiveHigh ? clearValue : !clearValue);

            string asyncToken;

            if (hasPreset || hasClear)
            {
                q = hasClear ? false : true;

                asyncToken = (q != qBeforeAsync)
                    ? (hasClear ? "clear_async" : "preset_async")
                    : (hasClear ? "clear_async_noop" : "preset_async_noop");

                if (q != qBeforeAsync)
                {
                    eventList.Add(new SignalEvent(i + 0.5f, q));
                }

                asyncSincePrevEdge = true;
            }
            else
            {
                asyncToken = (qBeforeAsync == q) ? "keep" : (q ? "set_initial" : "reset_initial");
            }

            timeline[asyncIndex] = q;
            opArr[asyncIndex] = asyncToken;
        }

        ops = opArr;
        events = eventList;
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
