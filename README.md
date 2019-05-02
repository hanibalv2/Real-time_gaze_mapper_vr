# Real-time_gaze_mapper_vr
![](https://github.com/hanibalv2/Real-time_gaze_mapper_vr/blob/master/images/result_invert.PNG)

![](https://github.com/hanibalv2/Real-time_gaze_mapper_vr/blob/master/images/result_view.PNG)

This tool can be used to project the gaze of a user on a surface of virtual enviroment in real time.
It is also shipped with a real time heatmap to map user movement in the room.

# Setup PC
  The application can also be used without a HMD. Just start the "TestRoomEyeTrackingLabyrinth" scene.

# Setup VR 
  1. Download pupil [hier](https://github.com/pupil-labs/pupil/releases).
  2. Start "pupil_service.exe".
  3. Start the calibration scene "CalibrationPupilLabsVR" in unity. 

# Dependencies
  - ValveSoftware/SteamVR plugin for unity (include in this project) [https://github.com/ValveSoftware/steamvr_unity_plugin](https://github.com/ValveSoftware/steamvr_unity_plugin)
  - Pupil-laps/HMD-eyes plugin (include in this project) [https://github.com/pupil-labs/hmd-eyes](https://github.com/pupil-labs/hmd-eyes)
  - Chaser324/Unity Wireframe Shaders (include in this project) [https://github.com/Chaser324/unity-wireframe](https://github.com/Chaser324/unity-wireframe)
  - Pupil-labs/pupil [https://github.com/pupil-labs/pupil (https://github.com/pupil-labs/pupil)
    - Latet releases: [https://github.com/pupil-labs/pupil/releases](https://github.com/pupil-labs/pupil/releases)

# User Controls

## General 
<kbd>P</kbd> - Screenshot 

## Debug 
  <kbd>W</kbd> <kbd>A</kbd> <kbd>S</kbd> <kbd>D</kbd> : User Movement 

  <kbd>Left Shift</kbd> : Raise Position

  <kbd>Left STRG</kbd>: Lower Position

## Heatmap 

   <kbd>V</kbd> - Start/Stop tracking

   <kbd>B</kbd> - Lower smoothvalue  

   <kbd>N</kbd> - Raise smoothvalue

   <kbd>M</kbd> - Hide/Show Heatmap

## Gaze-mapper

   <kbd>F</kbd> - Start/Stop tracking

  <kbd>G</kbd> - Lower threshold value

   <kbd>H</kbd> - Raise threshold value

   <kbd>J</kbd> - Hide/show grid

   <kbd>T</kbd> - switch view (inverted view)
