smtp4qa
=======

Smtp4qaService provides a option to store the email file (.eml) to the folder which is configurable.
Select Smtp4qaServices project and Config the App.config- appsettings:
 
    <add key="EmailFolderPath" value="ADD YOUR DIRECTORY"/>
    
Install the Smtp4qaServices to your machine by following steps:

1. Open CMD as administrator.Enter the following command 
2. "CD C:\WINDOWS\Microsoft.NET\Framework\v2.0.50727\"
3. To install "InstallUtil.exe C:\Smtp4qaServices\Smtp4qaServices.exe" <!-- your drive path and  Smtp4qaServices.exe-->


 

Now saved email are can view in the web page by setting up the Web.Config for folder path.

    <add key="EmailFolderPath" value="ADD YOUR DIRECTORY"/> 
 
 <!--Add EmailFolderPath value which provide in the app.config-->

The saved .eml files can be viewed from browser without downloading .eml file, easy for QA team to check the emails without sending.There is option to filter the email by date.


To uninstall the services excute this line "InstallUtil.exe /u C:\Smtp4qaServices\Smtp4qaServices.exe"


