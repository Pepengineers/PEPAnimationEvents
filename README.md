# PEPAnimationEvents
The animation events system allows binding events to the exact timings in the animations of your choice. 

## Installation
You can install via git url by adding this entry in your **manifest.json**
```
"com.pepengineers.pepanimationevents": "https://github.com/Pepengineers/PEPAnimationEvents",
```
or via Package Manager
![изображение](https://github.com/user-attachments/assets/9666de6f-8692-4b22-a8d2-2fe41d7089a1)


## Usage
Select the desired animation clip in the animator window, then press the "Add Behaviour" button and add the "Trigger Behaviour" sript:
![изображение](https://github.com/user-attachments/assets/310d339a-b01b-43ec-a8f8-10d695d0ad4f)

Then choose "IAnimationTrigger[]" in the Triggers field:
![изображение](https://github.com/user-attachments/assets/113f48ea-e3d3-4963-a4e0-e2d84675b38e)

Next press the "+" button and add the trigger of your choice to the list:
![изображение](https://github.com/user-attachments/assets/569718f4-1495-4af0-b07b-30f44e74aeb2)
You can chose the timing for the trigger using the "Trigger Time" slider. Select "Once" if you want the trigger to happen only once during a looped animation.

You can preview the object when setting "Trigger Time". Lock the animation window in the Inspector and select the object to preview. Then press the "Preview" button.
![изображение](https://github.com/user-attachments/assets/18366973-7bd2-4f3b-8bff-fd99dddd358c)

Now the object will play the animation, allowing for more prescise timing selection.
![изображение](https://github.com/user-attachments/assets/c8152ff4-b5ed-4879-a018-74ffff16f945)
