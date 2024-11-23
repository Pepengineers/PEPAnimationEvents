using Pepengineers.PEPAnimationEvents.Interfaces;
using UnityEngine;

namespace Pepengineers
{
    public class LogTrigger : IAnimationTrigger
    {
        public void Invoke(Animator animator)
        {
            Debug.Log("Animation event was invoked!");
        }
    }
}
