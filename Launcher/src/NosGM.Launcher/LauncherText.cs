// SPDX-License-Identifier: MIT

using System.Globalization;

namespace NosGM.Launcher;

internal sealed record LanguageOption(string Code, string DisplayName);

internal static class LauncherTextKeys
{
    public const string Subtitle = nameof(Subtitle);
    public const string Language = nameof(Language);
    public const string Installation = nameof(Installation);
    public const string OpenFolder = nameof(OpenFolder);
    public const string ChannelStatus = nameof(ChannelStatus);
    public const string ChannelConfigured = nameof(ChannelConfigured);
    public const string ChannelDisabled = nameof(ChannelDisabled);
    public const string RecoveryCompleted = nameof(RecoveryCompleted);
    public const string RecoveryDetail = nameof(RecoveryDetail);
    public const string Ready = nameof(Ready);
    public const string SafeBase = nameof(SafeBase);
    public const string ReadyDetail = nameof(ReadyDetail);
    public const string DisabledDetail = nameof(DisabledDetail);
    public const string Import = nameof(Import);
    public const string Check = nameof(Check);
    public const string Repair = nameof(Repair);
    public const string Play = nameof(Play);
    public const string ImportTitle = nameof(ImportTitle);
    public const string ImportMessage = nameof(ImportMessage);
    public const string Analyzing = nameof(Analyzing);
    public const string Imported = nameof(Imported);
    public const string ImportDetail = nameof(ImportDetail);
    public const string Checking = nameof(Checking);
    public const string Repairing = nameof(Repairing);
    public const string GameStarted = nameof(GameStarted);
    public const string GameStartedDetail = nameof(GameStartedDetail);
    public const string UpToDate = nameof(UpToDate);
    public const string AllFilesMatch = nameof(AllFilesMatch);
    public const string IgnoredDeletes = nameof(IgnoredDeletes);
    public const string UpdateAvailable = nameof(UpdateAvailable);
    public const string UpdateAvailableDetail = nameof(UpdateAvailableDetail);
    public const string UpdateCompleted = nameof(UpdateCompleted);
    public const string UpdateCompletedDetail = nameof(UpdateCompletedDetail);
    public const string Cancelled = nameof(Cancelled);
    public const string Failed = nameof(Failed);
    public const string PhaseScan = nameof(PhaseScan);
    public const string PhaseDownload = nameof(PhaseDownload);
    public const string PhaseCommit = nameof(PhaseCommit);
    public const string PhaseComplete = nameof(PhaseComplete);
    public const string PhaseRecovery = nameof(PhaseRecovery);
    public const string PhaseImport = nameof(PhaseImport);
}

