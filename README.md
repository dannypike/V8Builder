# V8Builder
This is a .Net helper "wizard" for configuring builds of Google's V8 engine on Windows using Visual Studio 2017. It may work for other operating systems that can but it's built as a WPF .Net application and I haven't tested it with Mono. Life is short.

### Summary

The wizard uses the various V8/Chromium depot_tools scripts to do the hard work. It's main purpose it to avoid having to remember what all of the commands are that you have to write and in which order. There is a TL;DR reminder section in this README for those who are already familiar with it, but you are strongly recommended to skim quickly through the whole README if this is your first time with V8Builder.

Google's V8 engine is a very complicated product and has a complex and time-consuming build process, even if it works first time. If this is your first time trying to build it on Windows, you may save a lot of time (days or weeks) and frustration by spending a few minutes reading the whole of this document.

### TL;DR

I haven't managed to find a simple TL;DR technique for building V8, especially on Windows. But, for completeness and for the sake of those who enjoy wasting time on dealing with complex commands and waiting for long builds, here is a summary of the steps to take for V8 to build successfully on Windows. V8Builder offers finer control with more options than this, but you're reading a TL;DR, so ...

1. Download the latest bundle of ```depot_tools``` from [here](https://storage.googleapis.com/chrome-infra/depot_tools.zip).

2. Unzip it into a folder with a nice simple name (no spaces or weird characters), e.g. ```C:\depot_tools```

3. Open a command window and add that folder to the start of your PATH:

```
C:\> PATH=C:\depot_tools;%PATH%
```

3. Make the new folder your current directory:

```
C:\> cd depot_tools
```

4. Run CIPD to install some of the tools and wait a while. It will eventually dump a load of text/options to the display:

```
C:\depot_tools> cipd
```

5. Run gclient to insatall the windows tools and wait even longer. It will also dump a load of text when it's finished.

```
C:\depot_tools> gclient
```

6. Make a folder to hold the V8 build itself, e.g ```C:\BuildV8```

```
C:\depot_tools> md C:\BuildV8
```

7. Change to that folder

```
C:\depot_tools> cd C:\BuildV8
```

8. Run the fetch tool to download the V8 source. Wait for it to complete (a few minutes).

```
C:\BuildV8> fetch v8
```

9. Change to the V8 subdirectory. This is the step that I usually forget!

```
C:\BuildV8> cd v8
```

10. Run the ```v8gen.py``` script to configure the build. This is the step that almost always fails for me, apparently because I have various versions of lots of things on my PC and lots of extraneous stuff in my PATH and I'm not set up to auto-run python.

```
C:\BuildV8\v8> python tools\dev\v8gen.py gen -b x64.release x64.release --no-goma -- is_clang=false treat_warnings_as_errors=false
```

11. Use ninja to build V8:

```
C:\BuildV8\v8> ninja -C out.gn\x64.release
```

This should result in a 64-bit optimised, static library, Visual Studio-compatible build of V8.

If it doesn't work, you could join the rest of those who have given up in the past. Or, you could stop reading the TL;DR and try V8Builder, instead. It's not been tested on a lot of systems yet, but it builds V8 fine for me from scratch, and I spent a lot of time searching for and collating hints and tips scattered throughout the Internet in order to write it. Hopefully you will find it a useful tool.

### Overview

The V8Builder user interface consists of two tabs. The first tab called "Setup" is where you tell it where to find V8 on the web and where to store it on your machine. The second tab called "Build" is where you choose the various options for building V8 and actually trigger a build.

You can stop and restart the wizard without losing the progress that you have made up until that point V8Builder caches relevant information and reloads it when you restart.

There is a Status Panel at the bottom of V8Builder's window. This shows what is happening at the moment and may include an error message if one of the operations fails. V8Builder also uses message boxes to report errors, such as warning you about missing folders or something that you need to click before you can proceed. The Status panel also includes two buttons:

1. ```HELP```
Clicking this button will open this REAMDE in your current browser.

2. ```EXPLORE CONFIG```
This will open Windows Explorer at the folder that V8Builder is using to store its configuration (the ```Save depot_tools``` folder in the ```Setup``` tab).

V8Builder is laid out in a wizard format. The general principle is that you verify the input fields and then click the buttons on the first tab in a sensible order, then do the same on the second tab. Each time that you click a button, V8Builder either displays a dialog box to prompt you for the location of a file or folder (typically the ```BROWSE...``` buttons do that), or it opens a Command Window and uses it to runone or more of the Google ```depot_tools``` scripts.

You must close the Command Window explicitly, when the operation is completed (V8Builder will display a message to this effect and dump you to the prompt:

```

Please close this window before continuing with V8Builder

C:\v8\v8>
```

You may type "exit" at the prompt and hit Enter, or click the ```X``` button at the top-right of the Command Window.

V8Builder does this so that you can check for any errors being output by the build scripts. Note that you will be unable to click any more "action" buttons in V8Builder until you close this window (though you can continue to use the rest of the user -interface to set up build options etc.)

### Beware of using existing folders and editing files

V8Builder may choose to delete files and folders that it thinks it owns - essentially anything that it has previously downloaded or needs to overwrite. It always uses the Recycle Bin, so you can recover them in an emergency. If you are low on disk space, and you know that you don't want these folders, it is safe to delete them permanently from your Recycle Bin in order to recover the space.

### Setup tab

The Setup tab is used to create the V8 build environment and update the source code from the Google Git servers. The tab contains three important text boxes (ignore the ones at the bottom - they are reserved for future releases):

1. ```Download depot tools from this url```

This is the web site that Google uses to publish the core set of depot_tools for Windows. You shouldn't need to change the URL unless you know something that I don't.

2. ```Save depot_tools to this folder```

This is the folder on your machine that V8Builder will use as its scratchpad. It holds temporary files, saved configuration information, internal script files and also is where it unpacks the Chromium depot_tools. It doesn't matter where you store these, but you should use a folder that doesn't contain any spaces so please edit the folder if the default location isn't suitable.

3. ```Source code parent folder```

This is a folder on your machine that will receive the V8 source code. It should be an empty folder and it must exist before you try to use the rest of the program to set up and build V8. You can reuse an existing distribution if you want to, but please be aware that V8Builder thinks it owns the contents of this folder and will happily decide to delete everything in it, if that's appropriate for the buttons you push!

There are several buttons on the tab:

1. ```DOWNLOAD TOOLS```

This is the very first button you should click. V8Builder will download the ```depot_tools``` from the "Download depot tools" URL and unpack them in the "Save depot_tools" folder. A progress bar is displayed along with a status bar message. It typically takes less than a minute.

2. ```UPDATE TOOLS```

This runs Google's ```gclient``` script in a Command prompt to configure the ```depot_tools``` that you downloaded above. The first time that you run it will take several minutes and there is no visible feedback in the Command Prompt. You should not need to click it again, unless you delete the V8Builder cache folder or you believe that Google may have published a new version of ```depot_tools``` since you last updated them.

3. ```DOWNLOAD SOURCE```

Click this button to download the V8 source code to the ```Source code parent folder```. Beware that V8Builder will delete any existing files and subfolders in that folder (so beware!). V8Builder opens a Command widow and runs the ```fetch v8``` script. This takes a while, depending on the speed of your connection to their Git server.

3. ```UPDATE SOURCE```

Click this button to get the latest version of the V8 source from Google. V8Builder will un the ```gclient sync``` script in a Command Window. If you have already built your V8 environment using the other buttons on a previous run of V8Builder, this is typically the only button you will ever click again.

*I'm not certain if you need to run it after you do the initial download, but it doesn't take very long, so you may choose to do it anyway.*

When you've done all that, it's time to move on to the Build tab.

### Build tab

This is the tab where you get to set up your building configurations (CPU type, output format - libs or dlls etc.) and start the V8 build itself.

At the top-left of this tab is a list box entitles ```Available options```. This list box displays all of the options that are supported by the ```depot_tools``` script called ```GN```. If the list box is empty, click the ```UPDATE OPTIONS``` button, which will open a Command Window to query GN for a list of its supported arguments. When the Command Window is finished, close it in the usual way for V8Builder scripts.

If you select an option in the ```Available Options``` list-box, the default value of that option, along with a short description is displayed to the right-hand side of the tab. Note that the default value and the description are provided by Google and not by V8Builder (which merely displays them for you).

Below the ```Available Options``` list box, is another list box that shows all of the available build configurations. If this list box is empty, make sure that you have completed all of the steps from the ```Setup``` tab. The build configurations are retrieved as a side-effect of those steps. Select the configuration that you want to build, e.g. ```ia32.optdebug``` or ```x64.release```.

To change the value of an option for the build, move it into the second list box ```Selected options```, then change the value in the text box that is below the ```BUILD V8``` button. You can move options between the two list boxes either by double-clicking them or by selecting them and clicking the ```>>``` or ```<<``` buttons, as appropriate.

If you select an option by clicking on it and then find that you are unable to change the value (i.e. the text box appears to be in read-only mode), then make sure that the option you are trying to edit is displayed in the ```Selected options``` list box and that it is currently selected (highlighted) in that list box. You cannot edit the value of the values in the ```Available options``` list box.

As you change the value in the text box, notice that the configure command that is displayed in the bottom-right window updates automatically in real-time. If you don't see a command line and, instead, see a message like this:

```
Select a configuration from the list box on the left (click the UPDATE OPTIONS button, if the list box is empty)
```

then you need to choose a build configuration from the list-box to the left of this window. The configure command window should look something like this:

```
C:\Users\Dan\AppData\Local\Gamaliel\V8Builder\depot_tools\win_tools-2_7_6_bin\python\bin\python.exe tools\dev\v8gen.py gen -b ia32.optdebug "ia32.optdebug" --no-goma -- is_clang=false
```

If you want to revert to using the default setting, move the option from the ```Selected option``` list-box back into the ```Available options``` list box. Notice how the option is removed from the configure command in the bottom-right.

When you are happy with the configuration options, click the ```CONFIGURE``` button. A Command Window will open and set up a V8 build configuration in a sub-folder of ```v8\out.gn``` that has the same name as the build configuration you selected. Close that window and you are ready to Build V8!

Click the final button called ```BUILD V8```. V8Builder will open another Command Window and set off a build of V8 using ```ninja```. This takes several minutes, even on a very fast machine. I suggest you turn off the -auto sleep on your PC and go take your dogs for a nice walk or something.

When that Command Window returns to the prompt, assuming that there were no errors, you should have a fully built version of V8 on your machine!

### Some important options

Below are listed some of the options that are relevant to building under Windows using Visual Studio (which is what I need to do). Other environments may need different combinations and you will need to research those yourself, I'm afraid.

1. is_clang

By default, ```ninja``` builds using the clang compiler, as this option is set to __true__ by default. If you want to build libraries for use in Visual Studio, select this option and change it to be __false__.

2. treat_warnings_as_errors

The Vsiual Studio builds of V8 produce a lot of warnings. By default, these will cause ```ninja``` to fail the build. You should set this option to __false__ (note that there is no default value for it).

3. fatal_linker_warnings

I'm not sure whether this is also a requirement to be able to build V8 under Windows, but I like to run builds with it set to __false__, just because it makes me feel a bit more comfortable when leaving ninja alone for long periods of time. My dogs walks are long and slow!

### License

V8Builder uses a number of third-party toolkits. Each of these is licensed separatedly and the licenses are available in the [LICENSE](https://github.com/dannypike/V8Builder/blob/master/LICENSE) file.


#

*"V8", "Chromium", "depot_tools" are products of Google Inc. Windows and Visual Studio are products of Microsoft Corporation and all of their respective copyrights are acknowledged. This document is copyright (c) 2018 Dan Pike (danny.pike gmail.com)*
