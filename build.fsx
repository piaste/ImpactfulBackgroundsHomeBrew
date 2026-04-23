open System
open System.IO

#load "./getLsLib.fsx"

if not (Directory.Exists "./Tools") then
    (LSLibHelpers.downloadToolsLsLib "Norbyte/lslib" ".").Wait()    

#r "./Tools/LSLib.dll"

open LSLib.LS

let upstreamModName = "Home Brew - Comprehensive Reworks"
let modName = "Home Brew - Comprehensive Reworks - Lore Texts"

// get mod version from `meta.lsx`
open System.Xml.Linq
let version =
    // intentionally unsafe, will crash if it can't read the version
    $"./{upstreamModName}/Mods/{modName}/meta.lsx"
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
do Localization.beforeBuild()
do Packager().CreatePackage(
        packagePath = outputPath,
        inputPath = System.IO.Path.GetFullPath $"./{upstreamModName}/" ,
        build = new PackageBuildData(
            Version = Enums.PackageVersion.V18,
            Compression = CompressionMethod.LZ4,
            Priority = 0uy
        )
    ).Wait()
do Localization.afterBuild()

System.Console.WriteLine $"Generated {outputPath}"