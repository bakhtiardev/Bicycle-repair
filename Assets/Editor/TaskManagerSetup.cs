// Editor-only setup helper — safe to delete after use.
// Adds TaskManager to BikeInteraction and pre-wires all discoverable references.
using UnityEngine;
using UnityEditor;
using MRBike;

public class TaskManagerSetup
{
    [MenuItem("Tools/Setup TaskManager")]
    public static void Setup()
    {
        // ── root ──────────────────────────────────────────────────────
        var root = GameObject.Find("BikeInteraction");
        if (root == null) { Debug.LogError("[TMSetup] BikeInteraction not found"); return; }

        var tm = root.GetComponent<TaskManager>() ?? root.AddComponent<TaskManager>();
        var so = new SerializedObject(tm);

        // ── shared refs ───────────────────────────────────────────────
        var voGO = GameObject.Find("BikeInteraction/VO");
        if (voGO != null)
        {
            var vop = voGO.GetComponent<TaskVOPlayer>();
            so.FindProperty("m_voPlayer").objectReferenceValue = vop;
        }

        var objComplete = GameObject.Find("BikeInteraction/sfx/BikeBuild_ObjectiveComplete");
        if (objComplete != null)
            so.FindProperty("m_objectiveCompleteSFX").objectReferenceValue =
                objComplete.GetComponent<BikeAudioTrigger>();

        // ── build step array ──────────────────────────────────────────
        var stepsProp = so.FindProperty("m_steps");
        stepsProp.arraySize = 3;

        // helper: set string field
        void SetStr(SerializedProperty p, string field, string val) =>
            p.FindPropertyRelative(field).stringValue = val;
        void SetObj(SerializedProperty p, string field, Object val) =>
            p.FindPropertyRelative(field).objectReferenceValue = val;
        void SetInt(SerializedProperty p, string field, int val) =>
            p.FindPropertyRelative(field).intValue = val;
        void SetArray(SerializedProperty p, string field, Object[] objs)
        {
            var arr = p.FindPropertyRelative(field);
            arr.arraySize = objs.Length;
            for (int i = 0; i < objs.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = objs[i];
        }

        // ─────────────────────────────────────────
        // STEP 0 — Seat Assembly
        // ─────────────────────────────────────────
        {
            var s = stepsProp.GetArrayElementAtIndex(0);
            SetStr(s, "stepName", "Seat Assembly");
            SetInt(s, "voStartClipIndex", 1);
            SetInt(s, "voCompleteClipIndex", -1);

            var seatAssembly = GameObject.Find("BikeInteraction/BikeInteractive/SeatAssembly");
            SetObj(s, "assemblyRoot", seatAssembly);

            // Parts: Seat_Grabbable from SeatHome
            var seatGrabbable = GameObject.Find(
                "BikeInteraction/ToolsAndPartsDisplay/SeatHome/Seat_Grabbable");
            SetArray(s, "parts", seatGrabbable ? new Object[] { seatGrabbable } : new Object[0]);

            // Tools: WrenchHome (seat bolts need wrench)
            var wrenchHome = GameObject.Find("BikeInteraction/ToolsAndPartsDisplay/WrenchHome");
            SetArray(s, "tools", wrenchHome ? new Object[] { wrenchHome } : new Object[0]);

            // Completion targets
            var seatTarget = GameObject.Find(
                "BikeInteraction/BikeInteractive/SeatAssembly/SeatInteractive/SeatTarget2")?
                .GetComponent<TransformTarget>();
            SetArray(s, "completionTargets",
                seatTarget ? new Object[] { seatTarget } : new Object[0]);

            // Affordances — SeatAssembly root AffordanceFX children
            var affordances = seatAssembly != null
                ? (Object[])seatAssembly.GetComponentsInChildren<AffordanceFX>(true)
                : new Object[0];
            SetArray(s, "affordances", affordances);

            // SFX
            var grabSFX = GameObject.Find("BikeInteraction/sfx/BikeBuild_SeatPostGrab");
            SetObj(s, "stepStartSFX", grabSFX?.GetComponent<BikeAudioTrigger>());
            var insertSFX = GameObject.Find("BikeInteraction/sfx/BikeBuild_SeatPostInsert");
            SetObj(s, "stepCompleteSFX", insertSFX?.GetComponent<BikeAudioTrigger>());
        }

        // ─────────────────────────────────────────
        // STEP 1 — Front Wheel Assembly
        // ─────────────────────────────────────────
        {
            var s = stepsProp.GetArrayElementAtIndex(1);
            SetStr(s, "stepName", "Front Wheel Assembly");
            SetInt(s, "voStartClipIndex", 2);
            SetInt(s, "voCompleteClipIndex", -1);

            var wheelAssembly = GameObject.Find(
                "BikeInteraction/BikeInteractive/FrontWheelAssembly");
            SetObj(s, "assemblyRoot", wheelAssembly);

            // Parts: Tire_Grabbable + Axle_Grabbable
            var tireGrabbable = GameObject.Find(
                "BikeInteraction/ToolsAndPartsDisplay/Wheelhome/Tire_Grabbable");
            var axleGrabbable = GameObject.Find(
                "BikeInteraction/ToolsAndPartsDisplay/AxleHome/Axle_Grabbable");
            var parts = new System.Collections.Generic.List<Object>();
            if (tireGrabbable) parts.Add(tireGrabbable);
            if (axleGrabbable) parts.Add(axleGrabbable);
            SetArray(s, "parts", parts.ToArray());

            // Tools: Wrench
            var wrenchHome = GameObject.Find("BikeInteraction/ToolsAndPartsDisplay/WrenchHome");
            SetArray(s, "tools", wrenchHome ? new Object[] { wrenchHome } : new Object[0]);

            // Completion targets: Wheel_Target and Axle_Target
            var wheelTarget = GameObject.Find(
                "BikeInteraction/BikeInteractive/FrontWheelAssembly/Wheel_Target")?
                .GetComponent<TransformTarget>();
            var axleTarget = GameObject.Find(
                "BikeInteraction/BikeInteractive/FrontWheelAssembly/Axle_Target")?
                .GetComponent<TransformTarget>();
            var targets = new System.Collections.Generic.List<Object>();
            if (wheelTarget) targets.Add(wheelTarget);
            if (axleTarget) targets.Add(axleTarget);
            SetArray(s, "completionTargets", targets.ToArray());

            // Affordances
            var affordances = wheelAssembly != null
                ? (Object[])wheelAssembly.GetComponentsInChildren<AffordanceFX>(true)
                : new Object[0];
            SetArray(s, "affordances", affordances);

            // SFX
            var grabSFX = GameObject.Find("BikeInteraction/sfx/BikeBuild_WheelGrab");
            SetObj(s, "stepStartSFX", grabSFX?.GetComponent<BikeAudioTrigger>());
            var insertSFX = GameObject.Find("BikeInteraction/sfx/BikeBuild_FrontWheelInsert");
            SetObj(s, "stepCompleteSFX", insertSFX?.GetComponent<BikeAudioTrigger>());
        }

        // ─────────────────────────────────────────
        // STEP 2 — Pedal Assembly
        // ─────────────────────────────────────────
        {
            var s = stepsProp.GetArrayElementAtIndex(2);
            SetStr(s, "stepName", "Pedal Assembly");
            SetInt(s, "voStartClipIndex", 3);
            SetInt(s, "voCompleteClipIndex", -1);

            var pedalAssembly = GameObject.Find(
                "BikeInteraction/BikeInteractive/PedalAssembly");
            SetObj(s, "assemblyRoot", pedalAssembly);

            // Parts: left + right pedal grabbables
            var leftPedal = GameObject.Find(
                "BikeInteraction/ToolsAndPartsDisplay/left_pedal_home/Pedal_left_Grabable");
            var rightPedal = GameObject.Find(
                "BikeInteraction/ToolsAndPartsDisplay/right_pedal_home/Pedal_right_Grabable");
            var parts = new System.Collections.Generic.List<Object>();
            if (leftPedal) parts.Add(leftPedal);
            if (rightPedal) parts.Add(rightPedal);
            SetArray(s, "parts", parts.ToArray());

            // Tools: Wrench
            var wrenchHome = GameObject.Find("BikeInteraction/ToolsAndPartsDisplay/WrenchHome");
            SetArray(s, "tools", wrenchHome ? new Object[] { wrenchHome } : new Object[0]);

            // Completion targets: LeftPedalTarget + RightPedalTarget on the Arms
            var leftTarget = GameObject.Find(
                "BikeInteraction/BikeInteractive/Bike_v3/Crankset/ArmsParent/Arms/LeftPedalTarget")?
                .GetComponent<TransformTarget>();
            var rightTarget = GameObject.Find(
                "BikeInteraction/BikeInteractive/Bike_v3/Crankset/ArmsParent/Arms/RightPedalTarget")?
                .GetComponent<TransformTarget>();
            var targets = new System.Collections.Generic.List<Object>();
            if (leftTarget) targets.Add(leftTarget);
            if (rightTarget) targets.Add(rightTarget);
            SetArray(s, "completionTargets", targets.ToArray());

            // Affordances
            var pedalAffordances = new System.Collections.Generic.List<Object>();
            var leftHolo = GameObject.Find(
                "BikeInteraction/BikeInteractive/Bike_v3/Crankset/ArmsParent/Arms/Left_Pedal_Hologram");
            var rightHolo = GameObject.Find(
                "BikeInteraction/BikeInteractive/Bike_v3/Crankset/ArmsParent/Arms/Right_Pedal_hologram");
            if (leftHolo) pedalAffordances.AddRange(leftHolo.GetComponentsInChildren<AffordanceFX>(true));
            if (rightHolo) pedalAffordances.AddRange(rightHolo.GetComponentsInChildren<AffordanceFX>(true));
            if (pedalAssembly) pedalAffordances.AddRange(pedalAssembly.GetComponentsInChildren<AffordanceFX>(true));
            SetArray(s, "affordances", pedalAffordances.ToArray());

            // SFX
            var grabSFX = GameObject.Find("BikeInteraction/sfx/BikeBuild_PedalAttach");
            SetObj(s, "stepStartSFX", grabSFX?.GetComponent<BikeAudioTrigger>());
            var twistSFX = GameObject.Find("BikeInteraction/sfx/BikeBuild_PedalTwist");
            SetObj(s, "stepCompleteSFX", twistSFX?.GetComponent<BikeAudioTrigger>());
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(tm);
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("[TMSetup] TaskManager configured and saved. ✓");
        // Log what was wired
        Debug.Log($"[TMSetup] Steps wired: {tm.name}");
        for (int i = 0; i < 3; i++)
        {
            var step = stepsProp.GetArrayElementAtIndex(i);
            Debug.Log($"  [{i}] {step.FindPropertyRelative("stepName").stringValue}  " +
                      $"parts={step.FindPropertyRelative("parts").arraySize}  " +
                      $"tools={step.FindPropertyRelative("tools").arraySize}  " +
                      $"targets={step.FindPropertyRelative("completionTargets").arraySize}");
        }
    }
}
