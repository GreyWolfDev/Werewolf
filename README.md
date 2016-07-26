# Werewolf for Telegram

This is the main repo for Werewolf for Telegram.

For language file updates, please submit the xml file on Telegram to the [support chat](http://telegram.me/werewolfsupport) and ask for assistance

## Requirements
* .NET Framework 4.5.2
* SQL Server (I am using 2014)
* Windows Server

## Setup

To set up werewolf on a private server, follow these steps:

1. Go to [BotFather](https://telegram.me/BotFather) and create a new bot.  Answer all of the questions it asks, and you will receive an API Token.
   * On your server, open regedit, and go to `HKLM\SOFTWARE\`, create a new Key named `Werewolf`
   * In the new key create a new string value named `ProductionAPI`.  
   * Paste you API token here.
2. Grab the Werewolf Database.sql file from this repository
   * Open the file in notepad, notepad++, whatever you use
   * Double check the path at the top of the file - update it if you are using a different SQL version
   * Run the sql script.  This will create the `werewolf` database and all the tables / views / stored procs to go with it
   * If you already have some admins (including yourself), add their TelegramID's to the `dbo.Admin` table
3. Now it's time to compile the source code
   * In the Database project, you will need to create an Internal.settings file
      * Create a string setting named `DBConnectionString`, Application Scope, and set the Value to your SQL connection string for the database you created in step 2
      * .gitignore has marked this file, so it won't be committed. **However, when you create the setting, VS will copy it to the app.config - make sure to remove it if you plan on committing back to your fork**
   * In Visual Studio, open the solution.  Make sure you are set to `RELEASE` build.  You may want to go into `Werewolf_Control.Handlers.UpdateHandler.cs` and change `internal static int Para = 129046388;` to match your id.  Also, double check the settings.cs files in both Control and Node.
   * Build the solution
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