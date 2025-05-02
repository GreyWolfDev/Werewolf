# Werewolf for Telegram

This is the main repo for Werewolf for Telegram.

For language file updates, please submit the xml file on Telegram to the [support chat](http://telegram.me/werewolfsupport) and ask for assistance

### Visual Studio Team Services Continuous Integration		
![build status](https://parabola949.visualstudio.com/_apis/public/build/definitions/c0505bb4-b972-452b-88be-acdc00501797/2/badge)

## Requirements
* .NET Framework 4.5.2
* SQL Server (I am using 2014) / SQL Server 2016
* Windows Server

## Setup

To set up werewolf on a private server, follow these steps:

1. Go to [BotFather](https://telegram.me/BotFather) and create a new bot.  Answer all of the questions it asks, and you will receive an API Token.
   * On your server, open regedit, and go to `HKLM\SOFTWARE\`, create a new Key named `Werewolf` (HKLM - HKEY_LOCAL_MACHINE)
   * In the new key create a new string value named `ProductionAPI`.  
   * Paste your API token here.
2. Grab the werewolf.sql file from this repository
   * Open the file in notepad, notepad++, whatever you use
   * Double check the path at the top of the file - update it if you are using a different SQL version
   * Run the sql script.  This will create the `werewolf` database and all the tables / views / stored procs to go with it
   * If you already have some admins (including yourself), add their TelegramID's to the `dbo.Admin` table
		* In order to obtain your ID, headover to your bot in telegram and /Start. After that, toss a random text to it. Enter this URL to your browser (https://api.telegram.org/botYOURTELEGRAMBOTAPIKEY/getUpdates)
3. Now it's time to compile the source code
   * On your server, open regedit
   * In the `Werewolf` key create a new string value named `BotConnectionString`.
   * Paste the Connection String here.
        * Connection String should be this (change the values) `metadata=res://*/WerewolfModel.csdl|res://*/WerewolfModel.ssdl|res://*/WerewolfModel.msl;provider=System.Data.SqlClient;provider connection string="data source=SERVERADDRESS;initial catalog=werewolf;user id=USERNAME;password=PASSWORD;MultipleActiveResultSets=True;App=EntityFramework"`
			* If you are using Windows Authentication for your MSSQL Server, do take note that the password property will NO Longer be required. You're required to replace it(both user id and password) with "Trusted_Connection=True;" instead.
      * .gitignore has marked this file, so it won't be committed. **However, when you create the setting, VS will copy it to the app.config - make sure to remove it if you plan on committing back to your fork**
   * Create another new string value named BotanReleaseAPI. You can leave this blank if you don't want to track your usage using BotanIO.
   * If you plan on running another instance of the bot as beta, add another two new string values named BotanBetaAPI and BetaAPI. Again, you can leave BotanBetaAPI empty if you want. Set BetaAPI to the token of your beta bot.
   * In Visual Studio, open the solution.  Make sure you are set to `RELEASE` build.  You may want to go into `Werewolf_Control.Helpers.UpdateHelper.cs` and add your id to `internal static int[] Devs = { ... }`.  Also, double check the `Settings.cs` files in both `Werewolf Control/Helpers` and `Werewolf Node/Helpers`.
   * Build the solution
4. Server directories
   * Pick any directory for your root directory

   | Directory | Contents |
   |-----------|---------:|
   |`root\Instance Name\Control`|Control build|
   |`root\Instance Name\Node 1`|Node build|
   |`root\Instance Name\Node <#>`|Node updates can be added to a new Node folder.  Running `/replacenodes` in Telegram will tell the bot to automatically find the newest node (by build time) and run it|
   |`root\Instance Name\Logs`|Logging directory|
   |`root\Languages`|Language xml files - These files are shared by all instances of Werewolf|

   * Note - Once all nodes are running the newest version (Node 2 directory), the next time you update nodes, you can put the new files in Node 1 and `/replacenodes`.  Again, the bot will always take whichever node it finds that is the newest, as long as the directory has `Node` in the name.  **do not name any other directory in the root folder anything with `Node` in it**
5. Fire up the bot!
6. If you try to start a game now, you will notice that the bot will just respond with an error. That is because you didn't update the gif ids yet. See the section below for instructions on how to do this.


## GIF SUPPORT
In order to use GIFs with the bot, you will need to "teach" the bot the new GIF IDs.  From Telegram, run `/learngif`, the bot will respond with `GIF learning = true`.  Now send it a GIF, and the bot will reply with an ID.  Send the bot all the GIFs you need.  In the Node project, go to Helpers > Settings.cs and find the GIF lists.  You'll need to remove all of the existing IDs and put in the IDs you just got from the bot.

You can test these by running `/dumpgifs` (preferably in Private Message!).  Make sure you check out DevCommands.cs, and look at the `DumpGifs()` method - most of it is commented out.  Uncomment what you need.
