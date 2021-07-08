Write-Output "Preparing configuration array"

[System.Collections.ArrayList] $configurations = @();

#wordcount configurations
$configurations.Add(@(0, 3, 6000, 'text', '', 0, 10, "crainst18"));
$configurations.Add(@(0, 3, 6000, 'text', '', 0, 15, "crainst18"));
$configurations.Add(@(0, 3, 6000, 'text', '', 1, 10, "crainst18"));
$configurations.Add(@(0, 3, 6000, 'text', '', 1, 15, "crainst18"));
$configurations.Add(@(0, 3, 6000, 'text', '', 2, 10, "crainst18"));
$configurations.Add(@(0, 3, 6000, 'text', '', 2, 15, "crainst18"));

#text projection configurations
$configurations.Add(@(1, 3, 6000, 'text', '', 0, 10, "crainst13"));
$configurations.Add(@(1, 3, 6000, 'text', '', 0, 15, "crainst13"));
$configurations.Add(@(1, 3, 6000, 'text', '', 1, 10, "crainst13"));
$configurations.Add(@(1, 3, 6000, 'text', '', 1, 15, "crainst13"));
$configurations.Add(@(1, 3, 6000, 'text', '', 2, 10, "crainst13"));
$configurations.Add(@(1, 3, 6000, 'text', '', 2, 15, "crainst13"));

#nexmark selection configurations
$configurations.Add(@(2, 10, 4500, 'nexmark', 'auctions,people', 0, 10, "crainst13"));
$configurations.Add(@(2, 10, 4500, 'nexmark', 'auctions,people', 0, 15, "crainst13"));
$configurations.Add(@(2, 10, 4500, 'nexmark', 'auctions,people', 1, 10, "crainst13"));
$configurations.Add(@(2, 10, 4500, 'nexmark', 'auctions,people', 1, 15, "crainst13"));
$configurations.Add(@(2, 10, 4500, 'nexmark', 'auctions,people', 2, 10, "crainst13"));
$configurations.Add(@(2, 10, 4500, 'nexmark', 'auctions,people', 2, 15, "crainst13"));

#nexmark local item configurations
$configurations.Add(@(3, 3, 1800, 'nexmark', 'bids', 0, 10, "crainst19"));
$configurations.Add(@(3, 3, 1800, 'nexmark', 'bids', 0, 15, "crainst19"));
$configurations.Add(@(3, 3, 1800, 'nexmark', 'bids', 1, 10, "crainst19"));
$configurations.Add(@(3, 3, 1800, 'nexmark', 'bids', 1, 15, "crainst19"));
$configurations.Add(@(3, 3, 1800, 'nexmark', 'bids', 2, 10, "crainst19"));
$configurations.Add(@(3, 3, 1800, 'nexmark', 'bids', 2, 15, "crainst19"));

#nexmark hot item configurations
$configurations.Add(@(4, 10, 1600, 'nexmark', 'auctions,people', 0, 10, "crainst10"));
$configurations.Add(@(4, 10, 1600, 'nexmark', 'auctions,people', 0, 15, "crainst10"));
$configurations.Add(@(4, 10, 1600, 'nexmark', 'auctions,people', 1, 10, "crainst10"));
$configurations.Add(@(4, 10, 1600, 'nexmark', 'auctions,people', 1, 15, "crainst10"));
$configurations.Add(@(4, 10, 1600, 'nexmark', 'auctions,people', 2, 10, "crainst10"));
$configurations.Add(@(4, 10, 1600, 'nexmark', 'auctions,people', 2, 15, "crainst10"));

#nexmark averageprice by seller configurations
$configurations.Add(@(5, 2, 1500, 'nexmark', 'people', 0, 10, "crainst09"));
$configurations.Add(@(5, 2, 1500, 'nexmark', 'people', 0, 15, "crainst09"));
$configurations.Add(@(5, 2, 1500, 'nexmark', 'people', 1, 10, "crainst09"));
$configurations.Add(@(5, 2, 1500, 'nexmark', 'people', 1, 15, "crainst09"));
$configurations.Add(@(5, 2, 1500, 'nexmark', 'people', 2, 10, "crainst09"));
$configurations.Add(@(5, 2, 1500, 'nexmark', 'people', 2, 15, "crainst09"));

#graph nhop configurations
$configurations.Add(@(6, 1, 3200, 'graph', '', 0, 10, "crainst14"));
$configurations.Add(@(6, 1, 3200, 'graph', '', 0, 15, "crainst14")); 
#$configurations.Add(@(6, 1, 3200, 'graph', '', 1, 10, "crainst14"));
#$configurations.Add(@(6, 1, 3200, 'graph', '', 1, 15, "crainst14")); -- EXCLUDED BECAUSE COORDINATED MODE DOES NOT SUPPORT CYCLES
$configurations.Add(@(6, 1, 3200, 'graph', '', 2, 10, "crainst14"));
$configurations.Add(@(6, 1, 3200, 'graph', '', 2, 15, "crainst14"));

Write-Output "Configuration array setup completed"

Foreach($conf in $configurations) {
    Write-Output ">>>> Starting experiment execution for job $($conf[0])"
    $repCount = 5
    For ($i=0; $i -lt $repCount; $i++) {
        #actual experiment execution happens here
        .\execute-experiment.ps1 $conf[0] $i $conf[1] $conf[2] $conf[3] $conf[4] $conf[5] $conf[6] $conf[7]
        
        Write-Output ">>>> Waiting for next repetition"
        Start-Sleep -s 30 #ensure pods have a chance to terminate or next run cant start due to insufficient available resources
    }
    Write-Output ">>>> Configuration executed"
    [console]::beep(500,150)
}





