#Requires -Version 5.1
# Console Mode - UTF-8 para console, pipeline e arquivos

function Initialize-ConsoleEncoding {
  $utf8NoBom = New-Object System.Text.UTF8Encoding $false

  try {
    [Console]::OutputEncoding = $utf8NoBom
    [Console]::InputEncoding = $utf8NoBom
  }
  catch { }

  $global:OutputEncoding = $utf8NoBom

  $PSDefaultParameterValues['Out-File:Encoding'] = 'utf8'
  $PSDefaultParameterValues['Set-Content:Encoding'] = 'utf8'
  $PSDefaultParameterValues['Add-Content:Encoding'] = 'utf8'
  $PSDefaultParameterValues['Get-Content:Encoding'] = 'utf8'
  $PSDefaultParameterValues['Export-Csv:Encoding'] = 'utf8'
  $PSDefaultParameterValues['Import-Csv:Encoding'] = 'utf8'
  $PSDefaultParameterValues['ConvertFrom-Csv:Encoding'] = 'utf8'
}
