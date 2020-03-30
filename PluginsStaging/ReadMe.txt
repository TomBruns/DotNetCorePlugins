Notes:
=====================================================================
This folder is a staging area for the plugins during dev/testing that will be loaded by the Scheduling Service
All of the folders & files here are copied to a subfolder of the Scheduling Service in a post build step

In the real world all of the appropriate folders and assys would be copied there are deployment time

In this POC, run these commands (inthis order) in each plug-in project folder
1. dotnet publish --runtime win-x64 --self-contained true

2. dotnet build -target:CopyToStaging
