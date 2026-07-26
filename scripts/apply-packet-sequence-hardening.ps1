param(
    [string]$Path = "Data/NosGm.GameObject/Networking/ClientSession.cs"
)

$ErrorActionPreference = "Stop"

function Replace-Required {
    param(
        [string]$InputText,
        [string]$OldText,
        [string]$NewText,
        [string]$Description
    )

    if (-not $InputText.Contains($OldText)) {
        throw "Required source block was not found: $Description"
    }

    return $InputText.Replace($OldText, $NewText)
}

if (-not (Test-Path -LiteralPath $Path)) {
    throw "ClientSession source not found: $Path"
}

$content = [System.IO.File]::ReadAllText($Path)

$content = Replace-Required $content @'
            return $"Account: {Account.Name}";
        }

        public void Initialize
'@ @'
            return $"Session: {SessionId} ClientId: {ClientId}";
        }

        public void Initialize
'@ "null-safe session identity"

$content = Replace-Required $content @'
            TriggerHandler(header, $"{_lastPacketId} {packet}", false, ignoreAuthority);
            _lastPacketId++;
        }
'@ @'
            TriggerHandler(header, $"{_lastPacketId} {packet}", false, ignoreAuthority);
            _lastPacketId = _lastPacketId >= ushort.MaxValue ? 0 : _lastPacketId + 1;
        }
'@ "injected packet counter wrap"

$content = Replace-Required $content @'
            catch (Exception)
            {
                PacketIngressMonitor.RecordError(metricGeneration);
                Disconnect();
            }
'@ @'
            catch (Exception ex)
            {
                PacketIngressMonitor.RecordError(metricGeneration);
                Logger.Error(
                    $"[PACKET_INGRESS_DRAIN_FAILED] SessionId={SessionId} ClientId={ClientId} Processed={processed}",
                    ex);
                Disconnect();
            }
'@ "packet drain exception diagnostics"

$content = Replace-Required $content @'
                if (!int.TryParse(sessionParts[0], out int packetId))
                {
                    Disconnect();
                    return false;
                }
                _lastPacketId = packetId;
'@ @'
                if (!ushort.TryParse(sessionParts[0], out ushort packetId))
                {
                    Logger.Warn(
                        $"[SESSION_PACKET_ID_REJECTED] SessionId={SessionId} ClientId={ClientId} " +
                        $"Received={FormatPacketIdForLog(sessionParts[0])}");
                    Disconnect();
                    return false;
                }
                _lastPacketId = packetId;
'@ "initial packet id validation"

$content = Replace-Required $content @'
                if (_encryptor.HasCustomParameter)
                {
                    var nextRawPacketId = packetsplit[0];
                    if (!int.TryParse(nextRawPacketId, out var nextPacketId) && nextPacketId != _lastPacketId + 1)
                    {
                        //LOGGERServerLog($"KeepAlive was corrupt. Removed Session", LogType.ServerError);
                        _client.Disconnect();
                        return false;
                    }

                    if (nextPacketId == 0)
                    {
                        if (_lastPacketId == ushort.MaxValue)
                        {
                            _lastPacketId = nextPacketId;
                        }
                    }
                    else
                    {
                        _lastPacketId = nextPacketId;
                    }

                    if (_waitForPacketsAmount.HasValue)
'@ @'
                if (_encryptor.HasCustomParameter)
                {
                    if (packetsplit.Length < 2 || string.IsNullOrWhiteSpace(packetsplit[0]))
                    {
                        Logger.Warn(
                            $"[PACKET_SEQUENCE_REJECTED] SessionId={SessionId} ClientId={ClientId} " +
                            "Reason=MissingPacketId");
                        Disconnect();
                        return false;
                    }

                    string nextRawPacketId = packetsplit[0];
                    if (!TryAdvancePacketSequence(nextRawPacketId, out int nextPacketId, out int expectedPacketId))
                    {
                        Logger.Warn(
                            $"[PACKET_SEQUENCE_REJECTED] SessionId={SessionId} ClientId={ClientId} " +
                            $"Expected={expectedPacketId} Received={FormatPacketIdForLog(nextRawPacketId)}");
                        Disconnect();
                        return false;
                    }

                    if (_waitForPacketsAmount.HasValue)
'@ "strict packet sequence validation"

$content = Replace-Required $content @'
        /// <summary>
        /// Handles one raw network message while preserving the old packet-ordering
'@ @'
        private static string FormatPacketIdForLog(string rawPacketId)
        {
            if (string.IsNullOrEmpty(rawPacketId))
            {
                return "<empty>";
            }

            const int maximumLogLength = 16;
            return rawPacketId.Length <= maximumLogLength
                ? rawPacketId
                : rawPacketId.Substring(0, maximumLogLength) + "...";
        }

        private bool TryAdvancePacketSequence(string rawPacketId, out int packetId, out int expectedPacketId)
        {
            packetId = 0;
            expectedPacketId = _lastPacketId >= ushort.MaxValue ? 0 : _lastPacketId + 1;

            if (!ushort.TryParse(rawPacketId, out ushort parsedPacketId))
            {
                return false;
            }

            packetId = parsedPacketId;
            if (packetId != expectedPacketId)
            {
                return false;
            }

            _lastPacketId = packetId;
            return true;
        }

        /// <summary>
        /// Handles one raw network message while preserving the old packet-ordering
'@ "packet sequence helper insertion"

$content = Replace-Required $content @'
                var key = HandlerMethods.Keys.FirstOrDefault(s => s.Any(m => string.Equals(m, packetHeader, StringComparison.CurrentCultureIgnoreCase)));
'@ @'
                var key = HandlerMethods.Keys.FirstOrDefault(s =>
                    s.Any(m => string.Equals(m, packetHeader, StringComparison.OrdinalIgnoreCase)));
'@ "ordinal packet header comparison"

$content = Replace-Required $content @'
                    catch (DivideByZeroException ex)
                    {

                    }
                    catch (Exception e)
                    {

                    }
'@ @'
                    catch (DivideByZeroException ex)
                    {
                        Logger.Error(
                            $"[PACKET_HANDLER_DIVIDE_BY_ZERO] Header={packetHeader} {GenerateIdentity()}",
                            ex);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(
                            $"[PACKET_HANDLER_FAILED] Header={packetHeader} {GenerateIdentity()}",
                            ex);
                    }
'@ "packet handler exception diagnostics"

$content = Replace-Required $content @'
                    if (packetHeader.ToLower() == "$commander")
'@ @'
                    if (string.Equals(packetHeader, "$commander", StringComparison.OrdinalIgnoreCase))
'@ "ordinal commander detection"

[System.IO.File]::WriteAllText($Path, $content, [System.Text.UTF8Encoding]::new($true))
Write-Host "ClientSession packet sequence hardening applied."
