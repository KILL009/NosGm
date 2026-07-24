SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.ShellEffect', N'U') IS NULL
BEGIN
    THROW 50001, 'dbo.ShellEffect does not exist.', 1;
END;

IF OBJECT_ID(N'dbo.ItemInstance', N'U') IS NULL
BEGIN
    THROW 50002, 'dbo.ItemInstance does not exist.', 1;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ShellEffect')
      AND name = N'IX_ShellEffect_EquipmentSerialId_IsRune'
)
BEGIN
    CREATE INDEX IX_ShellEffect_EquipmentSerialId_IsRune
        ON dbo.ShellEffect(EquipmentSerialId, IsRune)
        INCLUDE (ShellEffectId);
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ItemInstance')
      AND name = N'IX_ItemInstance_Character_EquipmentSerial'
)
BEGIN
    CREATE INDEX IX_ItemInstance_Character_EquipmentSerial
        ON dbo.ItemInstance(CharacterId, EquipmentSerialId)
        INCLUDE (ShellRarity);
END;
