# mpv.net

mpv.net is a libmpv based media player for Windows, it looks and works like mpv and also shares the same settings as mpv and therefore the mpv documentation applies.

mpv and mpv.net have a learning curve.

mpv manual: <https://mpv.io/manual/master/>

Table of contents
-----------------

- [Features](#features)
- [Screenshots](#screenshots)
- [Context Menu](#context-menu)
- [Settings](#settings)
- [Scripting](#scripting)
- [Support](#support)
- [Changelog](#changelog)

### Features

- Customizable context menu defined in the same file as the key bindings
- Searchable options dialog with modern UI as mpv compatible standalone application
- Searchable input (key/mouse) binding editor with modern UI as mpv compatible standalone application
- Rich addon API for .NET languages
- Rich scripting API for Python, C#, Lua, JavaScript and PowerShell
- mpv's OSC (on screen controller (play control bar)), IPC, conf files

### Screenshots

![](https://raw.githubusercontent.com/stax76/mpv.net/master/screenshots/mpvnet.png)

![](https://raw.githubusercontent.com/stax76/mpv.net/master/screenshots/mpvnetContextMenu.png)

![](https://raw.githubusercontent.com/stax76/mpv.net/master/screenshots/mpvConfEdit.png)

![](https://raw.githubusercontent.com/stax76/mpv.net/master/screenshots/mpvInputEdit.png)

### Context Menu

The context menu can be customized via input.conf file located at:
```
C:\Users\username\AppData\Roaming\mpv\input.conf
```
if it's missing mpv.net generates it with the following defaults:

<https://github.com/stax76/mpv.net/blob/master/mpv.net/Resources/input.conf.txt>

### Settings

mpv.net shares the settings with mpv, settings can be edited in a settings dialog or in a config file called mpv.conf located at:
```
C:\Users\user\AppData\Roaming\mpv\mpv.conf
```
if it's missing mpv.net generates it with the following defaults:

<https://github.com/stax76/mpv.net/blob/master/mpv.net/Resources/mpv.conf.txt>

### Scripting

Scripting is supported via Python, C#, Lua, JavaScript and PowerShell

https://github.com/stax76/mpv.net/wiki/Scripting-(CSharp,-Python,-JavaScript,-Lua,-PowerShell)

### Support

<https://forum.doom9.org/showthread.php?t=174841>

<https://forum.videohelp.com/threads/392514-mpv-net-a-extendable-media-player-for-windows>

<https://github.com/stax76/mpv.net/issues>

### Changelog

### 2.9 (2019-04-16)

- clicking the right top corner in fullscreen mode
  closes the player but it did not work on all displays
- the info display was changed to display the filename on top
  so it's not diplayed in the middle of the screen
- on start up of the conf editor all text is now selected in the
  search text box so it's ready for a new search to be typed
- the conf editor was changed to write the settings to disk
  only if the settings were actually modified, also the message
  that says that the settings will be available on next start
  is now only shown if the settings were actually modified.
- there was an instance in the context menu where the sub menu
  arrow was overlapping with the text
- in the input editor when only one character is entered in the
  search text box the search is performed only in the input and
  not in the command or menu
- in the input editor the routine that generates the input string
  was completely rewritten because it was adding Shift where it
  wasn't necessary (it took a huge amount of time to implement)
- the context menu has a new track menu where the active track
  can be seen and selected, it shows video, audio and subtitle
  tracks with various meta data. [Menu default definition](https://github.com/stax76/mpv.net/blob/master/mpv.net/Resources/input.conf.txt#L104).

[go to download page](https://github.com/stax76/mpv.net/releases)

### 2.8 (2019-04-12)

- Win 7 dark-mode render issue fix

### 2.7 (2019-04-12)

- the autofit mpv property was added to the conf editor
- the routine that writes the mpv.conf file in the conf editor was completely rewritten
- the conf editor has a dedicated page for mpv.net specific settings,
  these settings are saved in the same folder as mpv.conf using mpvnet.conf as filename,
  the first setting there is dark-mode
- new optional dark theme 

### 2.6 (2019-04-09)

- on Win 7 controls in the conf editor were using a difficult too read too light color
- context menu renderer changed to look like Win 10 design, except colors are still system theme colors

### 2.5 (2019-04-08)

- in case the input conf don't contain a menu definition mpv.net creates the default menu instead no menu like before
- all message boxes were migrated to use the TaskDialog API
- an improvement in the previous release unfortunately introduced a bug
  causing the conf editor not to save settings