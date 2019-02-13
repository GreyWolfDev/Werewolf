
-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, 2012 and Azure
-- --------------------------------------------------
-- Date Created: 02/11/2019 16:03:21
-- Generated from EDMX file: C:\Users\flmeyer\Source\Repos\Olfi01\Werewolf\Werewolf for Telegram\Database\WerewolfModel.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
USE [werewolf];
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[dbo].[FK_dbo_AspNetUserClaims_dbo_AspNetUsers_UserId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[AspNetUserClaims] DROP CONSTRAINT [FK_dbo_AspNetUserClaims_dbo_AspNetUsers_UserId];
GO
IF OBJECT_ID(N'[dbo].[FK_dbo_AspNetUserLogins_dbo_AspNetUsers_UserId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[AspNetUserLogins] DROP CONSTRAINT [FK_dbo_AspNetUserLogins_dbo_AspNetUsers_UserId];
GO
IF OBJECT_ID(N'[dbo].[FK_dbo_AspNetUserRoles_dbo_AspNetRoles_RoleId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[AspNetUserRoles] DROP CONSTRAINT [FK_dbo_AspNetUserRoles_dbo_AspNetRoles_RoleId];
GO
IF OBJECT_ID(N'[dbo].[FK_dbo_AspNetUserRoles_dbo_AspNetUsers_UserId]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[AspNetUserRoles] DROP CONSTRAINT [FK_dbo_AspNetUserRoles_dbo_AspNetUsers_UserId];
GO
IF OBJECT_ID(N'[dbo].[FK_Game_Group]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[Game] DROP CONSTRAINT [FK_Game_Group];
GO
IF OBJECT_ID(N'[dbo].[FK_GameKill_Game]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[GameKill] DROP CONSTRAINT [FK_GameKill_Game];
GO
IF OBJECT_ID(N'[dbo].[FK_GameKill_Killer]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[GameKill] DROP CONSTRAINT [FK_GameKill_Killer];
GO
IF OBJECT_ID(N'[dbo].[FK_GameKill_KillMethod]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[GameKill] DROP CONSTRAINT [FK_GameKill_KillMethod];
GO
IF OBJECT_ID(N'[dbo].[FK_GamePlayer_Game]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[GamePlayer] DROP CONSTRAINT [FK_GamePlayer_Game];
GO
IF OBJECT_ID(N'[dbo].[FK_GamePlayer_Player]', 'F') IS NOT NULL
    ALTER TABLE [dbo].[GamePlayer] DROP CONSTRAINT [FK_GamePlayer_Player];
GO
IF OBJECT_ID(N'[werewolfModelStoreContainer].[FK_GroupAdmin_Group]', 'F') IS NOT NULL
    ALTER TABLE [werewolfModelStoreContainer].[GroupAdmin] DROP CONSTRAINT [FK_GroupAdmin_Group];
GO
IF OBJECT_ID(N'[werewolfModelStoreContainer].[FK_GroupAdmin_Player]', 'F') IS NOT NULL
    ALTER TABLE [werewolfModelStoreContainer].[GroupAdmin] DROP CONSTRAINT [FK_GroupAdmin_Player];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[db_owner].[BotStatus]', 'U') IS NOT NULL
    DROP TABLE [db_owner].[BotStatus];
GO
IF OBJECT_ID(N'[db_owner].[ContestTerms]', 'U') IS NOT NULL
    DROP TABLE [db_owner].[ContestTerms];
GO
IF OBJECT_ID(N'[db_owner].[GlobalBan]', 'U') IS NOT NULL
    DROP TABLE [db_owner].[GlobalBan];
GO
IF OBJECT_ID(N'[dbo].[__MigrationHistory]', 'U') IS NOT NULL
    DROP TABLE [dbo].[__MigrationHistory];
GO
IF OBJECT_ID(N'[dbo].[Admin]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Admin];
GO
IF OBJECT_ID(N'[dbo].[AspNetRoles]', 'U') IS NOT NULL
    DROP TABLE [dbo].[AspNetRoles];
GO
IF OBJECT_ID(N'[dbo].[AspNetUserClaims]', 'U') IS NOT NULL
    DROP TABLE [dbo].[AspNetUserClaims];
GO
IF OBJECT_ID(N'[dbo].[AspNetUserLogins]', 'U') IS NOT NULL
    DROP TABLE [dbo].[AspNetUserLogins];
GO
IF OBJECT_ID(N'[dbo].[AspNetUserRoles]', 'U') IS NOT NULL
    DROP TABLE [dbo].[AspNetUserRoles];
GO
IF OBJECT_ID(N'[dbo].[AspNetUsers]', 'U') IS NOT NULL
    DROP TABLE [dbo].[AspNetUsers];
GO
IF OBJECT_ID(N'[dbo].[DailyCount]', 'U') IS NOT NULL
    DROP TABLE [dbo].[DailyCount];
GO
IF OBJECT_ID(N'[dbo].[Game]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Game];
GO
IF OBJECT_ID(N'[dbo].[GameKill]', 'U') IS NOT NULL
    DROP TABLE [dbo].[GameKill];
GO
IF OBJECT_ID(N'[dbo].[GamePlayer]', 'U') IS NOT NULL
    DROP TABLE [dbo].[GamePlayer];
GO
IF OBJECT_ID(N'[dbo].[GlobalStats]', 'U') IS NOT NULL
    DROP TABLE [dbo].[GlobalStats];
GO
IF OBJECT_ID(N'[dbo].[Group]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Group];
GO
IF OBJECT_ID(N'[dbo].[GroupRanking]', 'U') IS NOT NULL
    DROP TABLE [dbo].[GroupRanking];
GO
IF OBJECT_ID(N'[dbo].[GroupStats]', 'U') IS NOT NULL
    DROP TABLE [dbo].[GroupStats];
GO
IF OBJECT_ID(N'[dbo].[KillMethod]', 'U') IS NOT NULL
    DROP TABLE [dbo].[KillMethod];
GO
IF OBJECT_ID(N'[dbo].[Language]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Language];
GO
IF OBJECT_ID(N'[dbo].[Player]', 'U') IS NOT NULL
    DROP TABLE [dbo].[Player];
GO
IF OBJECT_ID(N'[dbo].[PlayerStats]', 'U') IS NOT NULL
    DROP TABLE [dbo].[PlayerStats];
GO
IF OBJECT_ID(N'[dbo].[RefreshDate]', 'U') IS NOT NULL
    DROP TABLE [dbo].[RefreshDate];
GO
IF OBJECT_ID(N'[werewolfModelStoreContainer].[GroupAdmin]', 'U') IS NOT NULL
    DROP TABLE [werewolfModelStoreContainer].[GroupAdmin];
GO
IF OBJECT_ID(N'[werewolfModelStoreContainer].[NotifyGame]', 'U') IS NOT NULL
    DROP TABLE [werewolfModelStoreContainer].[NotifyGame];
GO
IF OBJECT_ID(N'[werewolfModelStoreContainer].[v_InactivePlayersMain]', 'U') IS NOT NULL
    DROP TABLE [werewolfModelStoreContainer].[v_InactivePlayersMain];
GO
IF OBJECT_ID(N'[werewolfModelStoreContainer].[v_NonDefaultGroups]', 'U') IS NOT NULL
    DROP TABLE [werewolfModelStoreContainer].[v_NonDefaultGroups];
GO
IF OBJECT_ID(N'[werewolfModelStoreContainer].[v_PreferredGroups]', 'U') IS NOT NULL
    DROP TABLE [werewolfModelStoreContainer].[v_PreferredGroups];
GO
IF OBJECT_ID(N'[werewolfModelStoreContainer].[v_PublicGroups]', 'U') IS NOT NULL
    DROP TABLE [werewolfModelStoreContainer].[v_PublicGroups];
GO
IF OBJECT_ID(N'[werewolfModelStoreContainer].[v_WaitList]', 'U') IS NOT NULL
    DROP TABLE [werewolfModelStoreContainer].[v_WaitList];
GO
IF OBJECT_ID(N'[werewolfModelStoreContainer].[v_GroupRanking]', 'U') IS NOT NULL
    DROP TABLE [werewolfModelStoreContainer].[v_GroupRanking];
GO
IF OBJECT_ID(N'[werewolfModelStoreContainer].[v_IdleKill24HoursMain]', 'U') IS NOT NULL
    DROP TABLE [werewolfModelStoreContainer].[v_IdleKill24HoursMain];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'Admins'
CREATE TABLE [dbo].[Admins] (
    [UserId] int  NOT NULL
);
GO

-- Creating table 'Games'
CREATE TABLE [dbo].[Games] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [GroupName] nvarchar(max)  NOT NULL,
    [GroupId] bigint  NOT NULL,
    [TimeStarted] datetime  NULL,
    [TimeEnded] datetime  NULL,
    [Winner] nvarchar(50)  NULL,
    [GrpId] int  NULL,
    [Mode] nvarchar(50)  NULL,
    [Beta] bit  NULL
);
GO

-- Creating table 'GameKills'
CREATE TABLE [dbo].[GameKills] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [GameId] int  NOT NULL,
    [KillerId] int  NOT NULL,
    [VictimId] int  NOT NULL,
    [TimeStamp] datetime  NOT NULL,
    [KillMethodId] int  NOT NULL,
    [Day] int  NOT NULL
);
GO

-- Creating table 'GamePlayers'
CREATE TABLE [dbo].[GamePlayers] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [PlayerId] int  NOT NULL,
    [GameId] int  NOT NULL,
    [Survived] bit  NOT NULL,
    [Won] bit  NOT NULL,
    [Role] nvarchar(50)  NULL
);
GO

-- Creating table 'Groups'
CREATE TABLE [dbo].[Groups] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NOT NULL,
    [GroupId] bigint  NOT NULL,
    [Preferred] bit  NULL,
    [Language] nvarchar(max)  NULL,
    [DisableNotification] bit  NULL,
    [UserName] nvarchar(max)  NULL,
    [BotInGroup] bit  NULL,
    [ShowRoles] bit  NULL,
    [Mode] nvarchar(50)  NULL,
    [DayTime] int  NULL,
    [NightTime] int  NULL,
    [LynchTime] int  NULL,
    [AllowTanner] bit  NULL,
    [AllowFool] bit  NULL,
    [AllowCult] bit  NULL,
    [ShowRolesEnd] nvarchar(50)  NULL,
    [MaxPlayers] int  NULL,
    [DisableFlee] bit  NULL,
    [CreatedBy] nvarchar(max)  NULL,
    [ImageFile] nvarchar(max)  NULL,
    [Description] nvarchar(max)  NULL,
    [GroupLink] nvarchar(max)  NULL,
    [MemberCount] int  NULL,
    [AllowExtend] bit  NULL,
    [MaxExtend] int  NULL,
    [EnableSecretLynch] bit  NULL,
    [Flags] bigint  NULL,
    [BetaGroup] bit  NULL,
    [RoleFlags] bigint  NULL
);
GO

-- Creating table 'KillMethods'
CREATE TABLE [dbo].[KillMethods] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(50)  NOT NULL
);
GO

-- Creating table 'Players'
CREATE TABLE [dbo].[Players] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [TelegramId] int  NOT NULL,
    [Name] nvarchar(max)  NOT NULL,
    [UserName] nvarchar(max)  NULL,
    [Banned] bit  NULL,
    [BannedBy] nvarchar(max)  NULL,
    [HasPM] bit  NULL,
    [BanReason] nvarchar(max)  NULL,
    [ImageFile] nvarchar(max)  NULL,
    [Language] nvarchar(max)  NULL,
    [TempBanCount] int  NULL,
    [HasPM2] bit  NULL,
    [HasDebugPM] bit  NULL,
    [Achievements] bigint  NULL,
    [WebUserId] nvarchar(128)  NULL,
    [DonationLevel] int  NULL,
    [Founder] bit  NULL,
    [CustomGifSet] nvarchar(max)  NULL,
    [GifPurchased] bit  NULL,
    [NewAchievements] varbinary(50)  NULL
);
GO

-- Creating table 'NotifyGames'
CREATE TABLE [dbo].[NotifyGames] (
    [UserId] int  NOT NULL,
    [GroupId] bigint  NOT NULL
);
GO

-- Creating table 'v_NonDefaultGroups'
CREATE TABLE [dbo].[v_NonDefaultGroups] (
    [Name] nvarchar(max)  NOT NULL,
    [Language] nvarchar(max)  NULL,
    [ShowRoles] bit  NULL,
    [ShowRolesEnd] nvarchar(50)  NULL,
    [Mode] nvarchar(50)  NULL,
    [DayTime] int  NULL,
    [NightTime] int  NULL,
    [LynchTime] int  NULL,
    [AllowFool] bit  NULL,
    [AllowTanner] bit  NULL,
    [AllowCult] bit  NULL,
    [UserName] nvarchar(max)  NULL,
    [BotInGroup] bit  NULL,
    [DisableNotification] bit  NULL,
    [MaxPlayers] int  NULL,
    [DisableFlee] bit  NULL,
    [Preferred] bit  NULL,
    [GroupId] bigint  NOT NULL
);
GO

-- Creating table 'v_PreferredGroups'
CREATE TABLE [dbo].[v_PreferredGroups] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NOT NULL,
    [GroupId] bigint  NOT NULL,
    [Language] nvarchar(max)  NULL,
    [UserName] nvarchar(max)  NULL,
    [Description] nvarchar(max)  NULL,
    [GroupLink] nvarchar(max)  NULL
);
GO

-- Creating table 'v_WaitList'
CREATE TABLE [dbo].[v_WaitList] (
    [Name] nvarchar(max)  NOT NULL,
    [Expr1] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'GlobalStats'
CREATE TABLE [dbo].[GlobalStats] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [GamesPlayed] int  NOT NULL,
    [PlayersKilled] int  NOT NULL,
    [PlayersSurvived] int  NOT NULL,
    [MostKilledFirstNight] nvarchar(max)  NULL,
    [MostKilledFirstPercent] int  NULL,
    [MostLynchedFirstDay] nvarchar(max)  NULL,
    [MostLynchedFirstPercent] int  NULL,
    [MostKilledFirstDay] nvarchar(max)  NULL,
    [MostKilledFirstDayPercent] int  NULL,
    [BestSurvivor] nvarchar(max)  NULL,
    [BestSurvivorPercent] int  NULL,
    [LastRun] datetime  NULL,
    [TotalPlayers] int  NULL,
    [TotalGroups] int  NULL,
    [MostKilledFirstNightId] int  NULL,
    [MostLynchedFirstDayId] int  NULL,
    [MostKilledFirstDayId] int  NULL,
    [BestSurvivorId] int  NULL
);
GO

-- Creating table 'PlayerStats'
CREATE TABLE [dbo].[PlayerStats] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [PlayerId] int  NOT NULL,
    [GamesPlayed] int  NOT NULL,
    [GamesWon] int  NOT NULL,
    [GamesLost] int  NOT NULL,
    [MostCommonRole] nvarchar(50)  NOT NULL,
    [MostKilled] nvarchar(max)  NULL,
    [MostKilledBy] nvarchar(max)  NULL,
    [MostCommonRolePercent] int  NOT NULL,
    [GamesSurvived] int  NOT NULL,
    [LastRun] datetime  NULL
);
GO

-- Creating table 'GroupStats'
CREATE TABLE [dbo].[GroupStats] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [GroupId] bigint  NOT NULL,
    [GamesPlayed] int  NOT NULL,
    [MostKilledFirstNight] nvarchar(max)  NULL,
    [MostKilledFirstPercent] int  NULL,
    [MostLynchedFirstNight] nvarchar(max)  NULL,
    [MostLynchFirstPercent] int  NULL,
    [MostDeadFirstDay] nvarchar(max)  NULL,
    [MostDeadFirstPercent] int  NULL,
    [BestSurvivor] nvarchar(max)  NULL,
    [BestSurvivorPercent] int  NULL,
    [LastRun] datetime  NULL,
    [GroupName] nvarchar(max)  NULL
);
GO

-- Creating table 'DailyCounts'
CREATE TABLE [dbo].[DailyCounts] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Day] datetime  NOT NULL,
    [Groups] int  NOT NULL,
    [Games] int  NOT NULL,
    [Users] int  NOT NULL
);
GO

-- Creating table 'v_IdleKill24HoursMain'
CREATE TABLE [dbo].[v_IdleKill24HoursMain] (
    [Idles] int  NULL,
    [Name] nvarchar(max)  NOT NULL,
    [UserName] nvarchar(max)  NULL
);
GO

-- Creating table 'v_PublicGroups'
CREATE TABLE [dbo].[v_PublicGroups] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(max)  NOT NULL,
    [Language] nvarchar(max)  NULL,
    [MemberCount] int  NULL,
    [GroupLink] nvarchar(max)  NULL
);
GO

-- Creating table 'GlobalBans'
CREATE TABLE [dbo].[GlobalBans] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [TelegramId] int  NOT NULL,
    [Reason] nvarchar(max)  NOT NULL,
    [Expires] datetime  NOT NULL,
    [BannedBy] nvarchar(max)  NOT NULL,
    [BanDate] datetime  NULL,
    [Name] nvarchar(max)  NULL
);
GO

