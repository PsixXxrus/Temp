$baseKey = "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList"

# Найти SID с .bak
$bak = Get-ChildItem $baseKey | Where-Object { $_.PSChildName -like "*.bak" }

foreach ($item in $bak) {
    $sidBak = $item.PSChildName
    $sid = $sidBak -replace "\.bak$", ""

    Write-Host "Обработка SID: $sid"

    # Удаляем временный профиль (без .bak)
    if (Test-Path "$baseKey\$sid") {
        Remove-Item "$baseKey\$sid" -Recurse -Force
        Write-Host "Удален временный профиль: $sid"
    }

    # Переименовываем .bak обратно
    Rename-Item "$baseKey\$sidBak" $sid
    Write-Host "Восстановлен профиль из .bak"

    # Сброс проблемных флагов
    Set-ItemProperty "$baseKey\$sid" -Name State -Value 0 -ErrorAction SilentlyContinue
    Set-ItemProperty "$baseKey\$sid" -Name RefCount -Value 0 -ErrorAction SilentlyContinue

    Write-Host "Сброшены State и RefCount"
}
