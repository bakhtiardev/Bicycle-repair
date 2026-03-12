// TaskManager.cs
// Manages the 3-step bike repair sequence:
//   Step 0 → Seat Assembly
//   Step 1 → Front Wheel Assembly
//   Step 2 → Pedal Assembly
//
// Integrates with existing MRBike scripts:
//   - NetworkedBikeObjectAssembly  (per-part grab/target logic)
//   - TransformTarget              (OnComplete UnityEvent per target)
//   - TaskVOPlayer                 (voice-over audio)
//   - BikeAudioTrigger             (SFX triggers)
//   - AffordanceFX                 (highlight/glow on active parts)
//   - NetworkedBikeTools           (wrench from ToolsAndPartsDisplay)

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace MRBike
{
    public class TaskManager : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────
        // Step definition — wired in the Inspector
        // ─────────────────────────────────────────────────────────────
        [Serializable]
        public class RepairStep
        {
            [Tooltip("Human-readable label shown in the UI.")]
            public string stepName = "Step";

            [Tooltip("Root assembly GameObject for this step (e.g. SeatAssembly).")]
            public GameObject assemblyRoot;

            [Tooltip("All grabbable part GameObjects that belong to this step " +
                     "(from ToolsAndPartsDisplay). They are enabled only while this step is active.")]
            public GameObject[] parts;

            [Tooltip("All tool GameObjects required for this step " +
                     "(e.g. WrenchHome child). Enabled only while this step is active.")]
            public GameObject[] tools;

            [Tooltip("TransformTarget components whose OnComplete fires when the part " +
                     "is placed correctly. TaskManager listens to these to detect completion.")]
            public TransformTarget[] completionTargets;

            [Tooltip("AffordanceFX components to pulse when this step becomes active.")]
            public AffordanceFX[] affordances;

            [Tooltip("BikeAudioTrigger to fire when this step starts (e.g. grab SFX).")]
            public BikeAudioTrigger stepStartSFX;

            [Tooltip("BikeAudioTrigger to fire when this step completes (e.g. objective-complete SFX).")]
            public BikeAudioTrigger stepCompleteSFX;

            [Tooltip("Index inside TaskVOPlayer.m_clips to play when this step starts.")]
            public int voStartClipIndex = -1;

            [Tooltip("Index inside TaskVOPlayer.m_clips to play when this step completes.")]
            public int voCompleteClipIndex = -1;

            // Runtime state
            [NonSerialized] public int targetsCompleted;
            [NonSerialized] public bool isDone;
        }

        // ─────────────────────────────────────────────────────────────
        // Inspector fields
        // ─────────────────────────────────────────────────────────────
        [Header("Steps (assigned in Inspector)")]
        [SerializeField] private RepairStep[] m_steps;

        [Header("References")]
        [SerializeField] private TaskVOPlayer m_voPlayer;
        [SerializeField] private BikeAudioTrigger m_objectiveCompleteSFX;  // BikeBuild_ObjectiveComplete
        [SerializeField] private BikeAudioTrigger m_allDoneSFX;            // optional "all done" SFX

        [Header("Settings")]
        [SerializeField] private float m_stepCompletionDelay = 1.2f;       // seconds before advancing
        [SerializeField] private bool m_autoStartOnEnable = true;

        // ─────────────────────────────────────────────────────────────
        // Events — subscribe from UI or other systems
        // ─────────────────────────────────────────────────────────────
        [Header("Events")]
        /// <summary>Fired when a new step becomes active. Arg: step index (0-based).</summary>
        public UnityEvent<int> OnStepStarted;

        /// <summary>Fired when a step is completed. Arg: step index.</summary>
        public UnityEvent<int> OnStepCompleted;

        /// <summary>Fired with the step name string whenever a step starts.</summary>
        public UnityEvent<string> OnStepNameChanged;

        /// <summary>Fired when all steps are done.</summary>
        public UnityEvent OnAllStepsCompleted;

        // ─────────────────────────────────────────────────────────────
        // Runtime state
        // ─────────────────────────────────────────────────────────────
        private int m_currentStepIndex = -1;
        private bool m_sequenceFinished = false;

        public int CurrentStepIndex => m_currentStepIndex;
        public bool SequenceFinished => m_sequenceFinished;
        public string CurrentStepName =>
            (m_currentStepIndex >= 0 && m_currentStepIndex < m_steps.Length)
            ? m_steps[m_currentStepIndex].stepName : string.Empty;

        // ─────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────
        private void OnEnable()
        {
            if (m_autoStartOnEnable)
                StartSequence();
        }

        private void OnDisable()
        {
            UnsubscribeAllTargets();
        }

        // ─────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────

        /// <summary>Begin the repair sequence from step 0.</summary>
        public void StartSequence()
        {
            m_sequenceFinished = false;
            DisableAllSteps();
            AdvanceToStep(0);
        }

        /// <summary>Skip directly to a given step index (e.g. for debugging).</summary>
        public void GoToStep(int index)
        {
            if (index < 0 || index >= m_steps.Length) return;
            DisableAllSteps();
            AdvanceToStep(index);
        }

        /// <summary>Called by external code (or Inspector button) to force-complete current step.</summary>
        public void CompleteCurrentStep()
        {
            if (m_currentStepIndex < 0 || m_sequenceFinished) return;
            StartCoroutine(FinishStep(m_currentStepIndex));
        }

        // ─────────────────────────────────────────────────────────────
        // Internal step flow
        // ─────────────────────────────────────────────────────────────

        private void AdvanceToStep(int index)
        {
            if (index >= m_steps.Length)
            {
                FinishSequence();
                return;
            }

            m_currentStepIndex = index;
            RepairStep step = m_steps[index];
            step.targetsCompleted = 0;
            step.isDone = false;

            // Enable assembly root
            if (step.assemblyRoot != null)
                step.assemblyRoot.SetActive(true);

            // Enable parts + tools for this step only
            SetPartsAndToolsActive(step, true);

            // Trigger affordance highlights
            foreach (var fx in step.affordances)
                if (fx != null) fx.TriggerEffect();

            // Play start SFX
            if (step.stepStartSFX != null)
                step.stepStartSFX.PlayAudio();

            // Play VO
            if (m_voPlayer != null && step.voStartClipIndex >= 0)
                m_voPlayer.PlayOnce(step.voStartClipIndex);

            // Subscribe to each TransformTarget's OnComplete
            SubscribeTargets(step, index);

            // Fire events
            OnStepStarted?.Invoke(index);
            OnStepNameChanged?.Invoke(step.stepName);

            Debug.Log($"[TaskManager] ► Step {index}: {step.stepName}");
        }

        private void SubscribeTargets(RepairStep step, int stepIndex)
        {
            foreach (var target in step.completionTargets)
            {
                if (target == null) continue;
                // Capture for closure
                int capturedIndex = stepIndex;
                UnityAction listener = null;
                listener = () => OnTargetCompleted(capturedIndex, target, listener);
                target.OnComplete.AddListener(listener);
            }
        }

        private void UnsubscribeAllTargets()
        {
            foreach (var step in m_steps)
                foreach (var target in step.completionTargets)
                    if (target != null) target.OnComplete.RemoveAllListeners();
        }

        private void OnTargetCompleted(int stepIndex, TransformTarget target, UnityAction listener)
        {
            if (stepIndex != m_currentStepIndex) return;

            RepairStep step = m_steps[stepIndex];
            if (step.isDone) return;

            target.OnComplete.RemoveListener(listener);
            step.targetsCompleted++;

            Debug.Log($"[TaskManager] Target completed ({step.targetsCompleted}/{step.completionTargets.Length}) " +
                      $"for step {stepIndex}: {step.stepName}");

            // All targets for this step are done
            if (step.targetsCompleted >= step.completionTargets.Length)
                StartCoroutine(FinishStep(stepIndex));
        }

        private IEnumerator FinishStep(int stepIndex)
        {
            RepairStep step = m_steps[stepIndex];
            if (step.isDone) yield break;
            step.isDone = true;

            // Play complete SFX
            if (step.stepCompleteSFX != null)
                step.stepCompleteSFX.PlayAudio();
            else if (m_objectiveCompleteSFX != null)
                m_objectiveCompleteSFX.PlayAudio();

            // Play completion VO
            if (m_voPlayer != null && step.voCompleteClipIndex >= 0)
                m_voPlayer.PlayOnce(step.voCompleteClipIndex);

            OnStepCompleted?.Invoke(stepIndex);
            Debug.Log($"[TaskManager] ✓ Step {stepIndex} complete: {step.stepName}");

            yield return new WaitForSeconds(m_stepCompletionDelay);

            // Disable parts/tools for the finished step
            SetPartsAndToolsActive(step, false);

            // Advance
            AdvanceToStep(stepIndex + 1);
        }

        private void FinishSequence()
        {
            m_sequenceFinished = true;
            if (m_allDoneSFX != null) m_allDoneSFX.PlayAudio();
            OnAllStepsCompleted?.Invoke();
            Debug.Log("[TaskManager] ✓✓ All steps complete!");
        }

        // ─────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────

        private void DisableAllSteps()
        {
            foreach (var step in m_steps)
            {
                SetPartsAndToolsActive(step, false);
                // Keep assembly roots active — they hold the bike geometry
            }
        }

        private void SetPartsAndToolsActive(RepairStep step, bool active)
        {
            foreach (var part in step.parts)
                if (part != null) part.SetActive(active);

            foreach (var tool in step.tools)
                if (tool != null) tool.SetActive(active);
        }
    }
}
