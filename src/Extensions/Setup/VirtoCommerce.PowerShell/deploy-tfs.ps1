<#
    Script builds and deploys azure packages.

    Requirements
    1. Create new database server and set server name in settings below (make sure to add current ip to the firewall)
    2. Set-ExecutionPolicy RemoteSigned
    3. Update you subscription settings file by running Get-AzurePublishSettingsFile
    4. run the deployment

    Features
    - deploys front end
    - deploys database including sample data
#>
Param(  
  	[parameter(Mandatory=$true)]
		$subscriptionname,
        [parameter(Mandatory=$true)]
		$storageaccount,
		$region = "West US",
		$slot = "Production",
        [parameter(Mandatory=$true)]
        $search_servicename,
        [parameter(Mandatory=$true)]
        $frontend_servicename,
        [parameter(Mandatory=$true)]
        $scheduler_servicename,
        [parameter(Mandatory=$true)]
        $db_servername,
        [parameter(Mandatory=$true)]
        $db_serverlogin,
        [parameter(Mandatory=$true)]
        $db_serverpassword,
        [parameter(Mandatory=$true)]
        $db_databasename,
		$publishsettingsfile,
        $deploydatabase = "True",
        $deploysearch = "True",
        $deployfrontend = "True",
        $deployscheduler = "True",
        $deployadmin = "True",
		[parameter(Mandatory=$true)]
		$deploymentdir,
		[parameter(Mandatory=$true)]
		$solutiondir,
		$vcfpowershellfile,
		$build = $false
     )
cls

# single threaded
# $build = $false

$deploy_database = $true
$deploy_search = $true
$deploy_frontend = $true
$deploy_scheduler = $true
$deploy_admin = $true

if($deploydatabase -eq "False") 
{ 
    $deploy_database = $false
}

if($deploysearch -eq "False") 
{ 
    $deploy_search = $false
}

if($deployfrontend -eq "False") 
{
    $deploy_frontend = $false
}

if($deployscheduler -eq "False") 
{
    $deploy_scheduler = $false
}

if($deployadmin -eq "False") 
{
    $deploy_admin = $false
}

Set-Location "$solutiondir\src\extensions\Setup\VirtoCommerce.PowerShell"
. ".\azure-deploy.ps1" -deploymentdir $deploymentdir -solutiondir $solutiondir -storageaccount $storageaccount -subscriptionname $subscriptionname -search_servicename $search_servicename -frontend_servicename $frontend_servicename -scheduler_servicename $scheduler_servicename -db_servername $db_servername -db_serverlogin $db_serverlogin -db_serverpassword $db_serverpassword -db_databasename $db_databasename -build $build -deploy_database $deploy_database -publishsettingsfile $publishsettingsfile -vcfpowershellfile $vcfpowershellfile -region $region -slot $slot -deploy_search $deploy_search -deploy_scheduler $deploy_scheduler -deploy_frontend $deploy_frontend -deploy_admin $deploy_admin