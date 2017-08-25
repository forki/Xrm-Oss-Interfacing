// include Fake libs
#I @"packages\FAKE\tools\"
#r @"packages\FAKE\tools\FakeLib.dll"

open Fake
open Fake.AssemblyInfoFile
open System
open System.IO
open Fake.Paket

//Project config
let projectName = "Xrm.Oss.Interfacing"
let projectDescription = "A Dynamics CRM / Dynamics365 template for messaging interfaces using RabbitMQ"
let authors = ["Florian Kroenert"]

// Directories
let buildDir  = @".\build\"
let interfaceBuildDir = buildDir + @"interface\"
let crmConsumerBuildDir = interfaceBuildDir + @"crmConsumer";
let crmListenerBuildDir = interfaceBuildDir + @"crmListener";
let crmPublisherBuildDir = interfaceBuildDir + @"crmPublisher";
let demoCrmPublisherBuildDir = interfaceBuildDir + @"demoCrmPublisher";
let domainBuildDir = interfaceBuildDir + @"domain";
let thirdPartyConsumerBuildDir = interfaceBuildDir + @"thirdPartyConsumer";
let thirdPartyPublisherBuildDir = interfaceBuildDir + @"thirdPartyPublisher";
let workflowActivityBuildDir = interfaceBuildDir + @"workflowActivity";
let testDir   = @".\test\"

let deployDir = @".\Publish\"
let interfaceDeployDir = deployDir + @"interface\"
let crmConsumerDeployDir = interfaceDeployDir + @"crmConsumer";
let crmListenerDeployDir = interfaceDeployDir + @"crmListener";
let crmPublisherDeployDir = interfaceDeployDir + @"crmPublisher";
let demoCrmPublisherDeployDir = interfaceDeployDir + @"demoCrmPublisher";
let domainDeployDir = interfaceDeployDir + @"domain";
let thirdPartyConsumerDeployDir = interfaceDeployDir + @"thirdPartyConsumer";
let thirdPartyPublisherDeployDir = interfaceDeployDir + @"thirdPartyPublisher";
let workflowActivityDeployDir = interfaceDeployDir + @"workflowActivity";

let nugetDir = @".\nuget\"
let packagesDir = @".\packages\"

// version info
let mutable majorversion    = "1"
let mutable minorversion    = "0"
let mutable build           = buildVersion
let mutable nugetVersion    = ""
let mutable asmVersion      = ""
let mutable asmInfoVersion  = ""

let WiXPath = Path.Combine("packages", "WiX.Toolset", "tools", "wix")
let WixCrmListenerProductUpgradeGuid = new Guid("84293e9b-3c43-4bec-9c7b-88af9a70269f")
let WixCrmPublisherProductUpgradeGuid = new Guid("e6883ea5-17e1-4471-92dd-1b9106a6e26b")
let ProductVersion () = asmVersion
let ProductPublisher = "Xrm-Oss"

// Targets
Target "Clean" (fun _ ->

    CleanDirs [buildDir; testDir; deployDir; nugetDir]
)

Target "BuildVersions" (fun _ ->
    if isLocalBuild then
        build <- "0"

    asmVersion      <- majorversion + "." + minorversion + "." + build
    asmInfoVersion  <- asmVersion

    let nugetBuildNumber = if not isLocalBuild then build else "0"
    
    nugetVersion    <- majorversion + "." + minorversion + "." + nugetBuildNumber

    SetBuildNumber nugetVersion   // Publish version to TeamCity
)

Target "AssemblyInfo" (fun _ ->
    BulkReplaceAssemblyInfoVersions "src" (fun f -> 
                                              {f with
                                                  AssemblyVersion = asmVersion
                                                  AssemblyInformationalVersion = asmInfoVersion
                                                  AssemblyFileVersion = asmVersion})
)

Target "BuildCrmConsumer" (fun _ ->
    !! @"src\interface\Xrm.Oss.CrmConsumer\*.csproj"
        |> MSBuildRelease crmConsumerBuildDir "Build"
        |> Log "Build-Output: "
)

Target "BuildCrmListener" (fun _ ->
    !! @"src\interface\Xrm.Oss.CrmListener\*.csproj"
        |> MSBuildRelease crmListenerBuildDir "Build"
        |> Log "Build-Output: "
)

Target "BuildCrmPublisher" (fun _ ->
    !! @"src\interface\Xrm.Oss.CrmPublisher\*.csproj"
        |> MSBuildRelease crmPublisherBuildDir "Build"
        |> Log "Build-Output: "
)

