﻿(*** hide ***)

#load "credentials.fsx"
#r "../../packages/AForge.2.2.5/lib/AForge.dll" 
#r "../../packages/AForge.Math.2.2.5/lib/AForge.Math.dll" 
#r "../../packages/AForge.Imaging.2.2.5\lib\AForge.Imaging.dll" 

open System
open System.IO
open MBrace.Core
open MBrace.Store
open MBrace.Azure
open MBrace.Azure.Client

open System.Drawing
open AForge.Imaging.Filters
open System.Net
open System.IO

(**
# Using MBrace for image processing

In this tutorial, you use the AForge (you can install AForge from Nuget) to turn color images into gray ones by applying a gray filter.

Before running, edit credentials.fsx to enter your connection strings.
*)

(** First you connect to the cluster: *)
let cluster = Runtime.GetHandle(config)

(** Next, you define a method to download an image from a url, and return a stream containing the downloaded image. *)
let GetStreamFromUrl (url : string) =
    let webClient = new WebClient()
    let data = webClient.DownloadData(url)
    let stream = new MemoryStream(data)
    stream


(** Next, you create a method that turns the downloaded color image into a gray image by applying an AForge filter, and then uploads the gray image to Azure blob. *)
let GrayImageFromWeb (url : string) file =
    cloud {
        // Download image.
        let inputStream =GetStreamFromUrl url
        // Apply the gray filter to the original image.
        let originalBmp = new Bitmap(inputStream)
        let grayedBmp = Grayscale.CommonAlgorithms.BT709.Apply originalBmp        
        // Get the grayed image.
        let outputStream = new MemoryStream()
        do grayedBmp.Save(outputStream, Imaging.ImageFormat.Bmp)        
        // Upload the gray image to Azure blob.
        let! cFile = CloudFile.WriteAllBytes(file, outputStream.ToArray())
        inputStream.Close()
        outputStream.Close()
        return cFile
    }


(** Last, you perform parallel downloading and image processing in MBrace cluster. *)
let urls = [|
    "https://upload.wikimedia.org/wikipedia/commons/thumb/5/54/Tigress_at_Jim_Corbett_National_Park.jpg/330px-Tigress_at_Jim_Corbett_National_Park.jpg";
    "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8c/Poligraf_Poligrafovich.JPG/800px-Poligraf_Poligrafovich.JPG" |]

let tasks = 
    [|for url in urls -> GrayImageFromWeb url (sprintf ("tmp/%s") (Path.GetFileName(Uri(url).LocalPath))) |] 
    |> Cloud.Parallel
    |> cluster.Run


(** 
In this tutorial, you've learned how to use the AForge image processing library to turn color images to gray ones in MBrace.
*)