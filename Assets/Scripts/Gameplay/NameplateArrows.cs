// @role Fills Room 3's nameplate arrows from the random breaker target pattern.
// @calls GameState

using TMPro;
using UnityEngine;

/// Fills Room 3's nameplate arrows from GameState.Room3ArrowUp at Awake() — the real values only
/// exist once a game is actually in progress, so the editor generator lays out empty labels and
/// this fills them in at runtime (same reasoning as CardAlignmentUI's matrix digits in Room 2).
public class NameplateArrows : MonoBehaviour
{
    public TMP_Text[] arrows = System.Array.Empty<TMP_Text>();

    void Awake()
    {
        GameState.EnsureStarted();
        for (int i = 0; i < arrows.Length && i < GameState.Room3ArrowUp.Length; i++)
            if (arrows[i] != null)
                arrows[i].text = GameState.Room3ArrowUp[i] ? "▲" : "▼"; // up / down triangle
    }
}