Target "BuildDemoCrmPublisher" (fun _ ->
    !! @"src\interface\Xrm.Oss.DemoPublisher\*.csproj"
        |> MSBuildRelease demoCrmPublisherBuildDir "Build"
        |> Log "Build-Output: "
)

Target "BuildDomain" (fun _ ->
    !! @"src\domain\Xrm.Oss.Interfacing.Domain\*.csproj"
        |> MSBuildRelease domainBuildDir "Build"
        |> Log "Build-Output: "
)

Target "BuildWorkflowActivity" (fun _ ->
    !! @"src\plugin\Xrm.Oss.WorkflowActivities\*.csproj"
        |> MSBuildRelease workflowActivityBuildDir "Build"
        |> Log "Build-Output: "
)

Target "BuildThirdPartyConsumer" (fun _ ->
    !! @"src\interface\Xrm.Oss.ThirdPartyConsumer\*.csproj"
        |> MSBuildRelease thirdPartyConsumerBuildDir "Build"
        |> Log "Build-Output: "
)

Target "BuildThirdPartyPublisher" (fun _ ->
    !! @"src\interface\Xrm.Oss.ThirdPartyPublisher\*.csproj"
        |> MSBuildRelease thirdPartyPublisherBuildDir "Build"
        |> Log "Build-Output: "
)

Target "BuildTest" (fun _ ->
    !! @"src\test\**\*.csproj"
      |> MSBuildDebug testDir "Build"
      |> Log "Build Log: "
)

Target "BuildCrmListenerSetup" (fun _ ->
    // This defines, which files should be collected when running bulkComponentCreation
    let fileFilter = fun (file : FileInfo) -> true
        
    // Collect Files which should be shipped. Pass directory with your deployment output for deployDir
    // along with the targeted architecture.
    let components = bulkComponentCreation fileFilter (DirectoryInfo crmListenerDeployDir) Architecture.X64
             
    // Collect component references for usage in features
    let componentRefs = components |> Seq.map(fun comp -> comp.ToComponentRef())

    let completeFeature = generateFeatureElement (fun f -> 
                                                    {f with  
                                                        Id = "Complete"
                                                        Title = "Complete Feature"
                                                        Level = 1 
                                                        Description = "Installs all features"
                                                        Components = componentRefs
                                                        Display = Expand 
                                                    })

    // Generates a predefined WiX template with placeholders which will be replaced in "FillInWiXScript"
    generateWiXScript "SetupTemplate.wxs"

    let WiXUIMondo = generateUIRef (fun f ->
                                        {f with
                                            Id = "WixUI_Minimal"
                                        })

    let WiXUIError = generateUIRef (fun f ->
                                        {f with
                                            Id = "WixUI_ErrorProgressText"
                                        })

    let MajorUpgrade = generateMajorUpgradeVersion(
                            fun f ->
                                {f with 
                                    Schedule = MajorUpgradeSchedule.AfterInstallExecute
                                    DowngradeErrorMessage = "A later version is already installed, exiting."
                                })

    FillInWiXTemplate "" (fun f ->
                            {f with
                                // Guid which should be generated on every build
                                ProductCode = Guid.NewGuid()
                                ProductName = "Xrm-Oss-CrmListener"
                                Description = "CRM Listener to publish CRM events to service bus"
                                ProductLanguage = 1031
                                ProductVersion = ProductVersion()
                                ProductPublisher = ProductPublisher
                                // Set fixed upgrade guid, this should never change for this project!
                                UpgradeGuid = WixCrmListenerProductUpgradeGuid
                                MajorUpgrade = [MajorUpgrade]
                                UIRefs = [WiXUIMondo; WiXUIError]
                                ProgramFilesFolder = ProgramFiles64
                                Components = components
                                BuildNumber = build
                                Features = [completeFeature]
                            })
    
    let setupFileName = sprintf "%s - %s.msi" "Xrm-Oss-CrmListener" (ProductVersion ())

    // run the WiX tools
    WiX (fun p -> {p with ToolDirectory = WiXPath}) 
        (Path.Combine (deployDir, setupFileName))
        @".\SetupTemplate.wxs"
)

