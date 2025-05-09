#if INTERACTIVE
#r "System.Xml.Linq.dll"
#else
module Rss
#endif

open System
open System.IO
open System.Xml.Linq


type RssItem =
    { Title: string
      Link: string
      ReleaseDate: DateTimeOffset
      Description: string }


let description (row: string array) =
    let link = $"https://news.ycombinator.com/item?id={row[1]}"

    $"""<p>Comments most recent post: <a href="{link}">{link}</a>.</p> 
    <p>Posted {row[4]} times with {row[5]} median votes.</p>"""

// load tab separated file and ignore the lines at the top that start with a #
let classics =
    File.ReadAllLines "classics.tsv"
    |> Seq.filter (fun line -> not (line.StartsWith "#"))
    |> Seq.map (fun line -> line.Split('\t'))
    |> Seq.randomSample 10
    |> Seq.map (fun parts ->
        { Title = parts.[2]
          Link = parts.[3]
          ReleaseDate = DateTimeOffset.Now
          Description = description parts })
    |> Seq.toList

// Source: https://fssnip.net/7QI by Tuomas Hietanen
let myChannelFeed (channelTitle: string) (channelLink: string) (channelDescription: string) (items: RssItem list) =
    let xn = XName.Get
    let elem name (value: string) = XElement(xn name, value)

    let elems =
        items
        |> List.sortBy (fun i -> i.ReleaseDate)
        |> List.map (fun i ->
            XElement(
                xn "item",
                elem "title" i.Title,
                elem "link" i.Link,
                elem "guid" i.Link,
                elem "pubDate" (i.ReleaseDate.ToString("r")),
                elem "description" i.Description
            ))

    XDocument(
        XDeclaration("1.0", "utf-8", "yes"),
        XElement(
            xn "rss",
            XAttribute(xn "version", "2.0"),
            elem "title" channelTitle,
            elem "link" channelLink,
            elem "description" channelDescription,
            elem "language" "en-us",
            XElement(xn "channel", elems)
        )
        |> box
    )

let generateFeed =
    myChannelFeed
        "Hacker News Classics"
        "https://raw.githubusercontent.com/Roald87/HackernewsClassics/refs/heads/main/rss.xml"
        "Ten random Hacker News classics in a RSS feed."

generateFeed classics |> fun doc -> doc.Save "rss.xml"
