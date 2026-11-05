import subprocess

ps_cmd = 'Get-Process Unity | Select-Object Id, ProcessName, @{Name="CommandLine";Expression={(Get-CimInstance Win32_Process -Filter "ProcessId = $($_.Id)").CommandLine}} | Format-List'

res = subprocess.run(["powershell", "-NoProfile", "-Command", ps_cmd], capture_output=True, text=True)
print(res.stdout)
