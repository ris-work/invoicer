#Privileged run
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
mkdir 'HKCR:\Applications\HealthMonitorLogViewer.exe\shell'
mkdir 'HKCR:\Applications\HealthMonitorLogViewer.exe\shell\open'
mkdir 'HKCR:\Applications\HealthMonitorLogViewer.exe\shell\open\command'

$a = @{
    Path = 'HKCR:\Applications\HealthMonitorLogViewer.exe\shell\open\command'
    Name = "(default)"
    PropertyType = "String"
    Value = "`"$pwd\HealthMonitorLogViewer.exe`" `"%1`""
}
New-ItemProperty -Force @a


mkdir 'HKCR:\.rvhealthmonitorlogfile'
mkdir 'HKCR:\.rvhealthmonitorlogfile\shell'
mkdir 'HKCR:\.rvhealthmonitorlogfile\shell\open'
mkdir 'HKCR:\.rvhealthmonitorlogfile\shell\open\command'

$a = @{
    Path = 'HKCR:\.rvhealthmonitorlogfile\shell\open\command'
    Name = "(default)"
    PropertyType = "String"
    Value = "`"$pwd\HealthMonitorLogViewer.exe`" `"%1`""
}
New-ItemProperty -Force @a

$a = @{
    Path = 'HKCR:\.rvhealthmonitorlogfile\'
    Name = "(default)"
    PropertyType = "String"
    Value = "HealthMonitorLogViewer"
}
New-ItemProperty -Force @a


mkdir 'HKCR:\HealthMonitorLogViewer'
mkdir 'HKCR:\HealthMonitorLogViewer\DefaultIcon'
mkdir 'HKCR:\HealthMonitorLogViewer\shell'
mkdir 'HKCR:\HealthMonitorLogViewer\shell\open'
mkdir 'HKCR:\HealthMonitorLogViewer\shell\open\command'


$a = @{
    Path = 'HKCR:\HealthMonitorLogViewer\'
    Name = "(default)"
    PropertyType = "String"
    Value = "Health Monitor Log File"
    #Value = "`"$pwd\HealthMonitorLogViewer.exe`" `"%1`""
}
New-ItemProperty -Force @a

$a = @{
    Path = 'HKCR:\HealthMonitorLogViewer\shell\open\command'
    Name = "(default)"
    PropertyType = "String"
    Value = "`"$pwd\HealthMonitorLogViewer.exe`" `"%1`""
}
New-ItemProperty -Force @a

$a = @{
    Path = 'HKCR:\HealthMonitorLogViewer\DefaultIcon'
    Name = "(default)"
    PropertyType = "String"
    Value = "$pwd\cd_file.ico"
}
New-ItemProperty -Force @a

mkdir 'HKCR:\.rvhealthmonitorlogfile\DefaultIcon'
$a = @{
    Path = 'HKCR:\.rvhealthmonitorlogfile\DefaultIcon\'
    Name = "(default)"
    PropertyType = "String"
    Value = "$pwd\cd_file.ico"
}
New-ItemProperty -Force @a


mkdir 'HKCR:\HealthMonitorLogViewer\DefaultIcon'
$a = @{
    Path = 'HKCR:\HealthMonitorLogViewer\DefaultIcon\'
    Name = "(default)"
    PropertyType = "String"
    Value = "$pwd\cd_file.ico"
}
New-ItemProperty -Force @a

mkdir 'HKCR:\HealthMonitorLogViewer\shell'
mkdir 'HKCR:\HealthMonitorLogViewer\shell\open'
mkdir 'HKCR:\HealthMonitorLogViewer\shell\open\command'

$a = @{
    Path = 'HKCR:\HealthMonitorLogViewer\'
    Name = "(default)"
    PropertyType = "String"
    Value = "Health Monitor Log File"
}
New-ItemProperty -Force @a

$a = @{
    Path = 'HKCU:\Software\Classes\HealthMonitorLogViewer\shell\open\command'
    Name = "(default)"
    PropertyType = "String"
    Value = "`"$pwd\HealthMonitorLogViewer.exe`" `"%1`""
}
New-ItemProperty -Force @a

#HKCU things
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
$a = @{
    Path = 'HKCU:\Software\Classes\HealthMonitorLogViewer\'
    Name = "(default)"
    PropertyType = "String"
    Value = "Health Monitor Log File"
}
New-ItemProperty -Force @a

#FILE ASSOCIATION
#LOCAL USER

mkdir 'HKCU:\Software\Classes\.rvhealthmonitorlogfile\HealthMonitorLogViewer'
mkdir 'HKCU:\Software\Classes\.rvhealthmonitorlogfile\HealthMonitorLogViewer\ShellNew'
$a = @{
    Path = 'HKCU:\Software\Classes\.rvhealthmonitorlogfile\HealthMonitorLogViewer\ShellNew'
    Name = "FileName"
    PropertyType = "String"
    Value = "$pwd\new.logs.sqlite3.rvhealthmonitorlogfile"
}
New-ItemProperty -Force @a

#SYSTEM

mkdir 'HKCR:\.rvhealthmonitorlogfile\HealthMonitorLogViewer'
mkdir 'HKCR:\.rvhealthmonitorlogfile\HealthMonitorLogViewer\ShellNew'
$a = @{
    Path = 'HKCR:\Software\Classes\.rvhealthmonitorlogfile\HealthMonitorLogViewer\ShellNew'
    Name = "FileName"
    PropertyType = "String"
    Value = "$pwd\new.logs.sqlite3.rvhealthmonitorlogfile"
}
New-ItemProperty -Force @a

#Control panel things
$guid = Get-Content "guid.guid"
mkdir "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace\$guid"
mkdir "HKCR:\CLSID\$guid"

$a = @{
    Path = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\ControlPanel\NameSpace\$guid"
    Name = "(default)"
    PropertyType = "String"
    Value = "Health Monitor Log Viewer"
}
New-ItemProperty -Force @a

$a = @{
    Path = "HKCR:\CLSID\$guid"
    Name = "LocalizedString"
    PropertyType = "String"
    Value = "Health Monitor Log Viewer"
}
New-ItemProperty -Force @a

$a = @{
    Path = "HKCR:\CLSID\$guid"
    Name = "InfoTip"
    PropertyType = "String"
    Value = "Health Monitor Log Viewer"
}
New-ItemProperty -Force @a

$a = @{
    Path = "HKCR:\CLSID\$guid"
    Name = "System.ApplicationName"
    PropertyType = "String"
    Value = "HealthMonitorLogViewer"
}
New-ItemProperty -Force @a

$a = @{
    Path = "HKCR:\CLSID\$guid"
    Name = "System.ControlPanel.Category"
    PropertyType = "String"
    Value = "0,2,3,8"
}
New-ItemProperty -Force @a

mkdir "HKCR:\CLSID\$guid\DefaultIcon\"
$a = @{
    Path = "HKCR:\CLSID\$guid\DefaultIcon\"
    Name = "(default)"
    PropertyType = "String"
    Value = "$pwd\time-view.ico"
}
New-ItemProperty -Force @a


mkdir "HKCR:\CLSID\$guid\shell"
mkdir "HKCR:\CLSID\$guid\shell\open"
mkdir "HKCR:\CLSID\$guid\shell\open\command"
$a = @{
    Path = "HKCR:\CLSID\$guid\DefaultIcon\"
    Name = "(default)"
    PropertyType = "String"
    Value = "$pwd\HealthMonitorLogViewer.exe"
}
New-ItemProperty -Force @a