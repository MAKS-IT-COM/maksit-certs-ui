@{
    RootModule        = 'MaksIT.CertsUI.Client.PowerShell.dll'
    ModuleVersion      = '1.0.0'
    GUID               = 'b2c3d4e5-f6a7-4890-b123-456789abcdef'
    Author             = 'MaksIT'
    CompanyName        = 'MaksIT'
    Description        = 'PowerShell cmdlets for MaksIT CertsUI API. Use Connect-CertsUI to set base address and API key, then Get-CertsUIAccounts, Get-CertsUIAccount, Test-CertsUIHealth, etc.'
    PowerShellVersion  = '7.0'
    RequiredModules    = @()
    RequiredAssemblies = @('MaksIT.CertsUI.Client.PowerShell.dll')
    FunctionsToExport  = @()
    CmdletsToExport    = @(
        'Connect-CertsUI',
        'Disconnect-CertsUI',
        'Test-CertsUIHealth',
        'Get-CertsUIAccounts',
        'Get-CertsUIAccount',
        'Get-CertsUIRuntimeInstanceId',
        'Invoke-CertsUICreateAccount',
        'Invoke-CertsUIPatchAccount',
        'Invoke-CertsUIDeleteAccount'
    )
    VariablesToExport  = @()
    AliasesToExport     = @()
    PrivateData         = @{
        PSData = @{
            Tags       = @('CertsUI', 'MaksIT', 'ACME', 'API')
            LicenseUri = ''
            ProjectUri = ''
        }
    }
}