-- Creating table 'v_InactivePlayersMain'
CREATE TABLE [dbo].[v_InactivePlayersMain] (
    [Id] int  NOT NULL,
    [Name] nvarchar(max)  NOT NULL,
    [TelegramId] int  NOT NULL,
    [last] datetime  NULL
);
GO

-- Creating table 'BotStatus'
CREATE TABLE [dbo].[BotStatus] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [BotName] varchar(50)  NOT NULL,
    [BotStatus] varchar(50)  NOT NULL,
    [BotLink] varchar(max)  NOT NULL
);
GO

-- Creating table 'ContestTerms'
CREATE TABLE [dbo].[ContestTerms] (
    [TelegramId] int  NOT NULL,
    [AgreedTerms] bit  NOT NULL
);
GO

-- Creating table 'AspNetRoles'
CREATE TABLE [dbo].[AspNetRoles] (
    [Id] nvarchar(128)  NOT NULL,
    [Name] nvarchar(256)  NOT NULL
);
GO

-- Creating table 'AspNetUserClaims'
CREATE TABLE [dbo].[AspNetUserClaims] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [UserId] nvarchar(128)  NOT NULL,
    [ClaimType] nvarchar(max)  NULL,
    [ClaimValue] nvarchar(max)  NULL
);
GO

