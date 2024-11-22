# PEPAnimationEvents
The animation events system allows binding events to the exact timings in the animations of your choice. 

## Installation
You can install via git url by adding this entry in your **manifest.json**
```
"com.pepengineers.pepanimationevents": "https://github.com/Pepengineers/PEPAnimationEvents",
```

## Usage
Select the desired animation clip in the animator window, then press the "Add Behaviour" button and add the "Trigger Behaviour" sript:
(image placeholder)

Then choose "IAnimationTrigger[]" in the Triggers field:
(image placeholder)

Add a new trigger of desired type (Global or Target). Then create a Scriptable Object of type GlobalEvent or TargetEvent, based on your trigger choice, and drag the SO to the "Target Event" field:
(image placeholder)

Add an "Event Subscriber" component to your desired game object:
(image placeholder)

Inside the "Event Subscriber" component create a new event of the same type as your Trigger and SO. Put the SO as the "Key" in the according field and then add the value - the action that should be performed when the event happens:
(image placeholder)