Target "BuildCrmPublisherSetup" (fun _ ->
    // This defines, which files should be collected when running bulkComponentCreation
    let fileFilter = fun (file : FileInfo) -> true
        
    // Collect Files which should be shipped. Pass directory with your deployment output for deployDir
    // along with the targeted architecture.
    let components = bulkComponentCreation fileFilter (DirectoryInfo crmPublisherDeployDir) Architecture.X64
             
    // Collect component references for usage in features
    let componentRefs = components |> Seq.map(fun comp -> comp.ToComponentRef())

    let completeFeature = generateFeatureElement (fun f -> 
                                                    {f with  
                                                        Id = "Complete"
                                                        Title = "Complete Feature"
                                                        Level = 1 
                                                        Description = "Installs all features"
                                                        Components = componentRefs
                                                        Display = Expand 
                                                    })

    // Generates a predefined WiX template with placeholders which will be replaced in "FillInWiXScript"
    generateWiXScript "SetupTemplate.wxs"

    let WiXUIMondo = generateUIRef (fun f ->
                                        {f with
                                            Id = "WixUI_Minimal"
                                        })

    let WiXUIError = generateUIRef (fun f ->
                                        {f with
                                            Id = "WixUI_ErrorProgressText"
                                        })

    let MajorUpgrade = generateMajorUpgradeVersion(
                            fun f ->
                                {f with 
                                    Schedule = MajorUpgradeSchedule.AfterInstallExecute
                                    DowngradeErrorMessage = "A later version is already installed, exiting."
                                })

    FillInWiXTemplate "" (fun f ->
                            {f with
                                // Guid which should be generated on every build
                                ProductCode = Guid.NewGuid()
                                ProductName = "Xrm-Oss-CrmPublisher"
                                Description = "CRM Publisher to transform CRM events to messages in the service bus"
                                ProductLanguage = 1031
                                ProductVersion = ProductVersion()
                                ProductPublisher = ProductPublisher
                                // Set fixed upgrade guid, this should never change for this project!
                                UpgradeGuid = WixCrmPublisherProductUpgradeGuid
                                MajorUpgrade = [MajorUpgrade]
                                UIRefs = [WiXUIMondo; WiXUIError]
                                ProgramFilesFolder = ProgramFiles64
                                Components = components
                                BuildNumber = build
                                Features = [completeFeature]
                            })
        
    let setupFileName = sprintf "%s - %s.msi" "Xrm-Oss-CrmPublisher" (ProductVersion ())

    // run the WiX tools
    WiX (fun p -> {p with ToolDirectory = WiXPath}) 
        (Path.Combine (deployDir, setupFileName))
        @".\SetupTemplate.wxs"
)

Target "Publish" (fun _ ->
    CreateDir interfaceDeployDir
    CreateDir crmConsumerDeployDir
    CreateDir crmListenerDeployDir
    CreateDir crmPublisherDeployDir
    CreateDir thirdPartyConsumerDeployDir 
    CreateDir thirdPartyPublisherDeployDir
    CreateDir demoCrmPublisherDeployDir
    CreateDir domainDeployDir
    CreateDir workflowActivityDeployDir
    
    !! (crmConsumerBuildDir @@ @"*.*")
        |> CopyTo crmConsumerDeployDir

    !! (crmListenerBuildDir @@ @"*.*")
        |> CopyTo crmListenerDeployDir

    !! (crmPublisherBuildDir @@ @"*.*")
        |> CopyTo crmPublisherDeployDir

    !! (thirdPartyConsumerBuildDir @@ @"*.*")
        |> CopyTo thirdPartyConsumerDeployDir

    !! (thirdPartyPublisherBuildDir @@ @"*.*")
        |> CopyTo thirdPartyPublisherDeployDir

    !! (demoCrmPublisherBuildDir @@ @"*.*")
        |> CopyTo demoCrmPublisherDeployDir

    !! (domainBuildDir @@ @"*.*")
        |> CopyTo domainDeployDir

    !! (workflowActivityBuildDir @@ @"*.*")
        |> CopyTo workflowActivityDeployDir
)

Target "CreateNuget" (fun _ ->
    Pack (fun p ->
            {p with
                Version = nugetVersion
                
            })
)

// Dependencies
"Clean"
  ==> "BuildVersions"
  =?> ("AssemblyInfo", not isLocalBuild )
  ==> "BuildDomain"
  ==> "BuildCrmConsumer"
  ==> "BuildCrmListener"
  ==> "BuildCrmPublisher"
  ==> "BuildThirdPartyConsumer"
  ==> "BuildThirdPartyPublisher"
  ==> "BuildDemoCrmPublisher"
  ==> "BuildWorkflowActivity"
  ==> "BuildTest"
  ==> "Publish"
  ==> "BuildCrmPublisherSetup"
  ==> "BuildCrmListenerSetup"
  ==> "CreateNuget"

// start build
RunTargetOrDefault "BuildCrmListenerSetup"
