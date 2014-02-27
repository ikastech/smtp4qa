smtp4qa
=======
smtp4qa provides a option to store the email file (.eml) to the folder which is configurable.
Select Rnwood.Smtp4dev prject and Config the App.config- appsettings:
 
    <add key="EmailFolderPath" value="ADD YOUR DIRECTORY"/>
    <add key="EmailShowInGrid" value="1"/><!-- 1-Email Shown in grid, 0-Email doen't shown in grid-->
 

The saved email are can view in the web page by setting up the default.aspx folder path.

Config in default page  
static string EmailFolderPath = @"ADD YOUR DIRECTORY";<!--Add EmailFolderPath value which provide in the app.config-->

The saved .eml files can be viewed from browser without downloading .eml file, easy for QA team to check the emails without sending.There is option to filter the email by date.
