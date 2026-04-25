open System
open System.IO

#load "./getLsLib.fsx"

if not (Directory.Exists "./Tools") then
    (LSLibHelpers.downloadToolsLsLib "Norbyte/lslib" ".").Wait()    

#r "./Tools/LSLib.dll"

open LSLib.LS

let modName = "Impactful Backgrounds for Home Brew"

// get mod version from `meta.lsx`
open System.Xml.Linq
let version =
    // intentionally unsafe, will crash if it can't read the version
    $"./{modName}/Mods/{modName}/meta.lsx"
    |> XDocument.Load    
    |> _.Descendants()
        |> Seq.find (fun n -> 
            n.Name = XName.Get "node" 
            && n.Attribute(XName.Get "id").Value = "ModuleInfo")
    |> _.Elements()
        |> Seq.find (fun a -> 
            a.Name = XName.Get "attribute" 
            && a.Attribute(XName.Get "id").Value = "Version64")
    
    |> _.Attribute(XName.Get "value").Value
    |> System.Int64.Parse
    |> LSLib.LS.PackedVersion.FromInt64
    |> fun pv -> sprintf "%i.%i.%i.%i" pv.Major pv.Minor pv.Revision pv.Build

// build package
let outputPath = 
    Directory.CreateDirectory "./output"
    |> _.FullName
    |> fun path -> $"{path}/{modName}-{version}.pak"

// actual build
do File.Delete outputPath
let localizationXmlPath = 
    "./Impactful Backgrounds for Home Brew/Localization/English/ImpactfulBackgroundsForHomeBrew.xml"
    |> Path.GetFullPath
do LocaUtils.Save(
            resource = LocaUtils.Load localizationXmlPath,
            outputPath = localizationXmlPath.Replace(".xml", ".loca"),
            format = LocaFormat.Loca
        )
do Packager().CreatePackage(
        packagePath = outputPath,
        inputPath = System.IO.Path.GetFullPath $"./{modName}/" ,
        build = new PackageBuildData(
            Version = Enums.PackageVersion.V18,
            Compression = CompressionMethod.LZ4,
            Priority = 0uy
        )
    ).Wait()

System.Console.WriteLine $"Generated {outputPath}"

// helper
let encodeVersion major minor revision build = 
    printfn "%A" <| PackedVersion(Major = uint32 major, Minor = uint32 minor, Revision = uint32 revision, Build = uint32 build).ToVersion64()
