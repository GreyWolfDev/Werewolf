# Werewolf for Telegram

This is the main repo for Werewolf for Telegram.

For language file updates, please submit the xml file on Telegram to the [support chat](http://telegram.me/werewolfsupport) and ask for assistance

## Requirements
* .NET Framework 4.5.2
* SQL Server (I am using 2014) / SQL Server 2016
* Windows Server
* Visual Studio 2015 Community Edition / Enterprise

## Setup

To set up werewolf on a private server, follow these steps:

1. Go to [BotFather](https://telegram.me/BotFather) and create a new bot.  Answer all of the questions it asks, and you will receive an API Token.
   * On your server, open regedit, and go to `HKLM\SOFTWARE\`, create a new Key named `Werewolf` (HKLM - HKEY_LOCAL_MACHINE)
   * In the new key create a new string value named `ProductionAPI`.  
   * Paste your API token here.
2. Grab the Werewolf Database.sql file from this repository
   * Open the file in notepad, notepad++, whatever you use
   * Double check the path at the top of the file - update it if you are using a different SQL version
   * Run the sql script.  This will create the `werewolf` database and all the tables / views / stored procs to go with it
   * If you already have some admins (including yourself), add their TelegramID's to the `dbo.Admin` table 
		* In order to obtain your ID, headover to your bot in telegram and /Start. After that, toss a random text to it. Enter this URL to your browser (https://api.telegram.org/botYOURTELEGRAMBOTAPIKEY/getUpdates)
3. Now it's time to compile the source code **DO NOT OPEN OTHER FILES NOT STATED HERE**
   * In the Database project, you will need to create an Internal.settings file
	  * **In order to create the Internal.settings file, right click on Database -> Add -> New Item**
	  * Under Visual C# Items -> Settings File and name it as stated. 
      * Create a string setting named `DBConnectionString`, Application Scope, and set the Value to your SQL connection string for the database you created in step 2
         * Connection String should be this (change the values) `metadata=res://*/WerewolfModel.csdl|res://*/WerewolfModel.ssdl|res://*/WerewolfModel.msl;provider=System.Data.SqlClient;provider connection string="data source=SERVERADDRESS;initial catalog=werewolf;user id=USERNAME;password=PASSWORD;MultipleActiveResultSets=True;App=EntityFramework"`
			* **If you are using Windows Authentication for your MSSQL Server, do take note that the password property will NO Longer be required. You're required to replace it with "Trusted_Connection=True;" instead.**
      * .gitignore has marked this file, so it won't be committed. **However, when you create the setting, VS will copy it to the app.config - make sure to remove it if you plan on committing back to your fork**
   * In Visual Studio, open the solution.  Make sure you are set to `RELEASE` build.  You may want to go into `Werewolf_Control.Handlers.UpdateHandler.cs` and change `internal static int Para = 129046388;` to match your id.  Also, double check the settings.cs files in both Control and Node.
   * Build the solution
   
### Issues in Step 3.
	  * A common issue with the package Net.Http.Formatting package not being referenced properly by NuGet has been found. Resolve it by looking at the top menu of VS, clicking on Tools -> NuGet Package Manager -> Package Manager Console.
		* **Do "Install-Package System.Net.Http.Formatting.Extension" for the projects, Werewolf_Control & Werewolf_Node. THIS IS IMPORTANT**
		* Alternatively, you can manually add the package "DRIVE:\Users\YOURUSERNAME\.dnx\packages\Microsoft.AspNet.WebApi.Client\5.2.3\lib\net45"
4. Server directories
   * Pick any directory for your root directory

   | Directory | Contents |
   |-----------|---------:|
   |`root\Control`|Control build|
   |`root\Node 1`|Node build|
   |`root\Node <#>`|Node updates can be added to a new Node folder.  Running `/replacenodes` in Telegram will tell the bot to automatically find the newest node (by build time) and run it|
   |`root\Logs`|Node crash directory|
   |`root\Languages`|Language xml files|
   
   * Note - Once all nodes are running the newest version (Node 2 directory), the next time you update nodes, you can put the new files in Node 1 and `/replacenodes`.  Again, the bot will always take whichever node it finds that is the newest, as long as the directory has `Node` in the name.  **do not name any other directory in the root folder anything with `Node` in it**
5. Fire up the bot!
