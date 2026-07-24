// SPDX-License-Identifier: MIT

using System.Globalization;

namespace NosGM.Launcher;

internal static class InstallFolderTextKeys
{
    public const string Browse = nameof(Browse);
    public const string SelectTitle = nameof(SelectTitle);
    public const string EmptyStatus = nameof(EmptyStatus);
    public const string EmptyDetail = nameof(EmptyDetail);
    public const string ExistingStatus = nameof(ExistingStatus);
    public const string ExistingDetail = nameof(ExistingDetail);
    public const string ManagedStatus = nameof(ManagedStatus);
    public const string ManagedDetail = nameof(ManagedDetail);
}

internal static class InstallFolderText
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Catalogs =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["es"] = Catalog("Examinar", "Seleccionar carpeta de instalación", "Carpeta vacía seleccionada", "La carpeta está lista para una instalación nueva.", "Cliente existente detectado", "Puedes importar esta instalación mediante el manifiesto firmado.", "Instalación administrada detectada", "Versión {0}; {1} archivos administrados."),
            ["en"] = Catalog("Browse", "Select installation folder", "Empty folder selected", "The folder is ready for a new installation.", "Existing client detected", "You can import this installation through the signed manifest.", "Managed installation detected", "Version {0}; {1} managed files."),
            ["de"] = Catalog("Durchsuchen", "Installationsordner auswählen", "Leerer Ordner ausgewählt", "Der Ordner ist für eine neue Installation bereit.", "Vorhandener Client erkannt", "Diese Installation kann über das signierte Manifest importiert werden.", "Verwaltete Installation erkannt", "Version {0}; {1} verwaltete Dateien."),
            ["fr"] = Catalog("Parcourir", "Sélectionner le dossier d’installation", "Dossier vide sélectionné", "Le dossier est prêt pour une nouvelle installation.", "Client existant détecté", "Vous pouvez importer cette installation avec le manifeste signé.", "Installation gérée détectée", "Version {0} ; {1} fichiers gérés."),
            ["it"] = Catalog("Sfoglia", "Seleziona cartella di installazione", "Cartella vuota selezionata", "La cartella è pronta per una nuova installazione.", "Client esistente rilevato", "Puoi importare questa installazione tramite il manifesto firmato.", "Installazione gestita rilevata", "Versione {0}; {1} file gestiti."),
            ["pl"] = Catalog("Przeglądaj", "Wybierz folder instalacji", "Wybrano pusty folder", "Folder jest gotowy na nową instalację.", "Wykryto istniejącego klienta", "Możesz zaimportować tę instalację za pomocą podpisanego manifestu.", "Wykryto zarządzaną instalację", "Wersja {0}; zarządzanych plików: {1}."),
            ["cz"] = Catalog("Procházet", "Vybrat instalační složku", "Vybrána prázdná složka", "Složka je připravena pro novou instalaci.", "Nalezen existující klient", "Tuto instalaci lze importovat pomocí podepsaného manifestu.", "Nalezena spravovaná instalace", "Verze {0}; spravovaných souborů: {1}."),
            ["ru"] = Catalog("Обзор", "Выберите папку установки", "Выбрана пустая папка", "Папка готова для новой установки.", "Обнаружен существующий клиент", "Эту установку можно импортировать через подписанный манифест.", "Обнаружена управляемая установка", "Версия {0}; управляемых файлов: {1}."),
            ["jp"] = Catalog("参照", "インストールフォルダーを選択", "空のフォルダーを選択しました", "新規インストールに使用できます。", "既存クライアントを検出しました", "署名付きマニフェストを使って取り込めます。", "管理対象インストールを検出しました", "バージョン {0}; 管理対象ファイル {1} 件。"),
            ["cn"] = Catalog("浏览", "选择安装文件夹", "已选择空文件夹", "该文件夹可用于全新安装。", "检测到现有客户端", "可以通过签名清单导入此安装。", "检测到已管理安装", "版本 {0}；管理文件 {1} 个。")
        };

    public static void ValidateCatalogs()
    {
        var expected = Catalogs["en"].Keys.ToHashSet(StringComparer.Ordinal);
        foreach (var language in LauncherText.Languages)
        {
            if (!Catalogs.TryGetValue(language.Code, out var catalog) ||
                catalog.Count != expected.Count ||
                expected.Any(key => !catalog.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value)))
            {
                throw new InvalidDataException($"Install-folder language catalog '{language.Code}' is incomplete.");
            }
        }
    }

    public static string Get(string language, string key)
    {
        var catalog = Catalogs.TryGetValue(language, out var selected) ? selected : Catalogs["en"];
        return catalog.TryGetValue(key, out var value) ? value : Catalogs["en"][key];
    }

    public static string Format(string language, string key, params object?[] arguments)
        => string.Format(CultureInfo.InvariantCulture, Get(language, key), arguments);

    private static IReadOnlyDictionary<string, string> Catalog(
        string browse,
        string selectTitle,
        string emptyStatus,
        string emptyDetail,
        string existingStatus,
        string existingDetail,
        string managedStatus,
        string managedDetail)
        => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [InstallFolderTextKeys.Browse] = browse,
            [InstallFolderTextKeys.SelectTitle] = selectTitle,
            [InstallFolderTextKeys.EmptyStatus] = emptyStatus,
            [InstallFolderTextKeys.EmptyDetail] = emptyDetail,
            [InstallFolderTextKeys.ExistingStatus] = existingStatus,
            [InstallFolderTextKeys.ExistingDetail] = existingDetail,
            [InstallFolderTextKeys.ManagedStatus] = managedStatus,
            [InstallFolderTextKeys.ManagedDetail] = managedDetail
        };
}
