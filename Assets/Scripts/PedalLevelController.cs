using UnityEngine;

/// <summary>
/// Orchestrates the two-pedal removal sequence for the ChangePeдals level.
///
/// SEQUENCE
///   1. LEFT pedal: the player loosens it with the small wrench
///      (<see cref="leftPedalLoosening"/> starts ENABLED), then removes it to the carpet.
///   2. RIGHT pedal: automatically enabled after the left pedal is placed
///      (<see cref="rightPedalLoosening"/> starts DISABLED).
///   3. Once both pedals are on the carpet the <see cref="levelCompletedCanvas"/> is shown.
///
/// SCENE SETUP
///   1. Create an empty GameObject named "PedalLevelController" in the scene.
///   2. Attach this script to it.
///   3. Assign <see cref="leftPedalLoosening"/> (ENABLED at start) and
///      <see cref="rightPedalLoosening"/> (DISABLED at start).
///   4. Each <see cref="PedalGrab"/> must reference this controller and set
///      <see cref="PedalGrab.isLeftPedal"/> correctly.
///   5. Assign a World-Space Canvas for <see cref="levelCompletedCanvas"/>.
/// </summary>
public class PedalLevelController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────
    [Header("Pedal Sequence")]
    [Tooltip("PedalLooseningInteraction for the LEFT pedal — must be ENABLED at scene start.")]
    public PedalLooseningInteraction leftPedalLoosening;

    [Tooltip("PedalLooseningInteraction for the RIGHT pedal — must be DISABLED at scene start.")]
    public PedalLooseningInteraction rightPedalLoosening;

    [Header("UI")]
    [Tooltip("Canvas shown when both pedals have been placed on the carpet.")]
    public GameObject levelCompletedCanvas;

    // ── State ─────────────────────────────────────────────────────────────────
    private bool _leftDone  = false;
    private bool _rightDone = false;

    // ── Unity ─────────────────────────────────────────────────────────────────
    private void Start()
    {
        // Guarantee the right pedal loosening starts disabled; PedalLooseningInteraction.Start()
        // will also disable its PedalGrab, so both sides of the right step start locked.
        if (rightPedalLoosening != null)
            rightPedalLoosening.enabled = false;

        if (levelCompletedCanvas != null)
            levelCompletedCanvas.SetActive(false);

        // Canvas sequencing: only the left pedal canvas is visible at the start.
        SetCanvas(leftPedalLoosening,  true);
        SetCanvas(rightPedalLoosening, false);
    }

    // ── Public API ────────────────────────────────────────────────────────────
    /// <summary>
    /// Called by <see cref="PedalGrab.FinalizeOnCarpet"/> when a pedal is placed on the carpet.
    /// </summary>
    /// <param name="isLeftPedal">True when the left pedal was just placed, false for the right.</param>
    public void OnPedalPlaced(bool isLeftPedal)
    {
        if (isLeftPedal)
        {
            _leftDone = true;
            Debug.Log("[PedalLevelController] Left pedal placed on carpet. Enabling right pedal loosening.");

            // Hide left canvas, unlock and show right pedal canvas.
            SetCanvas(leftPedalLoosening, false);
            if (rightPedalLoosening != null){
                Debug.Log("[PedalLevelController] Enabling right pedal loosening interaction.");
                rightPedalLoosening.enabled = true;
            } else {
                Debug.LogWarning("[PedalLevelController] rightPedalLoosening is not assigned!");
            }
                
            SetCanvas(rightPedalLoosening, true);
        }
        else
        {
            _rightDone = true;
            Debug.Log("[PedalLevelController] Right pedal placed on carpet.");

            // Hide right canvas before showing level-complete.
            SetCanvas(rightPedalLoosening, false);
        }

        if (_leftDone && _rightDone)
            CompleteLevel();
    }

    // ── Internal ──────────────────────────────────────────────────────────────
    private void CompleteLevel()
    {
        Debug.Log("[PedalLevelController] Both pedals placed — level complete!");

        if (levelCompletedCanvas != null)
            levelCompletedCanvas.SetActive(true);
        else
            Debug.LogWarning("[PedalLevelController] levelCompletedCanvas is not assigned!");
    }

    private static void SetCanvas(PedalLooseningInteraction interaction, bool active)
    {
        if (interaction != null && interaction.counterCanvasObject != null)
            interaction.counterCanvasObject.SetActive(active);
    }
}
