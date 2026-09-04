$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'report-test-failures.ps1')

$fixture = @'
<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010">
  <TestDefinitions>
    <UnitTest id="one"><TestMethod className="Example.SecurityTests" name="RejectsForeignActor" /></UnitTest>
    <UnitTest id="two"><TestMethod className="Example.SecurityTests" name="ReadsOwnResource" /></UnitTest>
  </TestDefinitions>
  <Results>
    <UnitTestResult testId="one" outcome="Failed" testName="private parameter value">
      <Output><ErrorInfo><Message>private failure content</Message></ErrorInfo></Output>
    </UnitTestResult>
    <UnitTestResult testId="one" outcome="Failed" testName="another private parameter" />
    <UnitTestResult testId="two" outcome="Passed" />
    <UnitTestResult testId="missing" outcome="Error" testName="private missing name" />
  </Results>
</TestRun>
'@
$summary = Get-TrxFailureSummary -XmlText $fixture
if ($summary.FailureCount -ne 3 -or $summary.Names.Count -ne 2 -or
    $summary.Names -notcontains 'Example.SecurityTests.RejectsForeignActor' -or
    $summary.Names -notcontains 'UnidentifiedTest' -or
    ($summary.Names -join ',') -match 'private') {
    throw 'Failure reporting did not retain only static method names and aggregate counts.'
}

$empty = Get-TrxFailureSummary -XmlText '<TestRun><Results /></TestRun>'
if ($empty.FailureCount -ne 0 -or $empty.Names.Count -ne 0) { throw 'Empty report handling failed.' }

$rejected = $false
try {
    Get-TrxFailureSummary -XmlText '<!DOCTYPE x [<!ENTITY injected "untrusted">]><TestRun>&injected;</TestRun>' | Out-Null
}
catch { $rejected = $true }
if (-not $rejected) { throw 'DTD processing was not rejected.' }

Write-Output 'TRX diagnostic parser: pass, failure deduplication, unknown method, no payload disclosure and DTD rejection PASS.'
