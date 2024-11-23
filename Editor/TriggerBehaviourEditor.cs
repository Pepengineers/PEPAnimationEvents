using System;
using System.Linq;
using System.Reflection;
using Pepengineers.PEPAnimationEvents.Runtime;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Pepengineers.PEPAnimationEvents.Editor
{
    /// <summary>
    ///     Custom editor for the AnimationEventStateBehaviour class, providing a GUI for previewing animation states
    ///     and handling animation events within the Unity editor. Enables users to preview animations and manage
    ///     animation events directly in the editor.
    /// </summary>
    [CustomEditor(typeof(TriggerBehaviour), true)]
    internal sealed class TriggerBehaviourEditor :
#if ODIN_INSPECTOR
        OdinEditor
#else
        Editor
#endif
    {
        private Motion previewClip;
        private float previewTime;
        private bool isPreviewing;

        private PlayableGraph playableGraph;
        private AnimationMixerPlayable mixer;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var stateBehaviour = (TriggerBehaviour)target;

            if (Validate(stateBehaviour, out var errorMessage))
            {
                GUILayout.Space(10);

                if (isPreviewing)
                {
                    if (GUILayout.Button("Stop Preview"))
                    {
                        EnforceTPose();
                        isPreviewing = false;
                        AnimationMode.StopAnimationMode();
                        
                        if (playableGraph.IsValid()) playableGraph.Destroy();
                    }
                    else
                    {
                        PreviewAnimationClip(stateBehaviour);
                    }
                }
                else if (GUILayout.Button("Preview"))
                {
                    isPreviewing = true;
                    AnimationMode.StartAnimationMode();
                }

                GUILayout.Label($"Previewing at {previewTime:F2}s", EditorStyles.helpBox);
            }
            else
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Info);
            }
        }

        private void PreviewAnimationClip(TriggerBehaviour stateBehaviour)
        {
            var animatorController = GetValidAnimatorController(out var errorMessage);
            if (animatorController == null)
            {
                Debug.LogError(errorMessage);
                return;
            }

            var matchingState = animatorController.layers
                .Select(layer => FindMatchingState(layer.stateMachine, stateBehaviour))
                .FirstOrDefault(state => state.state != null);

            if (matchingState.state == null) return;

            var motion = matchingState.state.motion;

            // Handle BlendTree logic
            if (motion is BlendTree)
            {
                SampleBlendTreeAnimation(stateBehaviour, stateBehaviour.TriggerTime);
                return;
            }

            // If it's a simple AnimationClip, sample it directly
            if (motion is not AnimationClip clip) return;
            previewTime = stateBehaviour.TriggerTime * clip.length;
            AnimationMode.SampleAnimationClip(Selection.activeGameObject, clip, previewTime);
        }

        private void SampleBlendTreeAnimation(TriggerBehaviour stateBehaviour, float normalizedTime)
        {
            var animator = Selection.activeGameObject.GetComponent<Animator>();

            if (playableGraph.IsValid()) playableGraph.Destroy();

            playableGraph = PlayableGraph.Create("BlendTreePreviewGraph");
            mixer = AnimationMixerPlayable.Create(playableGraph, 1);

            var output = AnimationPlayableOutput.Create(playableGraph, "Animation", animator);
            output.SetSourcePlayable(mixer);

            var animatorController = GetValidAnimatorController(out var errorMessage);
            if (animatorController == null)
            {
                Debug.LogError(errorMessage);
                return;
            }

            var matchingState = animatorController.layers
                .Select(layer => FindMatchingState(layer.stateMachine, stateBehaviour))
                .FirstOrDefault(state => state.state != null);

            // If the matching state is not a BlendTree, bail out
            if (matchingState.state.motion is not BlendTree blendTree) return;

            // Determine the maximum threshold value in the blend tree
            var maxThreshold = blendTree.children.Max(child => child.threshold);

            var clipPlayables = new AnimationClipPlayable[blendTree.children.Length];
            var weights = new float[blendTree.children.Length];
            var totalWeight = 0f;

            // Scale target weight according to max threshold
            var targetWeight = Mathf.Clamp(normalizedTime * maxThreshold, blendTree.minThreshold, maxThreshold);

            for (var i = 0; i < blendTree.children.Length; i++)
            {
                var child = blendTree.children[i];
                var weight = CalculateWeightForChild(blendTree, child, targetWeight);
                weights[i] = weight;
                totalWeight += weight;

                var clip = GetAnimationClipFromMotion(child.motion);
                clipPlayables[i] = AnimationClipPlayable.Create(playableGraph, clip);
            }

            // Normalize weights so they sum to 1
            for (var i = 0; i < weights.Length; i++) weights[i] /= totalWeight;

            mixer.SetInputCount(clipPlayables.Length);
            for (var i = 0; i < clipPlayables.Length; i++)
            {
                mixer.ConnectInput(i, clipPlayables[i], 0);
                mixer.SetInputWeight(i, weights[i]);
            }

            AnimationMode.SamplePlayableGraph(playableGraph, 0, normalizedTime);
        }


        private float CalculateWeightForChild(BlendTree blendTree, ChildMotion child, float targetWeight)
        {
            var weight = 0f;

            switch (blendTree.blendType)
            {
                case BlendTreeType.Simple1D:
                {
                    // Find the neighbors around the target weight
                    ChildMotion? lowerNeighbor = null;
                    ChildMotion? upperNeighbor = null;

                    foreach (var motion in blendTree.children)
                    {
                        if (motion.threshold <= targetWeight && (lowerNeighbor == null || motion.threshold > lowerNeighbor.Value.threshold)) lowerNeighbor = motion;

                        if (motion.threshold >= targetWeight && (upperNeighbor == null || motion.threshold < upperNeighbor.Value.threshold)) upperNeighbor = motion;
                    }

                    if (lowerNeighbor.HasValue && upperNeighbor.HasValue)
                    {
                        if (Mathf.Approximately(child.threshold, lowerNeighbor.Value.threshold))
                            weight = 1.0f - Mathf.InverseLerp(lowerNeighbor.Value.threshold, upperNeighbor.Value.threshold, targetWeight);
                        else if (Mathf.Approximately(child.threshold, upperNeighbor.Value.threshold))
                            weight = Mathf.InverseLerp(lowerNeighbor.Value.threshold, upperNeighbor.Value.threshold, targetWeight);
                    }
                    else
                    {
                        // Handle edge cases where there is no valid interpolation range
                        weight = Mathf.Approximately(targetWeight, child.threshold) ? 1f : 0f;
                    }

                    break;
                }
                case BlendTreeType.FreeformCartesian2D or BlendTreeType.FreeformDirectional2D:
                {
                    Vector2 targetPos = new(
                        GetBlendParameterValue(blendTree, blendTree.blendParameter),
                        GetBlendParameterValue(blendTree, blendTree.blendParameterY)
                    );
                    var distance = Vector2.Distance(targetPos, child.position);
                    weight = Mathf.Clamp01(1.0f / (distance + 0.001f));
                    break;
                }
            }

            return weight;
        }


        private float GetBlendParameterValue(BlendTree blendTree, string parameterName)
        {
            var methodInfo = typeof(BlendTree).GetMethod("GetInputBlendValue", BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo != null) return (float)methodInfo.Invoke(blendTree, new object[] { parameterName });
            Debug.LogError("Failed to find GetInputBlendValue method via reflection.");
            return 0f;

        }

        private static ChildAnimatorState FindMatchingState(AnimatorStateMachine stateMachine, TriggerBehaviour stateBehaviour)
        {
            foreach (var state in stateMachine.states)
                if (state.state.behaviours.Contains(stateBehaviour))
                    return state;

            foreach (var subStateMachine in stateMachine.stateMachines)
            {
                var matchingState = FindMatchingState(subStateMachine.stateMachine, stateBehaviour);
                if (matchingState.state != null) return matchingState;
            }

            return default;
        }

        private bool Validate(TriggerBehaviour stateBehaviour, out string errorMessage)
        {
            var animatorController = GetValidAnimatorController(out errorMessage);
            if (animatorController == null) return false;

            var matchingState = animatorController.layers
                .Select(layer => FindMatchingState(layer.stateMachine, stateBehaviour))
                .FirstOrDefault(state => state.state != null);

            previewClip = GetAnimationClipFromMotion(matchingState.state?.motion);
            if (previewClip != null) return true;
            errorMessage = "No valid AnimationClip found for the current state.";
            return false;

        }

        private static AnimationClip GetAnimationClipFromMotion(Motion motion)
        {
            return motion switch
            {
                AnimationClip clip => clip,
                BlendTree blendTree => blendTree.children.Select(child => GetAnimationClipFromMotion(child.motion)).FirstOrDefault(childClip => childClip != null),
                _ => null
            };
        }

        private static AnimatorController GetValidAnimatorController(out string errorMessage)
        {
            errorMessage = string.Empty;

            var targetGameObject = Selection.activeGameObject;
            if (targetGameObject == null)
            {
                errorMessage = "Please select a GameObject with an Animator to preview.";
                return null;
            }

            var animator = targetGameObject.GetComponent<Animator>();
            if (animator == null)
            {
                errorMessage = "The selected GameObject does not have an Animator component.";
                return null;
            }

            var animatorController = animator.runtimeAnimatorController as AnimatorController;
            if (animatorController != null) return animatorController;
            errorMessage = "The selected Animator does not have a valid AnimatorController.";
            return null;

        }

        [MenuItem("GameObject/Enforce T-Pose", false, 0)]
        private static void EnforceTPose()
        {
            var selected = Selection.activeGameObject;
            if (!selected || !selected.TryGetComponent(out Animator animator) || !animator.avatar) return;

            var skeletonBones = animator.avatar.humanDescription.skeleton;

            foreach (HumanBodyBones hbb in Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (hbb == HumanBodyBones.LastBone) continue;

                var boneTransform = animator.GetBoneTransform(hbb);
                if (!boneTransform) continue;

                var skeletonBone = skeletonBones.FirstOrDefault(sb => sb.name == boneTransform.name);
                if (skeletonBone.name == null) continue;

                if (hbb == HumanBodyBones.Hips) boneTransform.localPosition = skeletonBone.position;
                boneTransform.localRotation = skeletonBone.rotation;
            }
        }
    }
}