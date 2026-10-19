using System.Collections.Generic;
using UnityEngine;

/// Cross-scene game state: a static class survives scene loads within one play session (same
/// trick GameResult used to rely on — Time.time is not reset by a scene load). Replaces the old
/// Room-1-only GameResult now that the game spans four rooms plus retries.
///
/// All per-room secrets are generated exactly once, in BeginNewGame() (called from the Title
/// "Start" button and from the End screen's "Play Again"). A scene reload from "Retry Room" never
/// calls BeginNewGame(), so the same secrets persist — only EnsureStarted() runs, which is a
/// no-op once a game is already in progress.
public static class GameState
{
    public static bool Started { get; private set; }

    // ---- Per-room secrets, generated once per game ----
    public static int[] Room1Code { get; private set; } = new int[4]; // indexed by ClueSymbol
    public static int[] Room2Code { get; private set; } = new int[4]; // indexed by pinhole number 1-4 (0-based)
    public static bool[] Room3ArrowUp { get; private set; } = new bool[4]; // target breaker position per slot
    public static ClueSymbol[] Room4MirrorOrder { get; private set; } = { ClueSymbol.Triangle, ClueSymbol.Square, ClueSymbol.Circle, ClueSymbol.Bar };

    // ---- Progress ----
    public static readonly HashSet<int> SolvedRooms = new HashSet<int>();
    public static int CheckpointRoom { get; private set; } = 1;
    public static List<string> CheckpointInventory { get; private set; } = new List<string>();

    /// Digit seen so far for each symbol (Circle/Triangle/Square/Bar), -1 = not yet seen. Mirrors
    /// ClueTracker's on-screen state so a scene reload (Retry Room on a later room) can restore it.
    public static int[] Room1ClueRecord { get; private set; } = { -1, -1, -1, -1 };

    // ---- Outcome ----
    public static bool Won;
    public static int FailedRoomNumber;
    public static float GlobalStartTime;
    public static float ElapsedSeconds;

    public static void BeginNewGame()
    {
        Started = true;
        FullCampaign.ResetRun();
        StoryExperience.IntroPending = false;
        Won = false;
        FailedRoomNumber = 0;
        SolvedRooms.Clear();
        CheckpointRoom = 1;
        CheckpointInventory = new List<string>();
        Room1ClueRecord = new[] { -1, -1, -1, -1 };
        GlobalStartTime = Time.time;
        ElapsedSeconds = 0f;

        for (int i = 0; i < 4; i++) Room1Code[i] = Random.Range(0, 10);
        for (int i = 0; i < 4; i++) Room2Code[i] = Random.Range(0, 10);

        // Room 3: breaker targets, excluding "all down" (otherwise the puzzle starts solved).
        bool[] arrows;
        do
        {
            arrows = new bool[4];
            for (int i = 0; i < 4; i++) arrows[i] = Random.value > 0.5f;
        } while (System.Array.TrueForAll(arrows, up => !up));
        Room3ArrowUp = arrows;

        // Room 4: a permutation of the 4 symbols, excluding the identity order (that would let
        // the player read Room 4's password straight off Room 1's tracker without the mirror).
        ClueSymbol[] identity = { ClueSymbol.Circle, ClueSymbol.Triangle, ClueSymbol.Square, ClueSymbol.Bar };
        ClueSymbol[] order;
        do { order = Shuffle(identity); } while (SequenceEquals(order, identity));
        Room4MirrorOrder = order;
    }

    /// Called defensively by systems that need a valid game in progress (e.g. opening the level
    /// scene directly in the editor without going through Title). Does nothing once a game has
    /// already started, so it never clobbers an in-progress run or a retry's saved state.
    public static void EnsureStarted()
    {
        if (!Started) BeginNewGame();
    }

    public static void SetCheckpoint(int roomNumber, List<string> inventorySnapshot)
    {
        if (roomNumber < CheckpointRoom) return;
        CheckpointRoom = roomNumber;
        CheckpointInventory = new List<string>(inventorySnapshot);
    }

    public static void MarkRoomSolved(int roomNumber) => SolvedRooms.Add(roomNumber);
    public static bool IsRoomSolved(int roomNumber) => SolvedRooms.Contains(roomNumber);

    public static void RecordClueDigit(ClueSymbol symbol, int digit) => Room1ClueRecord[(int)symbol] = digit;

    public static void Fail(int roomNumber)
    {
        Won = false;
        FailedRoomNumber = roomNumber;
        FullCampaign.SaveFailure(roomNumber);
        ElapsedSeconds = Time.time - GlobalStartTime;
    }

    public static void WinGame()
    {
        Won = true;
        ElapsedSeconds = Time.time - GlobalStartTime;
    }

    static T[] Shuffle<T>(T[] source)
    {
        T[] arr = (T[])source.Clone();
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
        return arr;
    }

    static bool SequenceEquals(ClueSymbol[] a, ClueSymbol[] b)
    {
        for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
        return true;
    }
}
