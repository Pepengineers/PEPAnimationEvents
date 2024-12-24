using Pepengineers.PEPAnimationEvents.Interfaces;
using UnityEngine;

namespace Pepengineers.PEPAnimationEvents.Runtime
{
    public abstract class TriggerBehaviour : StateMachineBehaviour
    {
        [SerializeReference] protected IAnimationTrigger[] triggers;
        
        protected virtual void Notify(Animator animator)
        {
            foreach (var trigger in triggers)
            {
                trigger.Invoke(animator);
            }
        }
    }
}