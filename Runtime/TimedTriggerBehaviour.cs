using UnityEngine;

namespace Pepengineers.PEPAnimationEvents.Runtime
{
    internal sealed class TimedTriggerBehaviour : TriggerBehaviour
    {
        [SerializeField] [Range(0f, 1f)] private float triggerTime;
        [SerializeField] private bool once;
        public float TriggerTime => triggerTime;
        
        private bool hasTriggered;
        private uint triggeredCount;
        
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            triggeredCount = 0;
            hasTriggered = false;
            if (triggerTime <= 0.00f)
            {
                Notify(animator);
            }
        }
        
        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (once && hasTriggered) return;

            var totalTime = stateInfo.normalizedTime;
            var currentIterationTime = totalTime % 1f;
            var iterationsCompleted = totalTime - currentIterationTime;
            
            if (hasTriggered)
            {
                if (totalTime <= triggeredCount) return;
                hasTriggered = false; 
            }

            if (triggerTime >= 1f) 
            {
                if (iterationsCompleted <= triggeredCount) return; 
            }
            else 
            {
                if (currentIterationTime < triggerTime) return; 
            } 
            
            Notify(animator);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (triggerTime < 1f) return;
            if (hasTriggered) return;
                
            if (stateInfo.normalizedTime + Time.deltaTime >= 1f)
            {
                Notify(animator);
            }
        }

        protected override void Notify(Animator animator)
        {
            base.Notify(animator);
            hasTriggered=true;
            triggeredCount++;
        }
    }
}
