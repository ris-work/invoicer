New-PSDrive -PSProvider registry -Root HKEY_CLASSES_ROOT -Name HKCR
mkdir 'HKCU:\software\microsoft\windows\CurrentVersion\App Paths\HealthMonitorLogViewer.exe'
$a = @{
    Path = 'HKCU:\software\microsoft\windows\CurrentVersion\App Paths\HealthMonitorLogViewer.exe'
    Name = "(default)"
    PropertyType = "String"
    Value = ""
}
New-ItemProperty @a

mkdir 'HKCR:\Applications\HealthMonitorLogViewer.exe'
$a = @{
    Path = 'HKCR:\Applications\HealthMonitorLogViewer.exe\HealthMonitorLogViewer.exe'
    Name = "(default)"
    PropertyType = "String"
    Value = ""
}
New-ItemProperty -Force @a

mkdir 'HKCR:\Applications\HealthMonitorLogViewer.exe\DefaultIcon'
$a = @{
    Path = 'HKCR:\Applications\HealthMonitorLogViewer.exe\DefaultIcon\(default)'
    Name = "(default)"
    PropertyType = "String"
    Value = "$pwd\cd_file.ico"
}
New-ItemProperty -Force @a

mkdir 'HKCR:\Applications\HealthMonitorLogViewer.exe\SupportedTypes'
$a = @{
    Path = 'HKCR:\Applications\HealthMonitorLogViewer.exe\SupportedTypes\'
    Name = ".rvhealthmonitorlogfile"
    PropertyType = "String"
    Value = ""
}
New-ItemProperty -Force @a


mkdir 'HKCU:\Software\Classes\Applications\HealthMonitorLogViewer.exe'
$a = @{
    Path = 'HKCU:\Software\Classes\Applications\HealthMonitorLogViewer.exe\HealthMonitorLogViewer.exe'
    Name = "(default)"
    PropertyType = "String"
    Value = ""
    #Value = "$pwd\HealthMonitorLogViewer.exe"
}
New-ItemProperty -Force @a

mkdir 'HKCU:\Software\Classes\Applications\HealthMonitorLogViewer.exe\DefaultIcon'
$a = @{
    Path = 'HKCU:\Software\Classes\Applications\HealthMonitorLogViewer.exe\DefaultIcon\'
    Name = "(default)"
    PropertyType = "String"
    Value = "$pwd\cd_file.ico"
}
New-ItemProperty -Force @a

mkdir 'HKCU:\Software\Classes\Applications\HealthMonitorLogViewer.exe\SupportedTypes'
$a = @{
    Path = 'HKCU:\Software\Classes\Applications\HealthMonitorLogViewer.exe\SupportedTypes\'
    Name = ".rvhealthmonitorlogfile"
    PropertyType = "String"
    Value = ""
}
New-ItemProperty -Force @a
mkdir 'HKCU:\Software\Classes\Applications\HealthMonitorLogViewer.exe\shell'
mkdir 'HKCU:\Software\Classes\Applications\HealthMonitorLogViewer.exe\shell\open'
mkdir 'HKCU:\Software\Classes\Applications\HealthMonitorLogViewer.exe\shell\open\command'

$a = @{
    Path = 'HKCU:\Software\Classes\Applications\HealthMonitorLogViewer.exe\shell\open\command'
    Name = "(default)"
    PropertyType = "String"
    Value = "`"$pwd\HealthMonitorLogViewer.exe`" `"%1`""
}
New-ItemProperty -Force @a


mkdir 'HKCU:\Software\Classes\.rvhealthmonitorlogfile'
mkdir 'HKCU:\Software\Classes\.rvhealthmonitorlogfile\shell'
mkdir 'HKCU:\Software\Classes\.rvhealthmonitorlogfile\shell\open'
mkdir 'HKCU:\Software\Classes\.rvhealthmonitorlogfile\shell\open\command'

$a = @{
    Path = 'HKCU:\Software\Classes\.rvhealthmonitorlogfile\shell\open\command'
    Name = "(default)"
    PropertyType = "String"
    Value = "`"$pwd\HealthMonitorLogViewer.exe`" `"%1`""
}
New-ItemProperty -Force @a

$a = @{
    Path = 'HKCU:\Software\Classes\.rvhealthmonitorlogfile\'
    Name = "(default)"
    PropertyType = "String"
    Value = "HealthMonitorLogViewer"
}
New-ItemProperty -Force @a


mkdir 'HKCU:\Software\Classes\HealthMonitorLogViewer'
$a = @{
    Path = 'HKCU:\Software\Classes\HealthMonitorLogViewer\'
    Name = "(default)"
    PropertyType = "String"
    Value = "Health Monitor Log File"
    #Value = "`"$pwd\HealthMonitorLogViewer.exe`" `"%1`""
}
New-ItemProperty -Force @a

mkdir 'HKCU:\Software\Classes\.rvhealthmonitorlogfile\DefaultIcon'
$a = @{
    Path = 'HKCU:\Software\Classes\.rvhealthmonitorlogfile\DefaultIcon\'
    Name = "(default)"
    PropertyType = "String"
    Value = "$pwd\cd_file.ico"
}
New-ItemProperty -Force @a


mkdir 'HKCU:\Software\Classes\HealthMonitorLogViewer\DefaultIcon'
$a = @{
    Path = 'HKCU:\Software\Classes\HealthMonitorLogViewer\DefaultIcon\'
    Name = "(default)"
    PropertyType = "String"
    Value = "$pwd\cd_file.ico"
}
New-ItemProperty -Force @a

mkdir 'HKCU:\Software\Classes\HealthMonitorLogViewer\shell'
mkdir 'HKCU:\Software\Classes\HealthMonitorLogViewer\shell\open'
mkdir 'HKCU:\Software\Classes\HealthMonitorLogViewer\shell\open\command'

$a = @{
    Path = 'HKCU:\Software\Classes\HealthMonitorLogViewer\shell\open\command'
    Name = "(default)"
    PropertyType = "String"
    Value = "`"$pwd\HealthMonitorLogViewer.exe`" `"%1`""
}
New-ItemProperty -Force @a