-- Creating table 'AspNetUserLogins'
CREATE TABLE [dbo].[AspNetUserLogins] (
    [LoginProvider] nvarchar(128)  NOT NULL,
    [ProviderKey] nvarchar(128)  NOT NULL,
    [UserId] nvarchar(128)  NOT NULL
);
GO

-- Creating table 'AspNetUsers'
CREATE TABLE [dbo].[AspNetUsers] (
    [Id] nvarchar(128)  NOT NULL,
    [Email] nvarchar(256)  NULL,
    [EmailConfirmed] bit  NOT NULL,
    [PasswordHash] nvarchar(max)  NULL,
    [SecurityStamp] nvarchar(max)  NULL,
    [PhoneNumber] nvarchar(max)  NULL,
    [PhoneNumberConfirmed] bit  NOT NULL,
    [TwoFactorEnabled] bit  NOT NULL,
    [LockoutEndDateUtc] datetime  NULL,
    [LockoutEnabled] bit  NOT NULL,
    [AccessFailedCount] int  NOT NULL,
    [UserName] nvarchar(256)  NOT NULL
);
GO

-- Creating table 'C__MigrationHistory'
CREATE TABLE [dbo].[C__MigrationHistory] (
    [MigrationId] nvarchar(150)  NOT NULL,
    [ContextKey] nvarchar(300)  NOT NULL,
    [Model] varbinary(max)  NOT NULL,
    [ProductVersion] nvarchar(32)  NOT NULL
);
GO

