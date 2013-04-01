param(
    $buildFile   = (join-path (Split-Path -parent $MyInvocation.MyCommand.Definition) "PerfIt.msbuild"),
    $buildParams = "/p:Configuration=Release",
    $buildTarget = "/t:Default"
)

& "$(get-content env:windir)\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" $buildFile $buildParams $buildTarget