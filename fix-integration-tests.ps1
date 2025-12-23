# Fix Integration Tests Script

$testFiles = @(
    "packages/flowFlex-backend/Tests/FlowFlex.Tests/Services/Integration/IntegrationServiceTests.cs",
    "packages/flowFlex-backend/Tests/FlowFlex.Tests/Services/Integration/EntityMappingServiceTests.cs",
    "packages/flowFlex-backend/Tests/FlowFlex.Tests/Services/Integration/FieldMappingServiceTests.cs",
    "packages/flowFlex-backend/Tests/FlowFlex.Tests/Services/Integration/QuickLinkServiceTests.cs",
    "packages/flowFlex-backend/Tests/FlowFlex.Tests/Services/Integration/IntegrationSyncServiceTests.cs"
)

foreach ($file in $testFiles) {
    $content = Get-Content $file -Raw
    
    # Fix InsertReturnSnowflakeIdAsync with 3 parameters -> 1 parameter
    $content = $content -replace 'InsertReturnSnowflakeIdAsync\(It\.IsAny<[^>]+>\(\), false, default\)', 'InsertReturnSnowflakeIdAsync(It.IsAny<$1>())'
    
    # Fix GetByIdAsync with 3 parameters -> 1 parameter  
    $content = $content -replace '\.GetByIdAsync\(It\.IsAny<long>\(\), false, default\)', '.GetByIdAsync(It.IsAny<object>())'
    
    # Fix ExistsNameAsync and ExistsLabelAsync
    $content = $content -replace 'ExistsNameAsync\(It\.IsAny<string>\(\), It\.IsAny<long\?>\(\)\)', 'ExistsNameAsync(It.IsAny<string>(), It.IsAny<object>())'
    $content = $content -replace 'ExistsLabelAsync\(It\.IsAny<long>\(\), It\.IsAny<string>\(\), It\.IsAny<long\?>\(\)\)', 'ExistsLabelAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<object>())'
    
    Set-Content -Path $file -Value $content -NoNewline
}

Write-Host "Fixed all test files!" -ForegroundColor Green

