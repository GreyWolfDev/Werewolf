USE [master]
GO

/****** Object:  Database [werewolf]    Script Date: 7/26/2016 9:08:39 AM ******/
CREATE DATABASE [werewolf]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'werewolf', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL12.SQLEXPRESS\MSSQL\DATA\werewolf.mdf' , SIZE = 2046976KB , MAXSIZE = UNLIMITED, FILEGROWTH = 1024KB )
 LOG ON 
( NAME = N'werewolf_log', FILENAME = N'C:\Program Files\Microsoft SQL Server\MSSQL12.SQLEXPRESS\MSSQL\DATA\werewolf_log.ldf' , SIZE = 241216KB , MAXSIZE = 2048GB , FILEGROWTH = 10%)
GO

ALTER DATABASE [werewolf] SET COMPATIBILITY_LEVEL = 120
GO

IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [werewolf].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO

ALTER DATABASE [werewolf] SET ANSI_NULL_DEFAULT OFF 
GO

ALTER DATABASE [werewolf] SET ANSI_NULLS OFF 
GO

ALTER DATABASE [werewolf] SET ANSI_PADDING OFF 
GO

ALTER DATABASE [werewolf] SET ANSI_WARNINGS OFF 
GO

ALTER DATABASE [werewolf] SET ARITHABORT OFF 
GO

ALTER DATABASE [werewolf] SET AUTO_CLOSE OFF 
GO

ALTER DATABASE [werewolf] SET AUTO_SHRINK OFF 
GO

ALTER DATABASE [werewolf] SET AUTO_UPDATE_STATISTICS ON 
GO

ALTER DATABASE [werewolf] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO

ALTER DATABASE [werewolf] SET CURSOR_DEFAULT  GLOBAL 
GO

ALTER DATABASE [werewolf] SET CONCAT_NULL_YIELDS_NULL OFF 
GO

ALTER DATABASE [werewolf] SET NUMERIC_ROUNDABORT OFF 
GO

ALTER DATABASE [werewolf] SET QUOTED_IDENTIFIER OFF 
GO

ALTER DATABASE [werewolf] SET RECURSIVE_TRIGGERS OFF 
GO

ALTER DATABASE [werewolf] SET  DISABLE_BROKER 
GO

ALTER DATABASE [werewolf] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO

ALTER DATABASE [werewolf] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO

ALTER DATABASE [werewolf] SET TRUSTWORTHY OFF 
GO

ALTER DATABASE [werewolf] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO

ALTER DATABASE [werewolf] SET PARAMETERIZATION SIMPLE 
GO

ALTER DATABASE [werewolf] SET READ_COMMITTED_SNAPSHOT OFF 
GO

ALTER DATABASE [werewolf] SET HONOR_BROKER_PRIORITY OFF 
GO

ALTER DATABASE [werewolf] SET RECOVERY SIMPLE 
GO

ALTER DATABASE [werewolf] SET  MULTI_USER 
GO

ALTER DATABASE [werewolf] SET PAGE_VERIFY CHECKSUM  
GO

ALTER DATABASE [werewolf] SET DB_CHAINING OFF 
GO

ALTER DATABASE [werewolf] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO

ALTER DATABASE [werewolf] SET TARGET_RECOVERY_TIME = 0 SECONDS 
GO

ALTER DATABASE [werewolf] SET DELAYED_DURABILITY = DISABLED 
GO

ALTER DATABASE [werewolf] SET  READ_WRITE 
GO

