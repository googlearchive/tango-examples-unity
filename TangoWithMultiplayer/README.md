TangoWithMultiplayer
===========================================
Copyright (C) 2016 Google Inc.


Contents
--------
This project uses the Tango SDK and Photon SDK to create a multi-user cube stacking experience in a shared world. Area Descriptions are used to have all users localized to the shared world. All players need to be near each other to relocalize.

The first user to start the experience must choose an existing Area Description and acts as the host. When an new user joins the experience, the host sends that Area Description over the network. When the send is complete, the new user loads that Area Description and localizes to it. Once localized, all players can see each other and together edit a shared cube stacking world.

Project Setup
--------------
Even though we configured the project to be directly runnable on Tango devices, we intentionally left the PUN Appid setup field to blank as it's different per application.

In order to set it up, open "Window->Photon Network Setting->PUN Wizard". In the "PUN Wizard", open "PUN Setup window", and fill the "Appid or Email" field.

You will need to register a PUN AppID through Photon [official website](https://www.photonengine.com/en/PUN). You can find more setup details from [Photon Setup tutorial](https://doc.photonengine.com/en/pun/current/getting-started/initial-setup)
