# SRWE
Simple Runtime Window Editor (SRWE) - a program that allows you to pick a running
application and manipulate size, position, styles of its main/child windows.
SRWE was built to maintain games that run in Windowed-mode. For example,
you can get the fullscreen-mode effect on a windowed-mode game or get fullscreen effect
with visible taskbar.
Since SRWE allows you to manually set up any window size or position, it can be useful
in taking high-resolution screenshots in games that support windowed-mode.

## Hotsampling games for screenshots

To take screenshots from games it's often better to run the game on a high resolution. This will likely make the game run 
slower than normal, but for taking screenshots this is OK. To set the game to a (much) higher resolution, SRWE can be used. 
This is called *hotsampling*. Not all games support this feature, however: the game has to resize its *viewport* (the area of
the window which displays the game's graphics) when the game's window (the frame/border around the viewport) is being resized. A lot of 
games simply stretch the viewport or create black borders around it. However some games do resize the viewport and for these games
you can use SRWE to resize them when you want to take screenshots. 

To test whether a game supports hotsampling, run the game in windowed mode so you'll have a border around the viewport. Now drag 
with the mouse the border to a direction to increase the window size. When you release the left mouse button, the viewport of the game
should resize. If the viewport adapts to the new window size, the game can be hotsampled. Games like Skyrim SE, all frostbyte games, Rise
of the Tomb Raider support hotsampling. 

### How to hotsample a game
To hotsample a game, it's key to run the game in windowed mode. To do this you have to select this mode in the game's graphics options.
Additionally, you have to run the game as administrator. Now start the game, press alt-tab and as administrator, also start SRWE. In 
SRWE, click _Select running Application_ from the toolbar. The window that pops up should enlist the game's .exe process. Select 
that process from the list and click _Open_. 

SRWE is now attached to the game window, and you can now manipulate the window. SRWE will show you all kinds of characteristics of the 
game's window, like its size, position, but also flags which Windows uses to define how a window looks. These are available on 
the 'Windows Styles' tab. For instance, you can change whether the window has a title bar or min/max buttons by checking/unchecking 
the checkboxes of `WS_SYSMENU` and `WS_DLGFRAME`. For games these aren't that important. 

To set the game's window to a high resolution, just type the resolution you want in the _Width_ and _Height_ text boxes. SRWE will update
the window immediately. For most games this is enough to set the game window to a higher resolution. If you want to get rid of the 
borders around the window, just click the _Remove borders_ button. 

#### EXITSIZEMOVE

Some games don't seem to work with SRWE, even though they passed the manual resize test. This is because their internal windows handling 
code waits for a sign from Windows that the user has finished resizing the window. This sign usually comes when you release the left-mouse
button after manually resizing the windows. These signs are called _messages_. With SRWE we basically trick the window it is attached to
that a user has resized the window manually by sending it these messages, like a size message that the window size has changed. For some
games, this is enough. For other games this isn't enough, they deliberately wait for the 'I'm done!' message. This particularly message is
called `WM_EXITSIZEMOVE`, and you can tell SRWE to send this message as well, by checking the _Force EXITSIZEMOVE after window resize_ 
checkbox. 

Some games, like Dragon Age:Inquisition, it's required to check this checkbox. For others, like Rise of the Tomb Raider, 
checking this checkbox actually causes resizing the window through SRWE to be stretching. It's therefore a trial/error process whether
to check this checkbox or not. But it's easy to find out (just try whether checking the checkbox helps solve it or not), and SRWE will remember 
the state of the checkbox. 

Remember, not all games support hotsampling. SRWE isn't a piece of magic that can enable functionality in a game that's not implemented by its developers:
it simply mimics a user's resize actions on a windowed game. If the game itself doesn't anticipate on resize actions (most games ignore any resize action
on the window, sadly) then SRWE can't add that functionality, only the game developers can.

## Profiles
It's of course tedious to type in a resolution each time, clicking borders off/on, etc. To solve that, SRWE allows you to save the
current state of SRWE as a _Profile_. To do this, simply click _Save Profile_. Any saved profile will be added to the list of 
_Recent profiles_, so you can quickly pick a profile from that list instead of typing the resolution in again. To get started, 
you can choose any of [the profiles in the SRWE repository](https://github.com/dtgDTGdtg/SRWE/tree/master/Profiles), and load these with
the _Load Profile_ button after you've downloaded them and stored them in a folder. Download the profiles from the 
[Releases](https://github.com/dtgDTGdtg/SRWE/releases) tab on GitHub in the `SRWE-Example-Profiles.zip` file. They're just examples: if you want to have different
resolutions, just load one of them, alter the resolution and save it under a different name. 