USE [werewolf]
GO
/****** Object:  Table [db_owner].[BotStatus]    Script Date: 9/6/2016 4:18:23 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [db_owner].[BotStatus](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[BotName] [varchar](50) NOT NULL,
	[BotStatus] [varchar](50) NOT NULL,
	[BotLink] [varchar](max) NOT NULL,
 CONSTRAINT [PK_BotStatus] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [db_owner].[ContestTerms]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [db_owner].[ContestTerms](
	[TelegramId] [int] NOT NULL,
	[AgreedTerms] [bit] NOT NULL,
 CONSTRAINT [PK_ContestTerms] PRIMARY KEY CLUSTERED 
(
	[TelegramId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [db_owner].[GlobalBan]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [db_owner].[GlobalBan](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TelegramId] [int] NOT NULL,
	[Reason] [nvarchar](max) NOT NULL,
	[Expires] [datetime] NOT NULL,
	[BannedBy] [nvarchar](max) NOT NULL,
	[BanDate] [datetime] NULL,
	[Name] [nvarchar](max) NULL,
 CONSTRAINT [PK_GlobalBan] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[__MigrationHistory]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO
CREATE TABLE [dbo].[__MigrationHistory](
	[MigrationId] [nvarchar](150) NOT NULL,
	[ContextKey] [nvarchar](300) NOT NULL,
	[Model] [varbinary](max) NOT NULL,
	[ProductVersion] [nvarchar](32) NOT NULL,
 CONSTRAINT [PK_dbo.__MigrationHistory] PRIMARY KEY CLUSTERED 
(
	[MigrationId] ASC,
	[ContextKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
SET ANSI_PADDING OFF
GO
/****** Object:  Table [dbo].[Action]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Action](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GameId] [int] NOT NULL,
	[TimeStamp] [datetime] NOT NULL,
	[InitiatorId] [int] NOT NULL,
	[ReceiverId] [int] NOT NULL,
	[ActionTaken] [nvarchar](50) NOT NULL,
	[Day] [int] NOT NULL,
 CONSTRAINT [PK_Action] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Admin]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Admin](
	[UserId] [int] NOT NULL,
 CONSTRAINT [PK_Admin] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[AspNetRoles]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetRoles](
	[Id] [nvarchar](128) NOT NULL,
	[Name] [nvarchar](256) NOT NULL,
 CONSTRAINT [PK_dbo.AspNetRoles] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[AspNetUserClaims]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserClaims](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [nvarchar](128) NOT NULL,
	[ClaimType] [nvarchar](max) NULL,
	[ClaimValue] [nvarchar](max) NULL,
 CONSTRAINT [PK_dbo.AspNetUserClaims] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[AspNetUserLogins]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserLogins](
	[LoginProvider] [nvarchar](128) NOT NULL,
	[ProviderKey] [nvarchar](128) NOT NULL,
	[UserId] [nvarchar](128) NOT NULL,
 CONSTRAINT [PK_dbo.AspNetUserLogins] PRIMARY KEY CLUSTERED 
(
	[LoginProvider] ASC,
	[ProviderKey] ASC,
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[AspNetUserRoles]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUserRoles](
	[UserId] [nvarchar](128) NOT NULL,
	[RoleId] [nvarchar](128) NOT NULL,
 CONSTRAINT [PK_dbo.AspNetUserRoles] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC,
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[AspNetUsers]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AspNetUsers](
	[Id] [nvarchar](128) NOT NULL,
	[Email] [nvarchar](256) NULL,
	[EmailConfirmed] [bit] NOT NULL,
	[PasswordHash] [nvarchar](max) NULL,
	[SecurityStamp] [nvarchar](max) NULL,
	[PhoneNumber] [nvarchar](max) NULL,
	[PhoneNumberConfirmed] [bit] NOT NULL,
	[TwoFactorEnabled] [bit] NOT NULL,
	[LockoutEndDateUtc] [datetime] NULL,
	[LockoutEnabled] [bit] NOT NULL,
	[AccessFailedCount] [int] NOT NULL,
	[UserName] [nvarchar](256) NOT NULL,
 CONSTRAINT [PK_dbo.AspNetUsers] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[DailyCount]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DailyCount](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Day] [date] NOT NULL,
	[Groups] [int] NOT NULL,
	[Games] [int] NOT NULL,
	[Users] [int] NOT NULL,
 CONSTRAINT [PK_DailyCount] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Game]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Game](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GroupName] [nvarchar](max) NOT NULL,
	[GroupId] [bigint] NOT NULL,
	[TimeStarted] [datetime] NULL,
	[TimeEnded] [datetime] NULL,
	[Winner] [nvarchar](50) NULL,
	[GrpId] [int] NULL,
	[Mode] [nvarchar](50) NULL,
 CONSTRAINT [PK_Game] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[GameKill]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GameKill](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GameId] [int] NOT NULL,
	[KillerId] [int] NOT NULL,
	[VictimId] [int] NOT NULL,
	[TimeStamp] [datetime] NOT NULL,
	[KillMethodId] [int] NOT NULL,
	[Day] [int] NOT NULL,
 CONSTRAINT [PK_GameKill] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[GamePlayer]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GamePlayer](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PlayerId] [int] NOT NULL,
	[GameId] [int] NOT NULL,
	[Survived] [bit] NOT NULL,
	[Won] [bit] NOT NULL,
	[Role] [nvarchar](50) NULL,
 CONSTRAINT [PK_GamePlayer] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[GlobalStats]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GlobalStats](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GamesPlayed] [int] NOT NULL,
	[PlayersKilled] [int] NOT NULL,
	[PlayersSurvived] [int] NOT NULL,
	[MostKilledFirstNight] [nvarchar](max) NULL,
	[MostKilledFirstPercent] [int] NULL,
	[MostLynchedFirstDay] [nvarchar](max) NULL,
	[MostLynchedFirstPercent] [int] NULL,
	[MostKilledFirstDay] [nvarchar](max) NULL,
	[MostKilledFirstDayPercent] [int] NULL,
	[BestSurvivor] [nvarchar](max) NULL,
	[BestSurvivorPercent] [int] NULL,
	[LastRun] [datetime] NULL,
	[TotalPlayers] [int] NULL,
	[TotalGroups] [int] NULL,
	[MostKilledFirstNightId] [int] NULL,
	[MostLynchedFirstDayId] [int] NULL,
	[MostKilledFirstDayId] [int] NULL,
	[BestSurvivorId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Group]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Group](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[GroupId] [bigint] NOT NULL,
	[Preferred] [bit] NULL,
	[Language] [nvarchar](max) NULL,
	[DisableNotification] [bit] NULL,
	[UserName] [nvarchar](max) NULL,
	[BotInGroup] [bit] NULL,
	[ShowRoles] [bit] NULL,
	[Mode] [nvarchar](50) NULL,
	[DayTime] [int] NULL,
	[NightTime] [int] NULL,
	[LynchTime] [int] NULL,
	[AllowTanner] [bit] NULL,
	[AllowFool] [bit] NULL,
	[AllowCult] [bit] NULL,
	[ShowRolesEnd] [nvarchar](50) NULL,
	[MaxPlayers] [int] NULL,
	[DisableFlee] [bit] NULL,
	[CreatedBy] [nvarchar](max) NULL,
	[ImageFile] [nvarchar](max) NULL,
	[Description] [nvarchar](max) NULL,
	[GroupLink] [nvarchar](max) NULL,
	[MemberCount] [int] NULL,
	[AllowExtend] [bit] NULL,
	[MaxExtend] [int] NULL,
	[EnableSecretLynch] [bit] NULL,
	[Flags] [bigint] NULL,
 CONSTRAINT [PK_Group] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[GroupAdmin]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GroupAdmin](
	[GroupId] [int] NOT NULL,
	[PlayerId] [int] NOT NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[GroupStats]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[GroupStats](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[GroupId] [bigint] NOT NULL,
	[GamesPlayed] [int] NOT NULL,
	[MostKilledFirstNight] [nvarchar](max) NULL,
	[MostKilledFirstPercent] [int] NULL,
	[MostLynchedFirstNight] [nvarchar](max) NULL,
	[MostLynchFirstPercent] [int] NULL,
	[MostDeadFirstDay] [nvarchar](max) NULL,
	[MostDeadFirstPercent] [int] NULL,
	[BestSurvivor] [nvarchar](max) NULL,
	[BestSurvivorPercent] [int] NULL,
	[LastRun] [datetime] NULL,
	[GroupName] [nvarchar](max) NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[KillMethod]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[KillMethod](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_KillMethod] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[NotifyGame]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NotifyGame](
	[UserId] [int] NOT NULL,
	[GroupId] [bigint] NOT NULL
) ON [PRIMARY]

GO
/****** Object:  Table [dbo].[Player]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Player](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TelegramId] [int] NOT NULL,
	[Name] [nvarchar](max) NOT NULL,
	[UserName] [nvarchar](max) NULL,
	[Banned] [bit] NULL,
	[BannedBy] [nvarchar](max) NULL,
	[HasPM] [bit] NULL,
	[BanReason] [nvarchar](max) NULL,
	[ImageFile] [nvarchar](max) NULL,
	[Language] [nvarchar](max) NULL,
	[TempBanCount] [int] NULL,
	[HasPM2] [bit] NULL,
	[HasDebugPM] [bit] NULL,
	[Achievements] [bigint] NULL,
	[WebUserId] [nvarchar](128) NULL,
	[DonationLevel] [int] NULL,
	[Founder] [bit] NULL,
	[CustomGifSet] [nvarchar](max) NULL,
	[GifPurchased] [bit] NULL,
 CONSTRAINT [PK_Player] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  Table [dbo].[PlayerStats]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PlayerStats](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[PlayerId] [int] NOT NULL,
	[GamesPlayed] [int] NOT NULL,
	[GamesWon] [int] NOT NULL,
	[GamesLost] [int] NOT NULL,
	[MostCommonRole] [nvarchar](50) NOT NULL,
	[MostKilled] [nvarchar](max) NULL,
	[MostKilledBy] [nvarchar](max) NULL,
	[MostCommonRolePercent] [int] NOT NULL,
	[GamesSurvived] [int] NOT NULL,
	[LastRun] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]

GO
/****** Object:  View [db_owner].[v_BotInGroups]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [db_owner].[v_BotInGroups]
AS
SELECT        COUNT(Id) AS Groups, CASE BotInGroup WHEN 0 THEN 'No' WHEN 1 THEN 'Yes' END AS 'Has Bot?'
FROM            dbo.[Group]
WHERE        (BotInGroup IS NOT NULL)
GROUP BY BotInGroup

GO
/****** Object:  View [db_owner].[v_InactivePlayersMain]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [db_owner].[v_InactivePlayersMain]
AS
SELECT        p.Id, p.Name, p.TelegramId, x.last
FROM            dbo.Player AS p INNER JOIN
                             (SELECT        MAX(g.TimeStarted) AS last, gp.PlayerId
                               FROM            dbo.GamePlayer AS gp INNER JOIN
                                                         dbo.Game AS g ON gp.GameId = g.Id
                               WHERE        (g.GrpId = 2882)
                               GROUP BY gp.PlayerId) AS x ON p.Id = x.PlayerId
WHERE        (x.last < DATEADD(day, - 14, GETDATE()))

GO
/****** Object:  View [db_owner].[v_LanguageCounts]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [db_owner].[v_LanguageCounts]
AS
SELECT        TOP (100) PERCENT COUNT(Id) AS Groups, Language
FROM            dbo.[Group]
GROUP BY Language
ORDER BY Groups DESC

GO
/****** Object:  View [db_owner].[v_NonDefaultGroups]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [db_owner].[v_NonDefaultGroups]
AS
SELECT        Name, Language, ShowRoles, ShowRolesEnd, Mode, DayTime, NightTime, LynchTime, AllowFool, AllowTanner, AllowCult, UserName, BotInGroup, DisableNotification, MaxPlayers, DisableFlee, Preferred, 
                         GroupId
FROM            dbo.[Group]
WHERE        (DayTime <> 60) OR
                         (NightTime <> 90) OR
                         (LynchTime <> 90) AND (AllowTanner <> 1) OR
                         (AllowFool <> 1) OR
                         (AllowCult <> 1) OR
                         (Mode <> 'Player') OR
                         (ShowRoles <> 1) OR
                         (ShowRolesEnd <> 'Living') OR
                         (MaxPlayers <> 35) OR
                         (DisableFlee = 1)

GO
/****** Object:  View [db_owner].[v_PreferredGroups]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [db_owner].[v_PreferredGroups]
AS
SELECT DISTINCT Id, Name, GroupId, Language, UserName, Description, GroupLink
FROM            dbo.[Group]
WHERE        (Preferred = 1)

GO
/****** Object:  View [db_owner].[v_PublicGroups]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [db_owner].[v_PublicGroups]
AS
SELECT        Id, Name, (CASE username WHEN 'mainwerewolfindo' THEN 'Bahasa Indo' WHEN 'werewolfgameindonesia' THEN 'Bahasa Indo' WHEN 'Bobervidihay' THEN 'Russian' ELSE Language END) AS Language, 
                         MemberCount, GroupLink
FROM            dbo.[Group]
WHERE        (GroupLink IS NOT NULL) AND (GroupId <> - 1001055238687) AND (BotInGroup = 1) AND (GroupId <> - 1001062468289)

GO
/****** Object:  View [db_owner].[v_SummaryTotals]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [db_owner].[v_SummaryTotals]
AS
SELECT        (SELECT        COUNT(Id) AS Expr1
                          FROM            dbo.Player) AS Players,
                             (SELECT        COUNT(Id) AS Expr1
                               FROM            dbo.Game) AS Games,
                             (SELECT        COUNT(Id) AS Expr1
                               FROM            dbo.[Group]) AS Groups,
                             (SELECT        COUNT(Id) AS Expr1
                               FROM            dbo.GamePlayer
                               WHERE        (Survived = 0)) AS Deaths,
                             (SELECT        COUNT(Id) AS Expr1
                               FROM            dbo.GamePlayer AS GamePlayer_1
                               WHERE        (Survived = 1)) AS Survivors,
                             (SELECT        MIN(g.TimeStarted) AS Expr1
                               FROM            dbo.GamePlayer AS gp INNER JOIN
                                                         dbo.Game AS g ON gp.GameId = g.Id) AS Since

GO
/****** Object:  View [db_owner].[v_WaitList]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [db_owner].[v_WaitList]
AS
SELECT        g.Name, p.Name AS Expr1
FROM            dbo.NotifyGame AS n INNER JOIN
                         dbo.[Group] AS g ON n.GroupId = g.GroupId INNER JOIN
                         dbo.Player AS p ON p.TelegramId = n.UserId

GO
/****** Object:  View [db_owner].[v_WinRatios]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [db_owner].[v_WinRatios]
AS
SELECT x.Players
, Count (x.GameId) AS Wins
, X.Winner AS Team
, Round((COUNT (x.GameId) * 100.0 / sum (count(x.GameId)) OVER (PARTITION BY Players)), 2) AS [%]
FROM
(
SELECT DISTINCT [gameid]
FROM [werewolf].[Dbo].[Action] WHERE ActionTaken = 'convert' AND [TimeStamp]> '05/15/2016'
) AS ac
iNNER JOIN
(
SELECT count (gp.PlayerId) AS Players
, gp.GameId
, CASE WHEN gm.Winner = 'Wolves' THEN 'Wolf' ELSE gm.Winner END AS Winner
FROM Game AS gm
INNER JOIN GamePlayer AS gp ON gp.GameId = gm.Id
WHERE gm.Winner is not null
GROUP BY gp.GameId, gm.Winner
HAVING COUNT (gp.PlayerId)> = 5
) AS x on ac.gameId = x.gameId

GROUP BY x.Winner, x.Players

GO
/****** Object:  View [dbo].[v_IdleKill24HoursMain]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[v_IdleKill24HoursMain]
AS
select count(gameid) as Idles, p.Name, p.UserName from 
gamekill gk
join game g on gk.GameId = g.Id
join player p on p.Id = gk.VictimId
where g.GroupId = -1001030085238
and KillMethodId = 16
and TimeStarted > DATEADD(day,-1,GETDATE())
group by p.Name, p.UserName

GO
ALTER TABLE [dbo].[Player] ADD  CONSTRAINT [DF_Player_Language]  DEFAULT ('English') FOR [Language]
GO
ALTER TABLE [dbo].[Action]  WITH CHECK ADD  CONSTRAINT [FK_Action_Game] FOREIGN KEY([GameId])
REFERENCES [dbo].[Game] ([Id])
GO
ALTER TABLE [dbo].[Action] CHECK CONSTRAINT [FK_Action_Game]
GO
ALTER TABLE [dbo].[Action]  WITH CHECK ADD  CONSTRAINT [FK_Action_Initiator] FOREIGN KEY([InitiatorId])
REFERENCES [dbo].[Player] ([Id])
GO
ALTER TABLE [dbo].[Action] CHECK CONSTRAINT [FK_Action_Initiator]
GO
ALTER TABLE [dbo].[Action]  WITH CHECK ADD  CONSTRAINT [FK_Action_Receiver] FOREIGN KEY([ReceiverId])
REFERENCES [dbo].[Player] ([Id])
GO
ALTER TABLE [dbo].[Action] CHECK CONSTRAINT [FK_Action_Receiver]
GO
ALTER TABLE [dbo].[AspNetUserClaims]  WITH CHECK ADD  CONSTRAINT [FK_dbo.AspNetUserClaims_dbo.AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserClaims] CHECK CONSTRAINT [FK_dbo.AspNetUserClaims_dbo.AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[AspNetUserLogins]  WITH CHECK ADD  CONSTRAINT [FK_dbo.AspNetUserLogins_dbo.AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserLogins] CHECK CONSTRAINT [FK_dbo.AspNetUserLogins_dbo.AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[AspNetUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_dbo.AspNetUserRoles_dbo.AspNetRoles_RoleId] FOREIGN KEY([RoleId])
REFERENCES [dbo].[AspNetRoles] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserRoles] CHECK CONSTRAINT [FK_dbo.AspNetUserRoles_dbo.AspNetRoles_RoleId]
GO
ALTER TABLE [dbo].[AspNetUserRoles]  WITH CHECK ADD  CONSTRAINT [FK_dbo.AspNetUserRoles_dbo.AspNetUsers_UserId] FOREIGN KEY([UserId])
REFERENCES [dbo].[AspNetUsers] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[AspNetUserRoles] CHECK CONSTRAINT [FK_dbo.AspNetUserRoles_dbo.AspNetUsers_UserId]
GO
ALTER TABLE [dbo].[Game]  WITH CHECK ADD  CONSTRAINT [FK_Game_Group] FOREIGN KEY([GrpId])
REFERENCES [dbo].[Group] ([Id])
GO
ALTER TABLE [dbo].[Game] CHECK CONSTRAINT [FK_Game_Group]
GO
ALTER TABLE [dbo].[GameKill]  WITH CHECK ADD  CONSTRAINT [FK_GameKill_Game] FOREIGN KEY([GameId])
REFERENCES [dbo].[Game] ([Id])
GO
ALTER TABLE [dbo].[GameKill] CHECK CONSTRAINT [FK_GameKill_Game]
GO
ALTER TABLE [dbo].[GameKill]  WITH CHECK ADD  CONSTRAINT [FK_GameKill_Killer] FOREIGN KEY([KillerId])
REFERENCES [dbo].[Player] ([Id])
GO
ALTER TABLE [dbo].[GameKill] CHECK CONSTRAINT [FK_GameKill_Killer]
GO
ALTER TABLE [dbo].[GameKill]  WITH CHECK ADD  CONSTRAINT [FK_GameKill_KillMethod] FOREIGN KEY([KillMethodId])
REFERENCES [dbo].[KillMethod] ([Id])
GO
ALTER TABLE [dbo].[GameKill] CHECK CONSTRAINT [FK_GameKill_KillMethod]
GO
ALTER TABLE [dbo].[GameKill]  WITH CHECK ADD  CONSTRAINT [FK_GameKill_Victim] FOREIGN KEY([VictimId])
REFERENCES [dbo].[Player] ([Id])
GO
ALTER TABLE [dbo].[GameKill] CHECK CONSTRAINT [FK_GameKill_Victim]
GO
ALTER TABLE [dbo].[GamePlayer]  WITH CHECK ADD  CONSTRAINT [FK_GamePlayer_Player] FOREIGN KEY([PlayerId])
REFERENCES [dbo].[Player] ([Id])
GO
ALTER TABLE [dbo].[GamePlayer] CHECK CONSTRAINT [FK_GamePlayer_Player]
GO
ALTER TABLE [dbo].[GroupAdmin]  WITH CHECK ADD  CONSTRAINT [FK_GroupAdmin_Group] FOREIGN KEY([GroupId])
REFERENCES [dbo].[Group] ([Id])
GO
ALTER TABLE [dbo].[GroupAdmin] CHECK CONSTRAINT [FK_GroupAdmin_Group]
GO
ALTER TABLE [dbo].[GroupAdmin]  WITH CHECK ADD  CONSTRAINT [FK_GroupAdmin_Player] FOREIGN KEY([PlayerId])
REFERENCES [dbo].[Player] ([Id])
GO
ALTER TABLE [dbo].[GroupAdmin] CHECK CONSTRAINT [FK_GroupAdmin_Player]
GO
/****** Object:  StoredProcedure [dbo].[getDailyCounts]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[getDailyCounts]
	-- Add the parameters for the stored procedure here
	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

SELECT        counts.Day, counts.Games, counts.Groups, playercount.players
FROM            (SELECT        SUM(games) AS Games, Day, COUNT(GroupId) AS Groups
                          FROM            (SELECT        COUNT(Id) AS games, Day, GroupId
                                                    FROM            (SELECT        Id, GroupId, CONVERT(date, TimeStarted) AS Day
                                                                              FROM            dbo.Game where timestarted >= DATEADD(DAY, -32, GETDATE()) ) AS x
                                                    GROUP BY Day, GroupId) AS y
                          GROUP BY Day) AS counts INNER JOIN
                             (SELECT        COUNT(PlayerId) AS players, Day
                               FROM            (SELECT DISTINCT gp.PlayerId, CONVERT(date, g.TimeStarted) AS Day
                                                         FROM            dbo.Game AS g INNER JOIN
                                                                                   dbo.GamePlayer AS gp ON g.Id = gp.GameId where timestarted >= DATEADD(DAY, -32, GETDATE())) AS x_1
                               GROUP BY Day) AS playercount ON counts.Day = playercount.Day
							   order by counts.Day DESC
END

GO
/****** Object:  StoredProcedure [dbo].[GetIdleKills24Hours]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[GetIdleKills24Hours]
	-- Add the parameters for the stored procedure here
	@userid int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	select count(gameid) from 
	gamekill gk
join player p on p.Id = gk.VictimId
where p.TelegramId = @userid
and KillMethodId = 16
and gk.TimeStamp > DATEADD(day,-1,GETDATE())

END

GO
/****** Object:  StoredProcedure [dbo].[getPlayTime]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[getPlayTime]
	@playerCount int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	
select AVG(Time) as Average, Min(Time) as Minimum, Max(Time) as Maximum from 
(select count(gp.id) as Players, g.Id, DateDiff(minute, TimeStarted, TimeEnded) as Time from 
Game g join gameplayer gp on g.Id = gp.GameId
group by g.Id, TimeStarted, TimeEnded
having count(gp.id) = @playerCount) x

END

GO
/****** Object:  StoredProcedure [dbo].[getRoles]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[getRoles]
	@groupName nvarchar(max)
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    select name, role from gameplayer gp
	join player p on gp.PlayerId = p.Id
	where gameid = (select max(id) from game where GroupName = @groupName)

END

GO
/****** Object:  StoredProcedure [dbo].[GlobalDay1Death]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[GlobalDay1Death]

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
select top 1 (day1death * 100 / games) as pct, p.Name, p.TelegramId from
(select day1death, victimid, count(gp.id) as games from gameplayer gp join
(select count(gameid) as day1death, VictimId from 
(select VictimId, GameId from GameKill gk
where Day = 1 and KillMethodId <> 8
group by VictimId, GameId) as x
group by victimid) as y on gp.PlayerId = y.victimid
group by victimid, day1death
having count(gp.id) > 99) as totals
join player p on p.id = totals.VictimId
order by pct desc;

END

GO
/****** Object:  StoredProcedure [dbo].[GlobalDay1Lynch]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[GlobalDay1Lynch]
	
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

select top 1 (day1death * 100 / games) as pct, p.Name, p.TelegramId from
(select day1death, victimid, count(gp.id) as games from gameplayer gp join
(select count(gameid) as day1death, VictimId from 
(select VictimId, GameId from GameKill gk
where Day = 1 and KillMethodId = 1 and KillMethodId <> 8
group by VictimId, GameId) as x
group by victimid) as y on gp.PlayerId = y.victimid
group by victimid, day1death
having count(gp.id) > 99) as totals
join player p on p.id = totals.VictimId
order by pct desc;

END

GO
/****** Object:  StoredProcedure [dbo].[GlobalNight1Death]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[GlobalNight1Death]

AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	select top 1 (deaths * 100 / games) as pct, p.Name, p.TelegramId from
(select count(gp.id) as games, victimid, deaths from gameplayer gp join
(select count(id) as deaths, VictimId from GameKill gk
where Day = 1 and KillMethodId <> 1 and KillMethodId <> 8
group by VictimId) as x on gp.PlayerId = x.victimid
group by victimid, deaths) as y
join player p on p.id = y.VictimId
where games > 99
order by pct desc

END

GO
/****** Object:  StoredProcedure [dbo].[GlobalSurvivor]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[GlobalSurvivor]
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

   select (survived * 100.0 / games) as pct, p.Name, p.TelegramId from
(select count(id) as games, sum(case survived when 1 then 1 else 0 end) as survived, playerid from gameplayer 
group by playerid 
having count(id) > 99) as x
join player p on p.id = x.playerid
order by pct desc

END

GO
/****** Object:  StoredProcedure [dbo].[GroupDay1Death]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[GroupDay1Death]
	-- Add the parameters for the stored procedure here
	@groupid bigint
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	select top 1 (day1death * 100 / games) as pct, p.Name, p.TelegramId from
(select day1death, victimid, count(gp.id) as games from gameplayer gp join
(select count(gameid) as day1death, VictimId from 
(select VictimId, GameId from GameKill gk
where Day = 1 and KillMethodId <> 8
group by VictimId, GameId) as x
join game g on g.id = gameid
where g.GroupId = @groupid
group by victimid) as y on gp.PlayerId = y.victimid
group by victimid, day1death
having count(gp.id) > 19) as totals
join player p on p.Id = totals.VictimId
order by pct desc;

END

GO
/****** Object:  StoredProcedure [dbo].[GroupDay1Lynch]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[GroupDay1Lynch]
	-- Add the parameters for the stored procedure here
	@groupid bigint
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
select top 1 (day1death * 100 / games) as pct, p.Name, p.TelegramId from
(select day1death, victimid, count(gp.id) as games from gameplayer gp join
(select count(gameid) as day1death, VictimId from 
(select VictimId, GameId from GameKill gk
where Day = 1 and KillMethodId = 1 and KillMethodId <> 8
group by VictimId, GameId) as x
join game g on g.id = gameid
where g.GroupId = @groupid
group by victimid) as y on gp.PlayerId = y.victimid
group by victimid, day1death
having count(gp.id) > 19) as totals
join player p on p.id = totals.VictimId
order by pct desc;

END

GO
/****** Object:  StoredProcedure [dbo].[GroupNight1Death]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[GroupNight1Death]
	-- Add the parameters for the stored procedure here
	@groupid bigint
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
select top 1 (day1death * 100 / games) as pct, p.Name, p.TelegramId from
(select day1death, victimid, count(gp.id) as games from gameplayer gp join
(select count(gameid) as day1death, VictimId from 
(select VictimId, GameId from GameKill gk
where Day = 1 and KillMethodId <> 1 and KillMethodId <> 8
group by VictimId, GameId) as x
join game g on g.id = gameid
where g.GroupId = @groupid
group by victimid) as y on gp.PlayerId = y.victimid
group by victimid, day1death
having count(gp.id) > 19) as totals
join player p on p.Id = totals.victimid
order by pct desc;

END

GO
/****** Object:  StoredProcedure [dbo].[GroupSurvivor]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[GroupSurvivor]
	@groupid bigint
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	select top 1 (survived * 100 / games) as pct, p.Name, p.TelegramId from
(select count(gp.id) as games, sum(case survived when 1 then 1 else 0 end) as survived, gp.playerid from gameplayer gp
join game g on gp.gameid = g.id
where g.GroupId = @groupid
group by gp.playerid
having count(gp.id) > 19) as x
join player p on x.playerid = p.id
order by pct desc
END

GO
/****** Object:  StoredProcedure [dbo].[PlayerMostKilled]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[PlayerMostKilled]
	-- Add the parameters for the stored procedure here
	@pid int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;
select top 1 p.Name, p.TelegramId, count(gk.id) as times from gamekill gk
join player p on gk.VictimId = p.Id
join player p2 on gk.KillerId = p2.Id
where p2.TelegramId = @pid and gk.KillMethodId <> 8
group by p.Name, p.TelegramId
order by count(gk.id) desc
END

GO
/****** Object:  StoredProcedure [dbo].[PlayerMostKilledBy]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[PlayerMostKilledBy]
	-- Add the parameters for the stored procedure here
	@pid int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	select p.Name, p.TelegramId, count(gk.id) as times from gamekill gk
join player p on gk.KillerId = p.Id
join player p2 on gk.VictimId = p2.Id
where p2.TelegramId = @pid and gk.KillMethodId <> 8
group by p.Name, p.TelegramId
order by count(gk.id) desc

END

GO
/****** Object:  StoredProcedure [dbo].[PlayerRoles]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[PlayerRoles]
	-- Add the parameters for the stored procedure here
	@pid int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
	select count(gp.id) as times, role from gameplayer gp
	join player p on p.id = gp.playerid
where p.TelegramId = @pid
group by role
order by count(gp.id) desc

END

GO
/****** Object:  StoredProcedure [dbo].[RestoreAccount]    Script Date: 9/6/2016 4:18:24 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[RestoreAccount]
	-- Add the parameters for the stored procedure here
@oldTGId int,
@newTGId int
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
	SET NOCOUNT ON;

    -- Insert statements for procedure here
--RESTORE ACCOUNT SCRIPT--

declare @oldId int;
declare @newId int;
declare @games int;
--get old and new database ids
set @oldId = (select max(id) from player where telegramid = @oldTGId);
set @newId = (select max(id) from player where telegramid = @newTGId);

--move old gameplayers to new player id
update gameplayer set playerid = @newId where playerid = @oldId;  --restore gameplayers

--copy achievements from old player to new player
update player set Achievements = Achievements | (select Achievements from player where id = @oldId) where id = @newId;

--view the results
set @games = (select games from (select p.id, p.Name, count(gp.id) as games from player p inner join gameplayer gp on p.id = gp.PlayerId where telegramid = @newId group by p.id, p.Name) as g)
return @games;

END

GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "Group (dbo)"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 136
               Right = 244
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
      Begin ColumnWidths = 9
         Width = 284
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 12
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_BotInGroups'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_BotInGroups'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "x"
            Begin Extent = 
               Top = 6
               Left = 249
               Bottom = 102
               Right = 419
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "p"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 136
               Right = 211
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
      Begin ColumnWidths = 9
         Width = 284
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_InactivePlayersMain'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_InactivePlayersMain'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "Group (dbo)"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 136
               Right = 244
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
      Begin ColumnWidths = 9
         Width = 284
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 12
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_LanguageCounts'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_LanguageCounts'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "Group (dbo)"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 136
               Right = 228
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
      Begin ColumnWidths = 17
         Width = 284
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 18
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
         Or = 1350
         Or = 1350
         Or = 1350
         Or = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_NonDefaultGroups'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_NonDefaultGroups'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "Group (dbo)"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 136
               Right = 228
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
      Begin ColumnWidths = 9
         Width = 284
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_PreferredGroups'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_PreferredGroups'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[41] 4[20] 2[11] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "Group (dbo)"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 136
               Right = 228
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
      Begin ColumnWidths = 9
         Width = 284
         Width = 1500
         Width = 5445
         Width = 1500
         Width = 1500
         Width = 4860
         Width = 1500
         Width = 1500
         Width = 1500
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_PublicGroups'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_PublicGroups'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_SummaryTotals'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_SummaryTotals'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
         Begin Table = "n"
            Begin Extent = 
               Top = 6
               Left = 38
               Bottom = 102
               Right = 208
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "g"
            Begin Extent = 
               Top = 6
               Left = 246
               Bottom = 136
               Right = 436
            End
            DisplayFlags = 280
            TopColumn = 0
         End
         Begin Table = "p"
            Begin Extent = 
               Top = 6
               Left = 474
               Bottom = 136
               Right = 644
            End
            DisplayFlags = 280
            TopColumn = 0
         End
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
      Begin ColumnWidths = 9
         Width = 284
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
         Width = 1500
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_WaitList'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_WaitList'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPane1', @value=N'[0E232FF0-B466-11cf-A24F-00AA00A3EFFF, 1.00]
Begin DesignProperties = 
   Begin PaneConfigurations = 
      Begin PaneConfiguration = 0
         NumPanes = 4
         Configuration = "(H (1[40] 4[20] 2[20] 3) )"
      End
      Begin PaneConfiguration = 1
         NumPanes = 3
         Configuration = "(H (1 [50] 4 [25] 3))"
      End
      Begin PaneConfiguration = 2
         NumPanes = 3
         Configuration = "(H (1 [50] 2 [25] 3))"
      End
      Begin PaneConfiguration = 3
         NumPanes = 3
         Configuration = "(H (4 [30] 2 [40] 3))"
      End
      Begin PaneConfiguration = 4
         NumPanes = 2
         Configuration = "(H (1 [56] 3))"
      End
      Begin PaneConfiguration = 5
         NumPanes = 2
         Configuration = "(H (2 [66] 3))"
      End
      Begin PaneConfiguration = 6
         NumPanes = 2
         Configuration = "(H (4 [50] 3))"
      End
      Begin PaneConfiguration = 7
         NumPanes = 1
         Configuration = "(V (3))"
      End
      Begin PaneConfiguration = 8
         NumPanes = 3
         Configuration = "(H (1[56] 4[18] 2) )"
      End
      Begin PaneConfiguration = 9
         NumPanes = 2
         Configuration = "(H (1 [75] 4))"
      End
      Begin PaneConfiguration = 10
         NumPanes = 2
         Configuration = "(H (1[66] 2) )"
      End
      Begin PaneConfiguration = 11
         NumPanes = 2
         Configuration = "(H (4 [60] 2))"
      End
      Begin PaneConfiguration = 12
         NumPanes = 1
         Configuration = "(H (1) )"
      End
      Begin PaneConfiguration = 13
         NumPanes = 1
         Configuration = "(V (4))"
      End
      Begin PaneConfiguration = 14
         NumPanes = 1
         Configuration = "(V (2))"
      End
      ActivePaneConfig = 0
   End
   Begin DiagramPane = 
      Begin Origin = 
         Top = 0
         Left = 0
      End
      Begin Tables = 
      End
   End
   Begin SQLPane = 
   End
   Begin DataPane = 
      Begin ParameterDefaults = ""
      End
   End
   Begin CriteriaPane = 
      Begin ColumnWidths = 11
         Column = 1440
         Alias = 900
         Table = 1170
         Output = 720
         Append = 1400
         NewValue = 1170
         SortType = 1350
         SortOrder = 1410
         GroupBy = 1350
         Filter = 1350
         Or = 1350
         Or = 1350
         Or = 1350
      End
   End
End
' , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_WinRatios'
GO
EXEC sys.sp_addextendedproperty @name=N'MS_DiagramPaneCount', @value=1 , @level0type=N'SCHEMA',@level0name=N'db_owner', @level1type=N'VIEW',@level1name=N'v_WinRatios'
GO
