#!/bin/pwsh
if ($env:OS -eq 'Windows_NT') {
    $env:FSCPATH = [System.IO.Directory]::GetParent("$((get-command fsc).Source)").ToString()
} else {
    $env:FSCPATH = './'
}

dotnet $args