internal static class LauncherText
{
    public static IReadOnlyList<LanguageOption> Languages { get; } =
    [
        new("es", "Español"),
        new("en", "English"),
        new("de", "Deutsch"),
        new("fr", "Français"),
        new("it", "Italiano"),
        new("pl", "Polski"),
        new("cz", "Čeština"),
        new("ru", "Русский"),
        new("jp", "日本語"),
        new("cn", "中文")
    ];

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Catalogs =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["es"] = Catalog(
                (LauncherTextKeys.Subtitle, "Actualizaciones firmadas, recuperación transaccional y lanzamiento sin privilegios administrativos."),
                (LauncherTextKeys.Language, "Idioma:"),
                (LauncherTextKeys.Installation, "Instalación:"),
                (LauncherTextKeys.OpenFolder, "Abrir carpeta"),
                (LauncherTextKeys.ChannelStatus, "Estado del canal: "),
                (LauncherTextKeys.ChannelConfigured, "configurado con clave {0}"),
                (LauncherTextKeys.ChannelDisabled, "desactivado hasta fijar HTTPS, keyId y clave pública"),
                (LauncherTextKeys.RecoveryCompleted, "Recuperación automática completada"),
                (LauncherTextKeys.RecoveryDetail, "Se procesaron {0} transacciones pendientes sin tocar archivos ajenos."),
                (LauncherTextKeys.Ready, "Listo para comprobar actualizaciones"),
                (LauncherTextKeys.SafeBase, "Base segura instalada"),
                (LauncherTextKeys.ReadyDetail, "El launcher verificará la firma del manifiesto antes de leer cualquier ruta de actualización."),
                (LauncherTextKeys.DisabledDetail, "La interfaz no descargará nada mientras el canal conserve los valores de ejemplo."),
                (LauncherTextKeys.Import, "Importar instalación"),
                (LauncherTextKeys.Check, "Comprobar"),
                (LauncherTextKeys.Repair, "Reparar"),
                (LauncherTextKeys.Play, "Jugar"),
                (LauncherTextKeys.ImportTitle, "Importar instalación existente"),
                (LauncherTextKeys.ImportMessage, "La importación registrará únicamente archivos existentes cuyas rutas aparezcan en el manifiesto firmado. Los archivos distintos podrán reemplazarse posteriormente al pulsar Reparar. Los archivos extra no se tocarán.\n\n¿Continuar?"),
                (LauncherTextKeys.Analyzing, "Analizando instalación existente..."),
                (LauncherTextKeys.Imported, "Instalación importada"),
                (LauncherTextKeys.ImportDetail, "{0} archivos administrados: {1} correctos, {2} para reparar y {3} ausentes."),
                (LauncherTextKeys.Checking, "Comprobando instalación..."),
                (LauncherTextKeys.Repairing, "Verificando y reparando..."),
                (LauncherTextKeys.GameStarted, "Juego iniciado"),
                (LauncherTextKeys.GameStartedDetail, "NosGM inició el cliente sin solicitar elevación administrativa."),
                (LauncherTextKeys.UpToDate, "La instalación está actualizada"),
                (LauncherTextKeys.AllFilesMatch, "Todos los archivos firmados coinciden con el manifiesto."),
                (LauncherTextKeys.IgnoredDeletes, "Se ignoraron {0} eliminaciones no administradas."),
                (LauncherTextKeys.UpdateAvailable, "Actualización disponible"),
                (LauncherTextKeys.UpdateAvailableDetail, "{0} archivos, {1:N0} bytes y {2} eliminaciones administradas."),
                (LauncherTextKeys.UpdateCompleted, "Actualización completada"),
                (LauncherTextKeys.UpdateCompletedDetail, "Versión {0}; {1} archivos instalados."),
                (LauncherTextKeys.Cancelled, "Operación cancelada"),
                (LauncherTextKeys.Failed, "No se pudo completar la operación"),
                (LauncherTextKeys.PhaseScan, "Comprobando"),
                (LauncherTextKeys.PhaseDownload, "Descargando"),
                (LauncherTextKeys.PhaseCommit, "Aplicando"),
                (LauncherTextKeys.PhaseComplete, "Completado"),
                (LauncherTextKeys.PhaseRecovery, "Recuperando"),
                (LauncherTextKeys.PhaseImport, "Importando")),
            ["en"] = Catalog(
                (LauncherTextKeys.Subtitle, "Signed updates, transactional recovery, and launch without administrator privileges."),
                (LauncherTextKeys.Language, "Language:"),
                (LauncherTextKeys.Installation, "Installation:"),
                (LauncherTextKeys.OpenFolder, "Open folder"),
                (LauncherTextKeys.ChannelStatus, "Channel status: "),
                (LauncherTextKeys.ChannelConfigured, "configured with key {0}"),
                (LauncherTextKeys.ChannelDisabled, "disabled until HTTPS, keyId, and public key are pinned"),
                (LauncherTextKeys.RecoveryCompleted, "Automatic recovery completed"),
                (LauncherTextKeys.RecoveryDetail, "Processed {0} pending transactions without touching unrelated files."),
                (LauncherTextKeys.Ready, "Ready to check for updates"),
                (LauncherTextKeys.SafeBase, "Secure foundation installed"),
                (LauncherTextKeys.ReadyDetail, "The launcher verifies the manifest signature before reading any update path."),
                (LauncherTextKeys.DisabledDetail, "Nothing will be downloaded while the channel keeps its placeholder values."),
                (LauncherTextKeys.Import, "Import installation"),
                (LauncherTextKeys.Check, "Check"),
                (LauncherTextKeys.Repair, "Repair"),
                (LauncherTextKeys.Play, "Play"),
                (LauncherTextKeys.ImportTitle, "Import existing installation"),
                (LauncherTextKeys.ImportMessage, "Import registers only existing files whose paths appear in the signed manifest. Different files may later be replaced by Repair. Extra files are not touched.\n\nContinue?"),
                (LauncherTextKeys.Analyzing, "Analyzing existing installation..."),
                (LauncherTextKeys.Imported, "Installation imported"),
                (LauncherTextKeys.ImportDetail, "{0} managed files: {1} correct, {2} to repair, and {3} missing."),
                (LauncherTextKeys.Checking, "Checking installation..."),
                (LauncherTextKeys.Repairing, "Verifying and repairing..."),
                (LauncherTextKeys.GameStarted, "Game started"),
                (LauncherTextKeys.GameStartedDetail, "NosGM started the client without requesting administrator elevation."),
                (LauncherTextKeys.UpToDate, "The installation is up to date"),
                (LauncherTextKeys.AllFilesMatch, "All signed files match the manifest."),
                (LauncherTextKeys.IgnoredDeletes, "Ignored {0} unmanaged deletions."),
                (LauncherTextKeys.UpdateAvailable, "Update available"),
                (LauncherTextKeys.UpdateAvailableDetail, "{0} files, {1:N0} bytes, and {2} managed deletions."),
                (LauncherTextKeys.UpdateCompleted, "Update completed"),
                (LauncherTextKeys.UpdateCompletedDetail, "Version {0}; {1} files installed."),
                (LauncherTextKeys.Cancelled, "Operation cancelled"),
                (LauncherTextKeys.Failed, "The operation could not be completed"),
                (LauncherTextKeys.PhaseScan, "Checking"),
                (LauncherTextKeys.PhaseDownload, "Downloading"),
                (LauncherTextKeys.PhaseCommit, "Applying"),
                (LauncherTextKeys.PhaseComplete, "Complete"),
                (LauncherTextKeys.PhaseRecovery, "Recovering"),
                (LauncherTextKeys.PhaseImport, "Importing")),
            ["de"] = Catalog(
                (LauncherTextKeys.Subtitle, "Signierte Updates, transaktionale Wiederherstellung und Start ohne Administratorrechte."),
                (LauncherTextKeys.Language, "Sprache:"),
                (LauncherTextKeys.Installation, "Installation:"),
                (LauncherTextKeys.OpenFolder, "Ordner öffnen"),
                (LauncherTextKeys.ChannelStatus, "Kanalstatus: "),
                (LauncherTextKeys.ChannelConfigured, "mit Schlüssel {0} konfiguriert"),
                (LauncherTextKeys.ChannelDisabled, "deaktiviert, bis HTTPS, keyId und öffentlicher Schlüssel festgelegt sind"),
                (LauncherTextKeys.RecoveryCompleted, "Automatische Wiederherstellung abgeschlossen"),
                (LauncherTextKeys.RecoveryDetail, "{0} ausstehende Transaktionen wurden verarbeitet, ohne fremde Dateien zu ändern."),
                (LauncherTextKeys.Ready, "Bereit zur Updateprüfung"),
                (LauncherTextKeys.SafeBase, "Sichere Grundlage installiert"),
                (LauncherTextKeys.ReadyDetail, "Der Launcher prüft die Manifestsignatur, bevor Updatepfade gelesen werden."),
                (LauncherTextKeys.DisabledDetail, "Solange der Kanal Platzhalterwerte enthält, wird nichts heruntergeladen."),
                (LauncherTextKeys.Import, "Installation importieren"),
                (LauncherTextKeys.Check, "Prüfen"),
                (LauncherTextKeys.Repair, "Reparieren"),
                (LauncherTextKeys.Play, "Spielen"),
                (LauncherTextKeys.ImportTitle, "Vorhandene Installation importieren"),
                (LauncherTextKeys.ImportMessage, "Es werden nur vorhandene Dateien registriert, deren Pfade im signierten Manifest stehen. Abweichende Dateien können später durch Reparieren ersetzt werden. Zusätzliche Dateien bleiben unberührt.\n\nFortfahren?"),
                (LauncherTextKeys.Analyzing, "Vorhandene Installation wird analysiert..."),
                (LauncherTextKeys.Imported, "Installation importiert"),
                (LauncherTextKeys.ImportDetail, "{0} verwaltete Dateien: {1} korrekt, {2} zu reparieren und {3} fehlen."),
                (LauncherTextKeys.Checking, "Installation wird geprüft..."),
                (LauncherTextKeys.Repairing, "Prüfung und Reparatur..."),
                (LauncherTextKeys.GameStarted, "Spiel gestartet"),
                (LauncherTextKeys.GameStartedDetail, "NosGM hat den Client ohne Administratorrechte gestartet."),
                (LauncherTextKeys.UpToDate, "Die Installation ist aktuell"),
                (LauncherTextKeys.AllFilesMatch, "Alle signierten Dateien entsprechen dem Manifest."),
                (LauncherTextKeys.IgnoredDeletes, "{0} nicht verwaltete Löschungen wurden ignoriert."),
                (LauncherTextKeys.UpdateAvailable, "Update verfügbar"),
                (LauncherTextKeys.UpdateAvailableDetail, "{0} Dateien, {1:N0} Bytes und {2} verwaltete Löschungen."),
                (LauncherTextKeys.UpdateCompleted, "Update abgeschlossen"),
                (LauncherTextKeys.UpdateCompletedDetail, "Version {0}; {1} Dateien installiert."),
                (LauncherTextKeys.Cancelled, "Vorgang abgebrochen"),
                (LauncherTextKeys.Failed, "Der Vorgang konnte nicht abgeschlossen werden"),
                (LauncherTextKeys.PhaseScan, "Prüfen"),
                (LauncherTextKeys.PhaseDownload, "Herunterladen"),
                (LauncherTextKeys.PhaseCommit, "Anwenden"),
                (LauncherTextKeys.PhaseComplete, "Abgeschlossen"),
                (LauncherTextKeys.PhaseRecovery, "Wiederherstellen"),
                (LauncherTextKeys.PhaseImport, "Importieren")),
            ["fr"] = Catalog(
                (LauncherTextKeys.Subtitle, "Mises à jour signées, récupération transactionnelle et lancement sans privilèges administrateur."),
                (LauncherTextKeys.Language, "Langue :"),
                (LauncherTextKeys.Installation, "Installation :"),
                (LauncherTextKeys.OpenFolder, "Ouvrir le dossier"),
                (LauncherTextKeys.ChannelStatus, "État du canal : "),
                (LauncherTextKeys.ChannelConfigured, "configuré avec la clé {0}"),
                (LauncherTextKeys.ChannelDisabled, "désactivé jusqu’à la configuration de HTTPS, keyId et de la clé publique"),
                (LauncherTextKeys.RecoveryCompleted, "Récupération automatique terminée"),
                (LauncherTextKeys.RecoveryDetail, "{0} transactions en attente ont été traitées sans toucher aux fichiers étrangers."),
                (LauncherTextKeys.Ready, "Prêt à rechercher les mises à jour"),
                (LauncherTextKeys.SafeBase, "Base sécurisée installée"),
                (LauncherTextKeys.ReadyDetail, "Le launcher vérifie la signature du manifeste avant de lire les chemins de mise à jour."),
                (LauncherTextKeys.DisabledDetail, "Aucun téléchargement n’aura lieu tant que le canal conserve ses valeurs d’exemple."),
                (LauncherTextKeys.Import, "Importer l’installation"),
                (LauncherTextKeys.Check, "Vérifier"),
                (LauncherTextKeys.Repair, "Réparer"),
                (LauncherTextKeys.Play, "Jouer"),
                (LauncherTextKeys.ImportTitle, "Importer une installation existante"),
                (LauncherTextKeys.ImportMessage, "L’importation enregistre uniquement les fichiers existants dont les chemins figurent dans le manifeste signé. Les fichiers différents pourront être remplacés ensuite par Réparer. Les fichiers supplémentaires restent intacts.\n\nContinuer ?"),
                (LauncherTextKeys.Analyzing, "Analyse de l’installation existante..."),
                (LauncherTextKeys.Imported, "Installation importée"),
                (LauncherTextKeys.ImportDetail, "{0} fichiers gérés : {1} corrects, {2} à réparer et {3} absents."),
                (LauncherTextKeys.Checking, "Vérification de l’installation..."),
                (LauncherTextKeys.Repairing, "Vérification et réparation..."),
                (LauncherTextKeys.GameStarted, "Jeu lancé"),
                (LauncherTextKeys.GameStartedDetail, "NosGM a lancé le client sans demander de droits administrateur."),
                (LauncherTextKeys.UpToDate, "L’installation est à jour"),
                (LauncherTextKeys.AllFilesMatch, "Tous les fichiers signés correspondent au manifeste."),
                (LauncherTextKeys.IgnoredDeletes, "{0} suppressions non gérées ont été ignorées."),
                (LauncherTextKeys.UpdateAvailable, "Mise à jour disponible"),
                (LauncherTextKeys.UpdateAvailableDetail, "{0} fichiers, {1:N0} octets et {2} suppressions gérées."),
                (LauncherTextKeys.UpdateCompleted, "Mise à jour terminée"),
                (LauncherTextKeys.UpdateCompletedDetail, "Version {0} ; {1} fichiers installés."),
                (LauncherTextKeys.Cancelled, "Opération annulée"),
                (LauncherTextKeys.Failed, "L’opération n’a pas pu être terminée"),
                (LauncherTextKeys.PhaseScan, "Vérification"),
                (LauncherTextKeys.PhaseDownload, "Téléchargement"),
                (LauncherTextKeys.PhaseCommit, "Application"),
                (LauncherTextKeys.PhaseComplete, "Terminé"),
                (LauncherTextKeys.PhaseRecovery, "Récupération"),
                (LauncherTextKeys.PhaseImport, "Importation")),
            ["it"] = Catalog(
                (LauncherTextKeys.Subtitle, "Aggiornamenti firmati, ripristino transazionale e avvio senza privilegi amministrativi."),
                (LauncherTextKeys.Language, "Lingua:"),
                (LauncherTextKeys.Installation, "Installazione:"),
                (LauncherTextKeys.OpenFolder, "Apri cartella"),
                (LauncherTextKeys.ChannelStatus, "Stato canale: "),
                (LauncherTextKeys.ChannelConfigured, "configurato con chiave {0}"),
                (LauncherTextKeys.ChannelDisabled, "disattivato finché HTTPS, keyId e chiave pubblica non sono configurati"),
                (LauncherTextKeys.RecoveryCompleted, "Ripristino automatico completato"),
                (LauncherTextKeys.RecoveryDetail, "Elaborate {0} transazioni in sospeso senza modificare file estranei."),
                (LauncherTextKeys.Ready, "Pronto a controllare gli aggiornamenti"),
                (LauncherTextKeys.SafeBase, "Base sicura installata"),
                (LauncherTextKeys.ReadyDetail, "Il launcher verifica la firma del manifesto prima di leggere i percorsi di aggiornamento."),
                (LauncherTextKeys.DisabledDetail, "Non verrà scaricato nulla finché il canale mantiene i valori di esempio."),
                (LauncherTextKeys.Import, "Importa installazione"),
                (LauncherTextKeys.Check, "Controlla"),
                (LauncherTextKeys.Repair, "Ripara"),
                (LauncherTextKeys.Play, "Gioca"),
                (LauncherTextKeys.ImportTitle, "Importa installazione esistente"),
                (LauncherTextKeys.ImportMessage, "L’importazione registra solo i file esistenti i cui percorsi compaiono nel manifesto firmato. I file diversi potranno essere sostituiti in seguito con Ripara. I file extra non verranno toccati.\n\nContinuare?"),
                (LauncherTextKeys.Analyzing, "Analisi dell’installazione esistente..."),
                (LauncherTextKeys.Imported, "Installazione importata"),
                (LauncherTextKeys.ImportDetail, "{0} file gestiti: {1} corretti, {2} da riparare e {3} mancanti."),
                (LauncherTextKeys.Checking, "Controllo installazione..."),
                (LauncherTextKeys.Repairing, "Verifica e riparazione..."),
                (LauncherTextKeys.GameStarted, "Gioco avviato"),
                (LauncherTextKeys.GameStartedDetail, "NosGM ha avviato il client senza richiedere privilegi amministrativi."),
                (LauncherTextKeys.UpToDate, "L’installazione è aggiornata"),
                (LauncherTextKeys.AllFilesMatch, "Tutti i file firmati corrispondono al manifesto."),
                (LauncherTextKeys.IgnoredDeletes, "Ignorate {0} eliminazioni non gestite."),
                (LauncherTextKeys.UpdateAvailable, "Aggiornamento disponibile"),
                (LauncherTextKeys.UpdateAvailableDetail, "{0} file, {1:N0} byte e {2} eliminazioni gestite."),
                (LauncherTextKeys.UpdateCompleted, "Aggiornamento completato"),
                (LauncherTextKeys.UpdateCompletedDetail, "Versione {0}; {1} file installati."),
                (LauncherTextKeys.Cancelled, "Operazione annullata"),
                (LauncherTextKeys.Failed, "Impossibile completare l’operazione"),
                (LauncherTextKeys.PhaseScan, "Controllo"),
                (LauncherTextKeys.PhaseDownload, "Download"),
                (LauncherTextKeys.PhaseCommit, "Applicazione"),
                (LauncherTextKeys.PhaseComplete, "Completato"),
                (LauncherTextKeys.PhaseRecovery, "Ripristino"),
                (LauncherTextKeys.PhaseImport, "Importazione")),
            ["pl"] = Catalog(
                (LauncherTextKeys.Subtitle, "Podpisane aktualizacje, odzyskiwanie transakcyjne i uruchamianie bez uprawnień administratora."),
                (LauncherTextKeys.Language, "Język:"),
                (LauncherTextKeys.Installation, "Instalacja:"),
                (LauncherTextKeys.OpenFolder, "Otwórz folder"),
                (LauncherTextKeys.ChannelStatus, "Stan kanału: "),
                (LauncherTextKeys.ChannelConfigured, "skonfigurowany z kluczem {0}"),
                (LauncherTextKeys.ChannelDisabled, "wyłączony do czasu ustawienia HTTPS, keyId i klucza publicznego"),
                (LauncherTextKeys.RecoveryCompleted, "Automatyczne odzyskiwanie zakończone"),
                (LauncherTextKeys.RecoveryDetail, "Przetworzono {0} oczekujących transakcji bez zmiany obcych plików."),
                (LauncherTextKeys.Ready, "Gotowy do sprawdzenia aktualizacji"),
                (LauncherTextKeys.SafeBase, "Bezpieczna podstawa zainstalowana"),
                (LauncherTextKeys.ReadyDetail, "Launcher sprawdza podpis manifestu przed odczytaniem ścieżek aktualizacji."),
                (LauncherTextKeys.DisabledDetail, "Nic nie zostanie pobrane, dopóki kanał używa wartości przykładowych."),
                (LauncherTextKeys.Import, "Importuj instalację"),
                (LauncherTextKeys.Check, "Sprawdź"),
                (LauncherTextKeys.Repair, "Napraw"),
                (LauncherTextKeys.Play, "Graj"),
                (LauncherTextKeys.ImportTitle, "Importuj istniejącą instalację"),
                (LauncherTextKeys.ImportMessage, "Import rejestruje tylko istniejące pliki, których ścieżki znajdują się w podpisanym manifeście. Inne pliki mogą później zostać zastąpione przez Napraw. Dodatkowe pliki pozostaną nietknięte.\n\nKontynuować?"),
                (LauncherTextKeys.Analyzing, "Analizowanie istniejącej instalacji..."),
                (LauncherTextKeys.Imported, "Instalacja zaimportowana"),
                (LauncherTextKeys.ImportDetail, "{0} zarządzanych plików: {1} poprawnych, {2} do naprawy i {3} brakujących."),
                (LauncherTextKeys.Checking, "Sprawdzanie instalacji..."),
                (LauncherTextKeys.Repairing, "Weryfikowanie i naprawianie..."),
                (LauncherTextKeys.GameStarted, "Gra uruchomiona"),
                (LauncherTextKeys.GameStartedDetail, "NosGM uruchomił klienta bez żądania uprawnień administratora."),
                (LauncherTextKeys.UpToDate, "Instalacja jest aktualna"),
                (LauncherTextKeys.AllFilesMatch, "Wszystkie podpisane pliki są zgodne z manifestem."),
                (LauncherTextKeys.IgnoredDeletes, "Zignorowano {0} niezarządzanych usunięć."),
                (LauncherTextKeys.UpdateAvailable, "Dostępna aktualizacja"),
                (LauncherTextKeys.UpdateAvailableDetail, "{0} plików, {1:N0} bajtów i {2} zarządzanych usunięć."),
                (LauncherTextKeys.UpdateCompleted, "Aktualizacja zakończona"),
                (LauncherTextKeys.UpdateCompletedDetail, "Wersja {0}; zainstalowano {1} plików."),
                (LauncherTextKeys.Cancelled, "Operacja anulowana"),
                (LauncherTextKeys.Failed, "Nie udało się zakończyć operacji"),
                (LauncherTextKeys.PhaseScan, "Sprawdzanie"),
                (LauncherTextKeys.PhaseDownload, "Pobieranie"),
                (LauncherTextKeys.PhaseCommit, "Stosowanie"),
                (LauncherTextKeys.PhaseComplete, "Zakończono"),
                (LauncherTextKeys.PhaseRecovery, "Odzyskiwanie"),
                (LauncherTextKeys.PhaseImport, "Importowanie")),
            ["cz"] = Catalog(
                (LauncherTextKeys.Subtitle, "Podepsané aktualizace, transakční obnova a spuštění bez oprávnění správce."),
                (LauncherTextKeys.Language, "Jazyk:"),
                (LauncherTextKeys.Installation, "Instalace:"),
                (LauncherTextKeys.OpenFolder, "Otevřít složku"),
                (LauncherTextKeys.ChannelStatus, "Stav kanálu: "),
                (LauncherTextKeys.ChannelConfigured, "nakonfigurováno s klíčem {0}"),
                (LauncherTextKeys.ChannelDisabled, "vypnuto, dokud nebudou nastaveny HTTPS, keyId a veřejný klíč"),
                (LauncherTextKeys.RecoveryCompleted, "Automatická obnova dokončena"),
                (LauncherTextKeys.RecoveryDetail, "Zpracováno {0} čekajících transakcí bez zásahu do cizích souborů."),
                (LauncherTextKeys.Ready, "Připraveno ke kontrole aktualizací"),
                (LauncherTextKeys.SafeBase, "Bezpečný základ nainstalován"),
                (LauncherTextKeys.ReadyDetail, "Launcher ověří podpis manifestu před načtením cest aktualizace."),
                (LauncherTextKeys.DisabledDetail, "Dokud kanál používá ukázkové hodnoty, nic se nestáhne."),
                (LauncherTextKeys.Import, "Importovat instalaci"),
                (LauncherTextKeys.Check, "Zkontrolovat"),
                (LauncherTextKeys.Repair, "Opravit"),
                (LauncherTextKeys.Play, "Hrát"),
                (LauncherTextKeys.ImportTitle, "Importovat existující instalaci"),
                (LauncherTextKeys.ImportMessage, "Import zaregistruje pouze existující soubory, jejichž cesty jsou v podepsaném manifestu. Odlišné soubory lze později nahradit pomocí Opravit. Další soubory zůstanou nedotčené.\n\nPokračovat?"),
                (LauncherTextKeys.Analyzing, "Analýza existující instalace..."),
                (LauncherTextKeys.Imported, "Instalace importována"),
                (LauncherTextKeys.ImportDetail, "{0} spravovaných souborů: {1} správných, {2} k opravě a {3} chybí."),
                (LauncherTextKeys.Checking, "Kontrola instalace..."),
                (LauncherTextKeys.Repairing, "Ověřování a oprava..."),
                (LauncherTextKeys.GameStarted, "Hra spuštěna"),
                (LauncherTextKeys.GameStartedDetail, "NosGM spustil klienta bez požadavku na oprávnění správce."),
                (LauncherTextKeys.UpToDate, "Instalace je aktuální"),
                (LauncherTextKeys.AllFilesMatch, "Všechny podepsané soubory odpovídají manifestu."),
                (LauncherTextKeys.IgnoredDeletes, "Ignorováno {0} nespravovaných odstranění."),
                (LauncherTextKeys.UpdateAvailable, "Je dostupná aktualizace"),
                (LauncherTextKeys.UpdateAvailableDetail, "{0} souborů, {1:N0} bajtů a {2} spravovaných odstranění."),
                (LauncherTextKeys.UpdateCompleted, "Aktualizace dokončena"),
                (LauncherTextKeys.UpdateCompletedDetail, "Verze {0}; nainstalováno {1} souborů."),
                (LauncherTextKeys.Cancelled, "Operace zrušena"),
                (LauncherTextKeys.Failed, "Operaci se nepodařilo dokončit"),
                (LauncherTextKeys.PhaseScan, "Kontrola"),
                (LauncherTextKeys.PhaseDownload, "Stahování"),
                (LauncherTextKeys.PhaseCommit, "Použití"),
                (LauncherTextKeys.PhaseComplete, "Dokončeno"),
                (LauncherTextKeys.PhaseRecovery, "Obnova"),
                (LauncherTextKeys.PhaseImport, "Import")),
            ["ru"] = Catalog(
                (LauncherTextKeys.Subtitle, "Подписанные обновления, транзакционное восстановление и запуск без прав администратора."),
                (LauncherTextKeys.Language, "Язык:"),
                (LauncherTextKeys.Installation, "Установка:"),
                (LauncherTextKeys.OpenFolder, "Открыть папку"),
                (LauncherTextKeys.ChannelStatus, "Состояние канала: "),
                (LauncherTextKeys.ChannelConfigured, "настроен с ключом {0}"),
                (LauncherTextKeys.ChannelDisabled, "отключён до настройки HTTPS, keyId и открытого ключа"),
                (LauncherTextKeys.RecoveryCompleted, "Автоматическое восстановление завершено"),
                (LauncherTextKeys.RecoveryDetail, "Обработано ожидающих транзакций: {0}; посторонние файлы не затронуты."),
                (LauncherTextKeys.Ready, "Готово к проверке обновлений"),
                (LauncherTextKeys.SafeBase, "Безопасная основа установлена"),
                (LauncherTextKeys.ReadyDetail, "Лаунчер проверяет подпись манифеста до чтения путей обновления."),
                (LauncherTextKeys.DisabledDetail, "Пока канал содержит тестовые значения, загрузка не выполняется."),
                (LauncherTextKeys.Import, "Импорт установки"),
                (LauncherTextKeys.Check, "Проверить"),
                (LauncherTextKeys.Repair, "Исправить"),
                (LauncherTextKeys.Play, "Играть"),
                (LauncherTextKeys.ImportTitle, "Импорт существующей установки"),
                (LauncherTextKeys.ImportMessage, "Импорт зарегистрирует только существующие файлы, пути которых указаны в подписанном манифесте. Отличающиеся файлы позже можно заменить через Исправить. Дополнительные файлы не затрагиваются.\n\nПродолжить?"),
                (LauncherTextKeys.Analyzing, "Анализ существующей установки..."),
                (LauncherTextKeys.Imported, "Установка импортирована"),
                (LauncherTextKeys.ImportDetail, "Управляемых файлов: {0}; корректных: {1}, требуют исправления: {2}, отсутствуют: {3}."),
                (LauncherTextKeys.Checking, "Проверка установки..."),
                (LauncherTextKeys.Repairing, "Проверка и исправление..."),
                (LauncherTextKeys.GameStarted, "Игра запущена"),
                (LauncherTextKeys.GameStartedDetail, "NosGM запустил клиент без запроса прав администратора."),
                (LauncherTextKeys.UpToDate, "Установка актуальна"),
                (LauncherTextKeys.AllFilesMatch, "Все подписанные файлы соответствуют манифесту."),
                (LauncherTextKeys.IgnoredDeletes, "Игнорировано неуправляемых удалений: {0}."),
                (LauncherTextKeys.UpdateAvailable, "Доступно обновление"),
                (LauncherTextKeys.UpdateAvailableDetail, "Файлов: {0}, байт: {1:N0}, управляемых удалений: {2}."),
                (LauncherTextKeys.UpdateCompleted, "Обновление завершено"),
                (LauncherTextKeys.UpdateCompletedDetail, "Версия {0}; установлено файлов: {1}."),
                (LauncherTextKeys.Cancelled, "Операция отменена"),
                (LauncherTextKeys.Failed, "Не удалось завершить операцию"),
                (LauncherTextKeys.PhaseScan, "Проверка"),
                (LauncherTextKeys.PhaseDownload, "Загрузка"),
                (LauncherTextKeys.PhaseCommit, "Применение"),
                (LauncherTextKeys.PhaseComplete, "Завершено"),
                (LauncherTextKeys.PhaseRecovery, "Восстановление"),
                (LauncherTextKeys.PhaseImport, "Импорт")),
            ["jp"] = Catalog(
                (LauncherTextKeys.Subtitle, "署名付き更新、トランザクション復旧、管理者権限なしでの起動。"),
                (LauncherTextKeys.Language, "言語:"),
                (LauncherTextKeys.Installation, "インストール先:"),
                (LauncherTextKeys.OpenFolder, "フォルダーを開く"),
                (LauncherTextKeys.ChannelStatus, "チャンネル状態: "),
                (LauncherTextKeys.ChannelConfigured, "キー {0} で設定済み"),
                (LauncherTextKeys.ChannelDisabled, "HTTPS、keyId、公開鍵が設定されるまで無効"),
                (LauncherTextKeys.RecoveryCompleted, "自動復旧が完了しました"),
                (LauncherTextKeys.RecoveryDetail, "保留中のトランザクション {0} 件を、無関係なファイルに触れず処理しました。"),
                (LauncherTextKeys.Ready, "更新を確認できます"),
                (LauncherTextKeys.SafeBase, "安全な基盤がインストールされました"),
                (LauncherTextKeys.ReadyDetail, "ランチャーは更新パスを読む前にマニフェストの署名を検証します。"),
                (LauncherTextKeys.DisabledDetail, "チャンネルがサンプル値の間は何もダウンロードしません。"),
                (LauncherTextKeys.Import, "インストールを取り込む"),
                (LauncherTextKeys.Check, "確認"),
                (LauncherTextKeys.Repair, "修復"),
                (LauncherTextKeys.Play, "プレイ"),
                (LauncherTextKeys.ImportTitle, "既存インストールの取り込み"),
                (LauncherTextKeys.ImportMessage, "署名付きマニフェストにあるパスの既存ファイルだけを登録します。異なるファイルは後で修復により置き換えられます。追加ファイルには触れません。\n\n続行しますか？"),
                (LauncherTextKeys.Analyzing, "既存インストールを解析中..."),
                (LauncherTextKeys.Imported, "インストールを取り込みました"),
                (LauncherTextKeys.ImportDetail, "管理対象 {0} 件: 正常 {1}、修復対象 {2}、不足 {3}。"),
                (LauncherTextKeys.Checking, "インストールを確認中..."),
                (LauncherTextKeys.Repairing, "検証と修復を実行中..."),
                (LauncherTextKeys.GameStarted, "ゲームを起動しました"),
                (LauncherTextKeys.GameStartedDetail, "NosGM は管理者権限を要求せずクライアントを起動しました。"),
                (LauncherTextKeys.UpToDate, "インストールは最新です"),
                (LauncherTextKeys.AllFilesMatch, "すべての署名付きファイルがマニフェストと一致します。"),
                (LauncherTextKeys.IgnoredDeletes, "管理対象外の削除 {0} 件を無視しました。"),
                (LauncherTextKeys.UpdateAvailable, "更新があります"),
                (LauncherTextKeys.UpdateAvailableDetail, "ファイル {0} 件、{1:N0} バイト、管理対象削除 {2} 件。"),
                (LauncherTextKeys.UpdateCompleted, "更新が完了しました"),
                (LauncherTextKeys.UpdateCompletedDetail, "バージョン {0}; {1} ファイルをインストールしました。"),
                (LauncherTextKeys.Cancelled, "操作をキャンセルしました"),
                (LauncherTextKeys.Failed, "操作を完了できませんでした"),
                (LauncherTextKeys.PhaseScan, "確認中"),
                (LauncherTextKeys.PhaseDownload, "ダウンロード中"),
                (LauncherTextKeys.PhaseCommit, "適用中"),
                (LauncherTextKeys.PhaseComplete, "完了"),
                (LauncherTextKeys.PhaseRecovery, "復旧中"),
                (LauncherTextKeys.PhaseImport, "取り込み中")),
            ["cn"] = Catalog(
                (LauncherTextKeys.Subtitle, "签名更新、事务恢复，并以非管理员权限启动。"),
                (LauncherTextKeys.Language, "语言："),
                (LauncherTextKeys.Installation, "安装目录："),
                (LauncherTextKeys.OpenFolder, "打开文件夹"),
                (LauncherTextKeys.ChannelStatus, "更新通道："),
                (LauncherTextKeys.ChannelConfigured, "已使用密钥 {0} 配置"),
                (LauncherTextKeys.ChannelDisabled, "在配置 HTTPS、keyId 和公钥之前保持禁用"),
                (LauncherTextKeys.RecoveryCompleted, "自动恢复已完成"),
                (LauncherTextKeys.RecoveryDetail, "已处理 {0} 个待完成事务，未触碰无关文件。"),
                (LauncherTextKeys.Ready, "可以检查更新"),
                (LauncherTextKeys.SafeBase, "安全基础已安装"),
                (LauncherTextKeys.ReadyDetail, "启动器会在读取任何更新路径之前验证清单签名。"),
                (LauncherTextKeys.DisabledDetail, "更新通道仍使用示例值时不会下载任何内容。"),
                (LauncherTextKeys.Import, "导入安装"),
                (LauncherTextKeys.Check, "检查"),
                (LauncherTextKeys.Repair, "修复"),
                (LauncherTextKeys.Play, "开始游戏"),
                (LauncherTextKeys.ImportTitle, "导入现有安装"),
                (LauncherTextKeys.ImportMessage, "仅登记已存在且路径出现在签名清单中的文件。不同的文件之后可通过“修复”替换。额外文件不会被触碰。\n\n继续吗？"),
                (LauncherTextKeys.Analyzing, "正在分析现有安装..."),
                (LauncherTextKeys.Imported, "安装已导入"),
                (LauncherTextKeys.ImportDetail, "管理文件 {0} 个：正确 {1} 个，待修复 {2} 个，缺失 {3} 个。"),
                (LauncherTextKeys.Checking, "正在检查安装..."),
                (LauncherTextKeys.Repairing, "正在验证并修复..."),
                (LauncherTextKeys.GameStarted, "游戏已启动"),
                (LauncherTextKeys.GameStartedDetail, "NosGM 已在不请求管理员权限的情况下启动客户端。"),
                (LauncherTextKeys.UpToDate, "安装已是最新版本"),
                (LauncherTextKeys.AllFilesMatch, "所有签名文件均与清单一致。"),
                (LauncherTextKeys.IgnoredDeletes, "已忽略 {0} 个非管理删除请求。"),
                (LauncherTextKeys.UpdateAvailable, "有可用更新"),
                (LauncherTextKeys.UpdateAvailableDetail, "{0} 个文件，{1:N0} 字节，{2} 个管理删除。"),
                (LauncherTextKeys.UpdateCompleted, "更新完成"),
                (LauncherTextKeys.UpdateCompletedDetail, "版本 {0}；已安装 {1} 个文件。"),
                (LauncherTextKeys.Cancelled, "操作已取消"),
                (LauncherTextKeys.Failed, "无法完成操作"),
                (LauncherTextKeys.PhaseScan, "检查中"),
                (LauncherTextKeys.PhaseDownload, "下载中"),
                (LauncherTextKeys.PhaseCommit, "应用中"),
                (LauncherTextKeys.PhaseComplete, "已完成"),
                (LauncherTextKeys.PhaseRecovery, "恢复中"),
                (LauncherTextKeys.PhaseImport, "导入中"))
        };

    public static void ValidateCatalogs()
    {
        var expected = Catalogs["en"].Keys.ToHashSet(StringComparer.Ordinal);
        foreach (var language in Languages)
        {
            if (!Catalogs.TryGetValue(language.Code, out var catalog) ||
                catalog.Count != expected.Count ||
                expected.Any(key => !catalog.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value)))
            {
                throw new InvalidDataException($"Launcher language catalog '{language.Code}' is incomplete.");
            }
        }
    }

    public static string Get(string language, string key)
    {
        var catalog = Catalogs.TryGetValue(language, out var selected)
            ? selected
            : Catalogs["en"];
        return catalog.TryGetValue(key, out var value)
            ? value
            : Catalogs["en"][key];
    }

    public static string Format(string language, string key, params object?[] arguments)
        => string.Format(Culture(language), Get(language, key), arguments);

    public static string Phase(string language, string phase)
        => phase switch
        {
            "scan" => Get(language, LauncherTextKeys.PhaseScan),
            "download" => Get(language, LauncherTextKeys.PhaseDownload),
            "commit" => Get(language, LauncherTextKeys.PhaseCommit),
            "complete" => Get(language, LauncherTextKeys.PhaseComplete),
            "recovery" => Get(language, LauncherTextKeys.PhaseRecovery),
            "import" => Get(language, LauncherTextKeys.PhaseImport),
            _ => phase
        };

    private static IReadOnlyDictionary<string, string> Catalog(
        params (string Key, string Value)[] entries)
        => entries.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

    private static CultureInfo Culture(string language)
        => CultureInfo.GetCultureInfo(language.ToLowerInvariant() switch
        {
            "es" => "es-ES",
            "de" => "de-DE",
            "fr" => "fr-FR",
            "it" => "it-IT",
            "pl" => "pl-PL",
            "cz" => "cs-CZ",
            "ru" => "ru-RU",
            "jp" => "ja-JP",
            "cn" => "zh-CN",
            _ => "en-US"
        });
}
