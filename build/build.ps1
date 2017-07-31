param(
    $buildFile   = (join-path (Split-Path -parent $MyInvocation.MyCommand.Definition) "PerfIt.msbuild"),
    $buildParams = "/p:Configuration=Release",
    $buildTarget = "/t:Default"
)

.\MSBuild.exe $buildFile $buildParams $buildTarget