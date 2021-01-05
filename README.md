# Kowloon-Unity-Client

Development version of a client for Kowloon, a shared virtual reality.

The client pulls data from the Kowloon server running on AWS and stores it locally. Assets can also be added to (or removed from) Kowloon and uploaded to the server through the client, where they will be available to anyone else using the client.

Currently developed for Oculus Rift on Windows (other headsets/operating systems have not been tested), but support for other headsets, multiplayer, user avatars, and many other features are planned for the near future.
If you encouter any issues installing or using this client please create a new issue here on github so that we can resolve it!


# Table of Contents
1. [Installation](#installation)
2. [Contributing to Client Development](#contributing)
3. [Contributing Assets](#assets)
4. [Frequently Asked Questions](#faq)
5. [Licensing](#licensing)


## Installation
### To download the client
The client executable can be downloaded here: https://drive.google.com/file/d/1BjHB_cVL1TxFUBqTsNd9M8vGSalauvsr/view?usp=sharing

### To install the development version
1. Clone this repository
2. Open Unity Hub, in the "Projects" tab, click "Add"
3. Select the "Kowloon-Unity-Client" folder
4. Open up the project in Unity (currently developing with v.2019.4.16f1, other 2019 versions may work)


## Contributing
Guidelines for contributing are currently informal and will likely be refined as development proceeds. Contributions should generally follow the typical "Fork and Pull" process, outlined below:

 1. **Fork** the repo on GitHub
 2. **Clone** the project to your own machine
 3. **Commit** changes to your own branch
 4. **Push** your work back up to your fork
 5. Submit a **Pull request** so that we can review your changes

NOTE: Be sure to merge the latest from "upstream" before making a pull request!


## Assets
Assets are added to Kowloon through the VR client. A number of "base" asset bundles are included for easy use, however making use of bundles other users have created, or creating your own bundles, is possible. 

### Using Base Bundles
Adding assets from the "base" asset bundles is easy. Simply grab the desired prefab from one of the menus in the client and place it in the world. Once all of the desired assets have been placed select "Upload Changes" from the control menu in game. Other users will now see the placed objects in their versions of the client.

**Note** - It is possible that the upload will fail if assets from many different bundles are uploaded simultaneously, depending on the size of the bundles in question. It is recommended that you upload assets from one bundle at a time.

### Using User-Made Bundles
If there are no assets in the base bundles that can be used to build what you have in mind, it is possible to use assets that other users have contributed. Users are encouraged to make use of existing asset bundles, rather than creating new ones, whenever possible. New asset bundles take up storage space, increase download times, and cost money in server resources. Reused asset bundles are effectively free.

All user-made asset bundles are stored at the [Persistent Data Path](https://docs.unity3d.com/ScriptReference/Application-persistentDataPath.html), inside the "Kowloon-Client\LoadedAssetBundles" directory. Copy them (do not drag them, a version must remain in the "LoadedAssetBundles" directory) into the "Kowloon-Client\LoadedAssetBundles\Custom Prefabs" directory. They will then appear in-game in the "Custom Prefabs" tab of the item menu.

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