-- Creating table 'GroupRanking'
CREATE TABLE [dbo].[GroupRanking] (
    [GroupId] int  NOT NULL,
    [Language] nvarchar(450)  NOT NULL,
    [PlayersCount] int  NOT NULL,
    [MinutesPlayed] decimal(18,10)  NOT NULL,
    [Ranking] decimal(18,10)  NULL,
    [LastRefresh] datetime  NOT NULL,
    [Id] int IDENTITY(1,1) NOT NULL,
    [GamesPlayed] int  NOT NULL,
    [Show] bit  NULL
);
GO

-- Creating table 'RefreshDate'
CREATE TABLE [dbo].[RefreshDate] (
    [Lock] char(1)  NOT NULL,
    [Date] datetime  NOT NULL
);
GO

-- Creating table 'v_GroupRanking'
CREATE TABLE [dbo].[v_GroupRanking] (
    [GroupId] int  NOT NULL,
    [TelegramId] bigint  NOT NULL,
    [Language] nvarchar(450)  NOT NULL,
    [Description] nvarchar(max)  NULL,
    [GroupLink] nvarchar(max)  NULL,
    [Ranking] decimal(18,0)  NULL,
    [LastRefresh] datetime  NOT NULL,
    [Name] nvarchar(max)  NOT NULL
);
GO

