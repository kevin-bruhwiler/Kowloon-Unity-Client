# Kowloon-Unity-Client

Development version of a client for Kowloon, a shared virtual reality.

The client pulls data from the Kowloon server running on AWS and stores it locally. Assets can also be added to (or removed from) Kowloon and uploaded to the server through the client, where they will be available to anyone else using the client.

Currently developed for Oculus Rift on Windows (other headsets/operating systems have not been tested), but support for other headsets, multiplayer, user avatars, persistent accounts, and many other features are planned for the near future.
If you encouter any issues installing or using this client please create a new issue here on github so that we can resolve it!


# Table of Contents
1. [Installation](#installation)
2. [Contributing to Client Development](#contributing)
3. [Contributing Assets](#assets)
4. [Frequently Asked Questions](#faq)
5. [Licensing](#licensing)


## Installation
### To download the client
The latest version of the client executable can be downloaded here: https://github.com/kevin-bruhwiler/Kowloon-Unity-Client/releases

### To install the development version
1. Clone this repository
2. Open Unity Hub, in the "Projects" tab, click "Add"
3. Select the "Kowloon-Unity-Client" folder
4. Open up the project in Unity (currently developing with v.2019.4.16f1, other 2019 versions may work)
5. Open the scene named "Root"


## Contributing
Guidelines for contributing are currently informal and will likely be refined as development proceeds. Contributions should generally follow the typical "Fork and Pull" process, outlined below:

 1. **Fork** the repo on GitHub
 2. **Clone** the project to your own machine
 3. **Commit** changes to your own branch
 4. **Push** your work back up to your fork
 5. Submit a **Pull request** so that we can review your changes

NOTE: Be sure to merge the latest from "upstream" before making a pull request!


## Assets
Assets are added to Kowloon through the VR client. A number of "base" asset bundles are included for easy use, however creating your own bundles, or making use of bundles other users have created, is possible. 

### Using Base Bundles
Adding assets from the "base" asset bundles is easy. Simply grab the desired prefab from one of the menus in the client and place it in the world. Once all of the desired assets have been placed select "Upload Changes" from the control menu in game. Other users will now see the placed objects in their versions of the client.

### Using User-Made Bundles
If there are no assets in the base bundles that can be used to build what you have in mind, it is possible to use assets that other users have contributed. Users are encouraged to make use of existing asset bundles, rather than creating new ones, whenever possible. New asset bundles take up storage space, increase download times, and cost money in server resources. Reused asset bundles are effectively free.

All user-made asset bundles are stored at the [Persistent Data Path](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html), inside the "Kowloon-Client\LoadedAssetBundles" directory. Copy them (do not drag them, a version must remain in the "LoadedAssetBundles" directory) into the "Kowloon-Client\LoadedAssetBundles\Custom Prefabs" directory. They will then appear in-game in the "Custom Prefabs" tab of the item menu.

### Creating Custom Bundles
It is possible to create custom asset bundles and add them to Kowloon. The process of creating asset bundles can be found [here](https://docs.unity3d.com/Manual/AssetBundles-Workflow.html). If you're using the Kowloon-Unity-Client development version (which is recommended when creating custom assets) the CreateAssetBundles script will already be present. Custom asset bundles should be placed in the "Kowloon-Client\LoadedAssetBundles\Custom Prefabs" directory at the [Persistent Data Path](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html), which will make all prefabs within the asset bundles available to be placed in-game.

Prefabs within custom asset bundles are expected to have some sort of collider, a kinematic rigidbody without gravity, and the OVRGrabbable script attached.

**Important Notes About Custom Asset Bundles**
1. Give your bundle an informative name so that other users who wish to make use of it can find it easily
2. Bundles with duplicate names *will not be uploaded*! Search inside the LoadedAssetBundles directory to make sure that no bundles with the same name already exist
3. *Do not treat any uploads as final*. Development may require wiping the server from time-to-time, although this will be avoided as much as possible. Keep backups of all assets you create just in case
4. It is possible that the upload will fail if assets from many new bundles are uploaded simultaneously, depending on the size of the bundles in question.
5. The server will not accept uploads greater than 20MB, consequently any asset bundle larger than 20MB cannot be uploaded (and because uploads contain additional data, asset bundles slightly smaller than 20MB may also fail).
6. Each new asset bundle increases the time it takes users to download Kowloon, the space Kowloon requires on every user's system, and the cost (in $$) of operating the Kowloon server. Consider reducing the size of your custom asset bundles by:
    * Using low resolution textures and simple meshes/animations
    * Using mesh and animation compression
    * Re-using textures within a bundle - especially useful when creating a bundle of many themed assets
7. Scripts can be added to custom prefabs, but compiled code cannot. This means that any script must be compiled and built within the client (see the section on [Contributing to Client Development](#contributing) for details on how to add scripts to the client). This may seem like an inconvenience but has several significant advantages:
    * Updating a single script in the client will effectively update every prefab that uses it
    * Reusing scripts other users have added to their prefabs is easy, allowing for well-maintained common code
    * All scripts will be vetted by the client developers before inclusion, so users can trust that no prefabs contain malicious code, malware, or scripts detrimental to performance


## FAQ
* **My upload/download is failing with error code 429**
    * The server limits the number of uploads and downloads from a client to three per hour, each (the client automatically downloads the latest updates on startup). In general this isn't an issue, at worst your world will be an hour out of date or you will need to wait for a while before uploading
* **My upload is failing with error code 413**
    * This means that the combined size of the asset bundles you are trying to upload is greater than 20MB, which is prohibited. Either upload assets from one bundle at a time or use a smaller bundle
* **My upload is failing with error code 5\*\***
    * This is a server issue. The most likely problem is that you are trying to build too far away from the spawn and the server has not yet allocated resources for that region. Regardless, please create an issue here with what you were trying to do, what the error code was, and any other information you deem necessary
* **After reloading the client I don't see any of the changes I just made**
    * Kowloon is moderated, and all additions must be approved before they will be visible. Additionally, the server may take a few minutes to propagate changes, so even moderator approved additions may not be instantly visible to all users
* **Objects I remove keep reappearing**
    * Only moderators can remove objects once they've been uploaded. If you identify an object that you would like removed, please contact a moderator
* **What other frequently asked questions should be answered here?**
    * No idea. We'll find out. Writing an FAQ before any questions have been asked is hard :upside_down_face:

## Licensing
MIT License

Copyright (c) 2021 Kevin Bruhwiler

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
