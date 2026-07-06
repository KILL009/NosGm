INSERT INTO Raidbox (IsRareRandom, ItemGeneratedAmount, ItemGeneratedDesign, ItemGeneratedVNum, MaximumOriginalItemRare, MinimumOriginalItemRare, OriginalItemDesign, OriginalItemVNum, Probability)
VALUES 
    -- IsRareRandom = Sagt an, ob das generierte Item ein zufälliges Rare haben soll | ist ein bool, kann nur 0 oder 1 sein. 0 = false, 1 = true | true für Waffe und Rüstung, rest false
    -- ItemGeneratedAmount = Anzahl der Items die ihr generiert (Bei EQ, Ressi und allem was im Equipment Slot ist immer 1, niemals höher. Was nicht stackable ist, wird nicht stackable gemacht)
    -- ItemGeneratedDesign = Erstmal unwichtig für euch, es sei denn ihr wollt ein spezielles Design von Stachelhaaren oder so
    -- ItemGeneratedVNum = VNum von dem Item welches ihr generiert
    -- MaximumOriginalItemRare = Maximum Rare für das generierte Item. Für EQ unter Hero Level immer 7, denn Rare 8 gibt es für nicht-helden Equipment nicht.
    -- MinimumOriginalItemRare = Bei Hauptwaffe, Zweitwaffe und Rüstung immer 1. 
    -- OriginalItemDesign = Immer auf 0, unwichtig für euch
    -- OriginalItemVNum = Die VNum der Raidbox, die geöffnet wird
    -- Probability = Chance, je höher, desto wahrscheinlicher. 1-1000 (5-15 = Sehr schwer, 500 = absolut common. 250 = uncommon, 200 = Rare etc. etc.)

    -- Cuby --
    (1, 1, 0, 3, 0, 0, 0, 4684, 400), -- Wooden Sword
    (1, 50, 0, 1182, 0, 0, 0, 4684, 200), -- Seed of Power
    (1, 1, 0, 5560, 0, 0, 0, 4684, 20), -- Onyx Wings


    -- Ginseng --
    (1, 1, 1, 1, 1, 1, 1, 1, 1),
    (1, 1, 1, 1, 1, 1, 1, 1, 1),
    (1, 1, 1, 1, 1, 1, 1, 1, 1),

    -- Grasslin --
    (1, 1, 1, 1, 1, 1, 1, 1, 1),
    (1, 1, 1, 1, 1, 1, 1, 1, 1),
    (1, 1, 1, 1, 1, 1, 1, 1, 1);