-- Creating table 'Language'
CREATE TABLE [dbo].[Language] (
    [Id] int IDENTITY(1,1) NOT NULL,
    [Name] nvarchar(100)  NOT NULL,
    [Base] nvarchar(100)  NOT NULL,
    [Variant] nvarchar(100)  NOT NULL,
    [FileName] nvarchar(200)  NOT NULL,
    [IsDefault] bit  NOT NULL,
    [LangCode] nvarchar(10)  NULL
);
GO

-- Creating table 'GroupAdmin'
CREATE TABLE [dbo].[GroupAdmin] (
    [Groups_Id] int  NOT NULL,
    [Players_Id] int  NOT NULL
);
GO

-- Creating table 'AspNetUserRoles'
CREATE TABLE [dbo].[AspNetUserRoles] (
    [AspNetRoles_Id] nvarchar(128)  NOT NULL,
    [AspNetUsers_Id] nvarchar(128)  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [UserId] in table 'Admins'
ALTER TABLE [dbo].[Admins]
ADD CONSTRAINT [PK_Admins]
    PRIMARY KEY CLUSTERED ([UserId] ASC);
GO

-- Creating primary key on [Id] in table 'Games'
ALTER TABLE [dbo].[Games]
ADD CONSTRAINT [PK_Games]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'GameKills'
ALTER TABLE [dbo].[GameKills]
ADD CONSTRAINT [PK_GameKills]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'GamePlayers'
ALTER TABLE [dbo].[GamePlayers]
ADD CONSTRAINT [PK_GamePlayers]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Groups'
ALTER TABLE [dbo].[Groups]
ADD CONSTRAINT [PK_Groups]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'KillMethods'
ALTER TABLE [dbo].[KillMethods]
ADD CONSTRAINT [PK_KillMethods]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'Players'
ALTER TABLE [dbo].[Players]
ADD CONSTRAINT [PK_Players]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [UserId], [GroupId] in table 'NotifyGames'
ALTER TABLE [dbo].[NotifyGames]
ADD CONSTRAINT [PK_NotifyGames]
    PRIMARY KEY CLUSTERED ([UserId], [GroupId] ASC);
GO

-- Creating primary key on [Name], [GroupId] in table 'v_NonDefaultGroups'
ALTER TABLE [dbo].[v_NonDefaultGroups]
ADD CONSTRAINT [PK_v_NonDefaultGroups]
    PRIMARY KEY CLUSTERED ([Name], [GroupId] ASC);
GO

-- Creating primary key on [Id], [Name], [GroupId] in table 'v_PreferredGroups'
ALTER TABLE [dbo].[v_PreferredGroups]
ADD CONSTRAINT [PK_v_PreferredGroups]
    PRIMARY KEY CLUSTERED ([Id], [Name], [GroupId] ASC);
GO

-- Creating primary key on [Name], [Expr1] in table 'v_WaitList'
ALTER TABLE [dbo].[v_WaitList]
ADD CONSTRAINT [PK_v_WaitList]
    PRIMARY KEY CLUSTERED ([Name], [Expr1] ASC);
GO

-- Creating primary key on [Id] in table 'GlobalStats'
ALTER TABLE [dbo].[GlobalStats]
ADD CONSTRAINT [PK_GlobalStats]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'PlayerStats'
ALTER TABLE [dbo].[PlayerStats]
ADD CONSTRAINT [PK_PlayerStats]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'GroupStats'
ALTER TABLE [dbo].[GroupStats]
ADD CONSTRAINT [PK_GroupStats]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'DailyCounts'
ALTER TABLE [dbo].[DailyCounts]
ADD CONSTRAINT [PK_DailyCounts]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Name] in table 'v_IdleKill24HoursMain'
ALTER TABLE [dbo].[v_IdleKill24HoursMain]
ADD CONSTRAINT [PK_v_IdleKill24HoursMain]
    PRIMARY KEY CLUSTERED ([Name] ASC);
