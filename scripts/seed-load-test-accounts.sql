/*
NosGM local load-test seed
Creates up to 1,000 deterministic test accounts, one starter character per account,
and the three starter Adventurer skills used by normal character creation.

Safe to re-run:
- missing accounts are inserted
- existing matching accounts are normalized to the load-test password and loopback IP
- missing slot-0 characters are inserted
- missing starter skills are inserted

The seed intentionally does not depend on optional Account columns such as Language,
so it can run against older NosGM databases as well as freshly migrated databases.
Temporary text columns explicitly use DATABASE_DEFAULT to avoid tempdb/database
collation conflicts.

Run this against the NosGM SQL Server database while the local stack is stopped
or before a load-test run.

Default credentials:
  load0001 .. load1000
  password: NosGM_Load_2026!
  character slot: 0

IMPORTANT: These are disposable local test credentials. Do not expose this seed
on an Internet-facing production database.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Count int = 1000;
DECLARE @AccountPrefix varchar(16) = 'load';
DECLARE @CharacterPrefix varchar(16) = 'LoadC';
DECLARE @PlainPassword varchar(64) = 'NosGM_Load_2026!';
DECLARE @PasswordHash varchar(255) =
    'nosgm$pbkdf2-sha256$v1$600000$qjgmftRU+71jlsw1heIH6Q==$D3Di2pzO4UhWMtprgkI2WOlBYt0/5iyI2+xFkHjM7j0=';
DECLARE @Loopback varchar(45) = '127.0.0.1';

IF @Count < 1 OR @Count > 9999
    THROW 50000, '@Count must be between 1 and 9999.', 1;

IF OBJECT_ID(N'dbo.Account', N'U') IS NULL
    THROW 50001, 'dbo.Account was not found. Select the NosGM database first.', 1;

IF OBJECT_ID(N'dbo.Character', N'U') IS NULL
    THROW 50002, 'dbo.Character was not found. Select the NosGM database first.', 1;

IF OBJECT_ID(N'dbo.CharacterSkill', N'U') IS NULL
    THROW 50003, 'dbo.CharacterSkill was not found. Select the NosGM database first.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.Map WHERE MapId = 1)
    THROW 50004, 'MapId 1 is missing. Import NosGM world data before seeding load-test characters.', 1;

IF EXISTS
(
    SELECT required.SkillVNum
    FROM (VALUES (CAST(200 AS smallint)), (CAST(201 AS smallint)), (CAST(209 AS smallint)))
        AS required(SkillVNum)
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM dbo.Skill AS s
        WHERE s.SkillVNum = required.SkillVNum
    )
)
    THROW 50005, 'Starter skills 200, 201 and 209 must exist before seeding.', 1;

CREATE TABLE #LoadUsers
(
    SeedNumber int NOT NULL PRIMARY KEY,
    Username varchar(255) COLLATE DATABASE_DEFAULT NOT NULL,
    CharacterName varchar(255) COLLATE DATABASE_DEFAULT NOT NULL
);

;WITH Numbers AS
(
    SELECT TOP (@Count)
        ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) AS SeedNumber
    FROM sys.all_objects AS a
    CROSS JOIN sys.all_objects AS b
)
INSERT #LoadUsers (SeedNumber, Username, CharacterName)
SELECT
    SeedNumber,
    @AccountPrefix + RIGHT('0000' + CONVERT(varchar(4), SeedNumber), 4),
    @CharacterPrefix + RIGHT('0000' + CONVERT(varchar(4), SeedNumber), 4)
FROM Numbers;

BEGIN TRANSACTION;

INSERT dbo.Account
(
    Authority,
    Email,
    Name,
    Password,
    RegistrationIP,
    VerificationToken
)
SELECT
    0,
    u.Username + '@loadtest.local',
    u.Username,
    @PasswordHash,
    @Loopback,
    NULL
FROM #LoadUsers AS u
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Account AS a
    WHERE a.Name = u.Username
);

-- Keep re-runs deterministic without touching non-load-test accounts.
UPDATE a
SET
    a.Authority = 0,
    a.Email = u.Username + '@loadtest.local',
    a.Password = @PasswordHash,
    a.RegistrationIP = @Loopback
FROM dbo.Account AS a
INNER JOIN #LoadUsers AS u
    ON u.Username = a.Name;

INSERT dbo.Character
(
    AccountId,
    Act4Dead,
    Act4Kill,
    Act4Points,
    ArenaWinner,
    Biography,
    BuffBlocked,
    Class,
    Compliment,
    Dignity,
    EmoticonsBlocked,
    ExchangeBlocked,
    Faction,
    FamilyRequestBlocked,
    FriendRequestBlocked,
    Gender,
    Gold,
    GoldBank,
    GroupRequestBlocked,
    HairColor,
    HairStyle,
    HeroChatBlocked,
    HeroLevel,
    HeroXp,
    Hp,
    HpBlocked,
    IsPartnerAutoRelive,
    IsPetAutoRelive,
    IsSeal,
    JobLevel,
    JobLevelXp,
    LastFamilyLeave,
    Level,
    LevelXp,
    MapId,
    MapX,
    MapY,
    MasterPoints,
    MasterTicket,
    MaxMateCount,
    MaxPartnerCount,
    MinilandInviteBlocked,
    MinilandMessage,
    MinilandPoint,
    MinilandState,
    MouseAimLock,
    Mp,
    Name,
    QuickGetUp,
    RagePoint,
    Reputation,
    Slot,
    SpAdditionPoint,
    SpPoint,
    State,
    TalentLose,
    TalentSurrender,
    TalentWin,
    ArenaKill,
    ArenaDeath,
    WhisperBlocked,
    HideHat,
    UiBlocked,
    TrophyCount,
    Trophy1,
    Trophy2,
    Trophy3,
    Trophy4,
    Trophy5,
    Trophy6,
    Trophy7,
    Trophy8,
    Trophy9,
    Trophy10,
    Trophy11,
    Trophy12,
    Trophy13,
    Trophy14,
    Trophy15,
    LegendaryTrophy,
    MasteryXp,
    MasteryLevel,
    RaidCount,
    MonsterCount,
    MysteryBoxCount,
    BattlePassPoints,
    HasPremiumBattlePass,
    UnlockedBattlePassMultiplicator,
    BuffCharge,
    LimitedBuffCharge,
    Stage,
    PrimalCharacterQuest,
    PrimalRaidQuest,
    PrimalFamilyQuest,
    PrimalCharacterQuestProgress,
    PrimalRaidQuestProgress,
    PrimalFamilyQuestProgress,
    PrimalQuestCount,
    DailyRewardChest,
    AutoLoot,
    SafeBet,
    DuelWon,
    DuelLost,
    DuelCount,
    CurrentIp,
    StarterBoxUsed,
    InstanceMapId,
    InstanceMapX,
    InstanceMapY,
    PityCount,
    Icon,
    MiniPet,
    PetSkill1,
    PetSkill2
)
SELECT
    a.AccountId,
    0,
    0,
    0,
    0,
    NULL,
    0,
    0,
    0,
    0.0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    221,
    0,
    1,
    1,
    0,
    1,
    0,
    0,
    1,
    0,
    1,
    80,
    115,
    0,
    0,
    10,
    4,
    0,
    N'NosGM load test',
    2000,
    0,
    0,
    69,
    u.CharacterName,
    0,
    0,
    0,
    0,
    0,
    10000,
    1,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    @Loopback,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0,
    0
FROM #LoadUsers AS u
INNER JOIN dbo.Account AS a
    ON a.Name = u.Username
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Character AS c
    WHERE c.AccountId = a.AccountId
      AND c.Slot = 0
);

;WITH StarterSkills AS
(
    SELECT CAST(200 AS smallint) AS SkillVNum
    UNION ALL SELECT CAST(201 AS smallint)
    UNION ALL SELECT CAST(209 AS smallint)
)
INSERT dbo.CharacterSkill
(
    Id,
    CharacterId,
    SkillVNum,
    IsTattoo,
    TattooLevel,
    IsPartnerSkill
)
SELECT
    NEWID(),
    c.CharacterId,
    s.SkillVNum,
    0,
    0,
    0
FROM #LoadUsers AS u
INNER JOIN dbo.Account AS a
    ON a.Name = u.Username
INNER JOIN dbo.Character AS c
    ON c.AccountId = a.AccountId
   AND c.Slot = 0
CROSS JOIN StarterSkills AS s
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.CharacterSkill AS existing
    WHERE existing.CharacterId = c.CharacterId
      AND existing.SkillVNum = s.SkillVNum
);

COMMIT TRANSACTION;

DECLARE @SeededAccounts int =
(
    SELECT COUNT(*)
    FROM dbo.Account AS a
    INNER JOIN #LoadUsers AS u ON u.Username = a.Name
);

DECLARE @SeededCharacters int =
(
    SELECT COUNT(*)
    FROM dbo.Character AS c
    INNER JOIN dbo.Account AS a ON a.AccountId = c.AccountId
    INNER JOIN #LoadUsers AS u ON u.Username = a.Name
    WHERE c.Slot = 0
);

DECLARE @SeededSkills int =
(
    SELECT COUNT(*)
    FROM dbo.CharacterSkill AS cs
    INNER JOIN dbo.Character AS c ON c.CharacterId = cs.CharacterId
    INNER JOIN dbo.Account AS a ON a.AccountId = c.AccountId
    INNER JOIN #LoadUsers AS u ON u.Username = a.Name
    WHERE c.Slot = 0
      AND cs.SkillVNum IN (200, 201, 209)
);

SELECT
    DB_NAME() AS DatabaseName,
    @SeededAccounts AS SeededAccounts,
    @SeededCharacters AS SeededCharacters,
    @SeededSkills AS StarterSkillRows,
    @PlainPassword AS LoadTestPassword;

-- Convenient preview matching NosGM.LoadTest accounts.csv.
SELECT
    u.Username AS username,
    @PlainPassword AS password,
    CAST(0 AS tinyint) AS slot
FROM #LoadUsers AS u
ORDER BY u.SeedNumber;

DROP TABLE #LoadUsers;
