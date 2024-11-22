using Pepengineers.PEPAnimationEvents.Interfaces;
using UnityEngine;

namespace Pepengineers.PEPAnimationEvents.Runtime
{
    public class TriggerBehaviour : StateMachineBehaviour
    {
        [SerializeReference] private IAnimationTrigger[] triggers;
    
        [SerializeField] [Range(0f, 1f)] private float triggerTime;
        
        public float TriggerTime => triggerTime;

        private bool hasTriggered;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) 
        {
            hasTriggered = false;
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) 
        {
            
            float currentTime = stateInfo.normalizedTime % 1f;

            if (!hasTriggered && currentTime >= triggerTime) 
            {
                Notify(animator);
                hasTriggered = true;
            }
        }
    
        private void Notify(Animator animator)
        {
            foreach (var trigger in triggers)
            {
                trigger.Invoke(animator);
            }
        }

    }
}