GO

-- Creating primary key on [Id], [Name] in table 'v_PublicGroups'
ALTER TABLE [dbo].[v_PublicGroups]
ADD CONSTRAINT [PK_v_PublicGroups]
    PRIMARY KEY CLUSTERED ([Id], [Name] ASC);
GO

-- Creating primary key on [Id] in table 'GlobalBans'
ALTER TABLE [dbo].[GlobalBans]
ADD CONSTRAINT [PK_GlobalBans]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id], [Name], [TelegramId] in table 'v_InactivePlayersMain'
ALTER TABLE [dbo].[v_InactivePlayersMain]
ADD CONSTRAINT [PK_v_InactivePlayersMain]
    PRIMARY KEY CLUSTERED ([Id], [Name], [TelegramId] ASC);
GO

-- Creating primary key on [Id] in table 'BotStatus'
ALTER TABLE [dbo].[BotStatus]
ADD CONSTRAINT [PK_BotStatus]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [TelegramId] in table 'ContestTerms'
ALTER TABLE [dbo].[ContestTerms]
ADD CONSTRAINT [PK_ContestTerms]
    PRIMARY KEY CLUSTERED ([TelegramId] ASC);
GO

-- Creating primary key on [Id] in table 'AspNetRoles'
ALTER TABLE [dbo].[AspNetRoles]
ADD CONSTRAINT [PK_AspNetRoles]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Id] in table 'AspNetUserClaims'
ALTER TABLE [dbo].[AspNetUserClaims]
ADD CONSTRAINT [PK_AspNetUserClaims]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [LoginProvider], [ProviderKey], [UserId] in table 'AspNetUserLogins'
ALTER TABLE [dbo].[AspNetUserLogins]
ADD CONSTRAINT [PK_AspNetUserLogins]
    PRIMARY KEY CLUSTERED ([LoginProvider], [ProviderKey], [UserId] ASC);
