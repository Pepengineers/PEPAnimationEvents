using UnityEngine;

namespace Pepengineers.PEPAnimationEvents.Runtime
{
    internal sealed class TransitionTriggerBehaviour : TriggerBehaviour
    {
        [SerializeField] private bool onEnter;
        [SerializeField] private bool onExit;
        
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (onEnter) base.Notify(animator);
        }
        
        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (onExit) base.Notify(animator);
        }
    }
}