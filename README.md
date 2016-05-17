# Bachelor Thesis - Consensus of History in Distributed DCR Graphs

## Installing
The client that contacts the server and events that are hosted on Microsoft Azure is installed using the installer 'setup.exe'. 
The installer creates a shortcut on the desktop that will launch the program. 

In order to log in to the client, the login "admin" and password "bachelor" should be used.
By clicking "History", a window where the user can produce and view produced history will appear. 

The triggers in this window enable and disable the different steps of the production of an order of execution described in the report. 

Note that because Azure hibernates VMs that are not in use, a delay might occur before the events respond with history on the first run.

The program has been tested on Windows 10, and *should* run without issues on Windows 8, 8.1 and 7. 

## Source
If the source code for the implementation is opened and run in the "Debug" configuration of Visual Studio, the services have been set up to run on the local machine instead of on Microsoft Azure. 