GO

-- Creating primary key on [Id] in table 'AspNetUsers'
ALTER TABLE [dbo].[AspNetUsers]
ADD CONSTRAINT [PK_AspNetUsers]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [MigrationId], [ContextKey] in table 'C__MigrationHistory'
ALTER TABLE [dbo].[C__MigrationHistory]
ADD CONSTRAINT [PK_C__MigrationHistory]
    PRIMARY KEY CLUSTERED ([MigrationId], [ContextKey] ASC);
GO

-- Creating primary key on [Id] in table 'GroupRanking'
ALTER TABLE [dbo].[GroupRanking]
ADD CONSTRAINT [PK_GroupRanking]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Lock] in table 'RefreshDate'
ALTER TABLE [dbo].[RefreshDate]
ADD CONSTRAINT [PK_RefreshDate]
    PRIMARY KEY CLUSTERED ([Lock] ASC);
GO

-- Creating primary key on [GroupId], [Language] in table 'v_GroupRanking'
ALTER TABLE [dbo].[v_GroupRanking]
ADD CONSTRAINT [PK_v_GroupRanking]
    PRIMARY KEY CLUSTERED ([GroupId], [Language] ASC);
GO

-- Creating primary key on [Id] in table 'Language'
ALTER TABLE [dbo].[Language]
ADD CONSTRAINT [PK_Language]
    PRIMARY KEY CLUSTERED ([Id] ASC);
GO

-- Creating primary key on [Groups_Id], [Players_Id] in table 'GroupAdmin'
ALTER TABLE [dbo].[GroupAdmin]
ADD CONSTRAINT [PK_GroupAdmin]
    PRIMARY KEY CLUSTERED ([Groups_Id], [Players_Id] ASC);
GO

-- Creating primary key on [AspNetRoles_Id], [AspNetUsers_Id] in table 'AspNetUserRoles'
ALTER TABLE [dbo].[AspNetUserRoles]
ADD CONSTRAINT [PK_AspNetUserRoles]
    PRIMARY KEY CLUSTERED ([AspNetRoles_Id], [AspNetUsers_Id] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [GrpId] in table 'Games'
ALTER TABLE [dbo].[Games]
ADD CONSTRAINT [FK_Game_Group]
    FOREIGN KEY ([GrpId])
    REFERENCES [dbo].[Groups]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_Game_Group'
CREATE INDEX [IX_FK_Game_Group]
ON [dbo].[Games]
    ([GrpId]);
GO

-- Creating foreign key on [GameId] in table 'GameKills'
ALTER TABLE [dbo].[GameKills]
ADD CONSTRAINT [FK_GameKill_Game]
    FOREIGN KEY ([GameId])
    REFERENCES [dbo].[Games]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_GameKill_Game'
CREATE INDEX [IX_FK_GameKill_Game]
ON [dbo].[GameKills]
    ([GameId]);
GO

-- Creating foreign key on [KillerId] in table 'GameKills'
ALTER TABLE [dbo].[GameKills]
ADD CONSTRAINT [FK_GameKill_Killer]
    FOREIGN KEY ([KillerId])
    REFERENCES [dbo].[Players]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_GameKill_Killer'
CREATE INDEX [IX_FK_GameKill_Killer]
ON [dbo].[GameKills]
    ([KillerId]);
GO

-- Creating foreign key on [KillMethodId] in table 'GameKills'
ALTER TABLE [dbo].[GameKills]
ADD CONSTRAINT [FK_GameKill_KillMethod]
    FOREIGN KEY ([KillMethodId])
    REFERENCES [dbo].[KillMethods]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_GameKill_KillMethod'
CREATE INDEX [IX_FK_GameKill_KillMethod]
ON [dbo].[GameKills]
    ([KillMethodId]);
GO

-- Creating foreign key on [VictimId] in table 'GameKills'
ALTER TABLE [dbo].[GameKills]
ADD CONSTRAINT [FK_GameKill_Victim]
    FOREIGN KEY ([VictimId])
    REFERENCES [dbo].[Players]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_GameKill_Victim'
CREATE INDEX [IX_FK_GameKill_Victim]
ON [dbo].[GameKills]
    ([VictimId]);
GO

-- Creating foreign key on [PlayerId] in table 'GamePlayers'
ALTER TABLE [dbo].[GamePlayers]
ADD CONSTRAINT [FK_GamePlayer_Player]
    FOREIGN KEY ([PlayerId])
    REFERENCES [dbo].[Players]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_GamePlayer_Player'
CREATE INDEX [IX_FK_GamePlayer_Player]
ON [dbo].[GamePlayers]
    ([PlayerId]);
GO

-- Creating foreign key on [Groups_Id] in table 'GroupAdmin'
ALTER TABLE [dbo].[GroupAdmin]
ADD CONSTRAINT [FK_GroupAdmin_Group]
    FOREIGN KEY ([Groups_Id])
    REFERENCES [dbo].[Groups]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Players_Id] in table 'GroupAdmin'
