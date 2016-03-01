Project Tango UnitySDK Examples
===========================================
Copyright (C) 2015 Google Inc.


Useful Websites
---------------
SDK Download - https://developers.google.com/project-tango/downloads

Developer Website - https://developers.google.com/project-tango/apis/unity


Contents
--------
This contains the Project Tango UnitySDK examples and tutorial projects for Unity 5 and above.


#### UnityExamples
This project contains examples for Unity 5 and above.  Each example is in its own scene:
* **DetectTangoCore** - This example shows how to show UIs only if the Project Tango APIs are available.
* **MotionTracking** - How to use the motion tracking APIs.
* **PointCloud** - How to use the depth APIs and use the pose data to transform the point cloud into world coordinates.
* **AreaLearning** - How to use the Area Description motion APIs and place objects at specific spots in the area description. 
* **AreaDescriptionManagement** - How to use the Area Description management APIs and create a new area description.
* **AugmentedReality** - How to create an augmented reality experience using the video camera overlay APIs and motion tracking APIs.
* **MeshBuilder(Experimental)** - How to build a mesh using the motion tracking APIs and depth APIs.


#### MotionTrackingTutorialStart
This project contains a starting point for the motion tracking tutorial on the Project Tango developer website.

#### TangoWithCardboardExperiments
This experimental project shows an example of how to integrate Cardboard UnitySDK and Tango UnitySDK together to create a 6DOF VR experience.

#### TangoWithMultiplayer
This project is an example of a networked multi-user Tango experience in a shared world using the [Photon Unity Networking SDK](https://www.photonengine.com/en/PUN).

Project Layout
--------------
Projects commonly have the following folders:
* **Google-Unity/** - General Android lifecycle management
* **Plugins/Android/** - General Android and Tango-specific libraries
* **Standard Assets/** - Unity packages we are using
* **TangoPrefabs/** - Common Tango prefabs, like a simple camera
* **TangoSDK/** - Unity3D interface to the Tango libraries
* **TangoSDK/Examples** - One subfolder here per example
* **TangoSDK/Examples/Scenes/** - All the Tango example scenes


Support
-------
First please take a look at our [FAQ](http://stackoverflow.com/questions/tagged/google-project-tango?sort=faq&amp;pagesize=50) page. Most of the issues can be solved by the FAQ section.

If you have general API questions related to Tango, we encourage you to post your question to our [stack overflow page](http://stackoverflow.com/questions/tagged/google-project-tango).

You are also welcome to visit [Project Tango Developer website](https://developers.google.com/project-tango/) to learn more about general concepts and other information about the project.


Contribution
------------
Want to contribute? Great! First, read this page (including the small print at the end).

#### Before you contribute
Before we can use your code, you must sign the
[Google Individual Contributor License Agreement](https://developers.google.com/open-source/cla/individual?csw=1)
(CLA), which you can do online. The CLA is necessary mainly because you own the
copyright to your changes, even after your contribution becomes part of our
codebase, so we need your permission to use and distribute your code. We also
need to be sure of various other things—for instance that you'll tell us if you
know that your code infringes on other people's patents. You don't have to sign
the CLA until after you've submitted your code for review and a member has
approved it, but you must do it before we can put your code into our codebase.
Before you start working on a larger contribution, you should get in touch with
us first through the issue tracker with your idea so that we can help out and
possibly guide you. Coordinating up front makes it much easier to avoid
frustration later on.

#### Code reviews
All submissions, including submissions by project members, require review. We
use Github pull requests for this purpose.

#### The small print
Contributions made by corporations are covered by a different agreement than
the one above, the Software Grant and Corporate Contributor License Agreement.