ALTER TABLE [dbo].[GroupAdmin]
ADD CONSTRAINT [FK_GroupAdmin_Player]
    FOREIGN KEY ([Players_Id])
    REFERENCES [dbo].[Players]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_GroupAdmin_Player'
CREATE INDEX [IX_FK_GroupAdmin_Player]
ON [dbo].[GroupAdmin]
    ([Players_Id]);
GO

-- Creating foreign key on [UserId] in table 'AspNetUserClaims'
ALTER TABLE [dbo].[AspNetUserClaims]
ADD CONSTRAINT [FK_dbo_AspNetUserClaims_dbo_AspNetUsers_UserId]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[AspNetUsers]
        ([Id])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_dbo_AspNetUserClaims_dbo_AspNetUsers_UserId'
CREATE INDEX [IX_FK_dbo_AspNetUserClaims_dbo_AspNetUsers_UserId]
ON [dbo].[AspNetUserClaims]
    ([UserId]);
GO

-- Creating foreign key on [UserId] in table 'AspNetUserLogins'
ALTER TABLE [dbo].[AspNetUserLogins]
ADD CONSTRAINT [FK_dbo_AspNetUserLogins_dbo_AspNetUsers_UserId]
    FOREIGN KEY ([UserId])
    REFERENCES [dbo].[AspNetUsers]
        ([Id])
    ON DELETE CASCADE ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_dbo_AspNetUserLogins_dbo_AspNetUsers_UserId'
CREATE INDEX [IX_FK_dbo_AspNetUserLogins_dbo_AspNetUsers_UserId]
ON [dbo].[AspNetUserLogins]
    ([UserId]);
GO

-- Creating foreign key on [AspNetRoles_Id] in table 'AspNetUserRoles'
ALTER TABLE [dbo].[AspNetUserRoles]
ADD CONSTRAINT [FK_AspNetUserRoles_AspNetRole]
    FOREIGN KEY ([AspNetRoles_Id])
    REFERENCES [dbo].[AspNetRoles]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [AspNetUsers_Id] in table 'AspNetUserRoles'
ALTER TABLE [dbo].[AspNetUserRoles]
ADD CONSTRAINT [FK_AspNetUserRoles_AspNetUser]
    FOREIGN KEY ([AspNetUsers_Id])
    REFERENCES [dbo].[AspNetUsers]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_AspNetUserRoles_AspNetUser'
CREATE INDEX [IX_FK_AspNetUserRoles_AspNetUser]
ON [dbo].[AspNetUserRoles]
    ([AspNetUsers_Id]);
GO

-- Creating foreign key on [GameId] in table 'GamePlayers'
ALTER TABLE [dbo].[GamePlayers]
ADD CONSTRAINT [FK_GamePlayer_Game]
    FOREIGN KEY ([GameId])
    REFERENCES [dbo].[Games]
        ([Id])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating non-clustered index for FOREIGN KEY 'FK_GamePlayer_Game'
CREATE INDEX [IX_FK_GamePlayer_Game]
ON [dbo].[GamePlayers]
    ([GameId]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------