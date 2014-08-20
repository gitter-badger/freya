﻿namespace Dyfrig

open System.Collections.Generic
open Aether
open Aether.Operators

type Method =
    | DELETE 
    | HEAD 
    | GET 
    | OPTIONS 
    | PATCH 
    | POST 
    | PUT 
    | TRACE 
    | Custom of string

type Protocol =
    | HTTP of float 
    | Custom of string

type Scheme =
    | HTTP 
    | HTTPS 
    | Custom of string

[<AutoOpen>]
module internal Http =

    let isoMethodLens : Lens<string, Method> =
        (fun s -> 
            match s with
            | "DELETE" -> DELETE 
            | "HEAD" -> HEAD 
            | "GET" -> GET 
            | "OPTIONS" -> OPTIONS
            | "PATCH" -> PATCH 
            | "POST" -> POST 
            | "PUT" -> PUT 
            | "TRACE" -> TRACE
            | x -> Method.Custom x ), 
        (fun m _ -> 
            match m with
            | DELETE -> "DELETE" 
            | HEAD -> "HEAD" 
            | GET -> "GET" 
            | OPTIONS -> "OPTIONS"
            | PATCH -> "PATCH" 
            | POST -> "POST" 
            | PUT -> "PUT"  
            | TRACE -> "TRACE"
            | Method.Custom x -> x)

    let isoProtocolLens : Lens<string, Protocol> =
        (fun s ->
            match s with
            | "HTTP/1.0" -> Protocol.HTTP 1.0 
            | "HTTP/1.1" -> Protocol.HTTP 1.1 
            | x -> Protocol.Custom x), 
        (fun p _ ->
            match p with
            | Protocol.HTTP x -> sprintf "HTTP/%f" x 
            | Protocol.Custom x -> x)

    let isoSchemeLens : Lens<string, Scheme> =
        (fun s ->
            match s with
            | "http" -> HTTP 
            | "https" -> HTTPS 
            | x -> Custom x), 
        (fun s _ ->
            match s with
            | HTTP -> "http" 
            | HTTPS -> "https" 
            | Custom x -> x)

    let isoHeaderPLens k : PLens<IDictionary<string, string []>, string list> =
        (fun headers ->
            match headers.TryGetValue k with
            | true, v -> Some (List.ofArray v)
            | _ -> None),
        (fun x headers ->
            headers.[k] <- List.toArray x
            headers)

[<AutoOpen>]
module Lenses =

    let owinEnvLens k : Lens<OwinEnv, obj> =
        (fun e -> e.[k]), 
        (fun o e -> e.[k] <- o; e)

    let owinEnvPLens k : PLens<OwinEnv, obj> =
        (fun e -> e.TryGetValue k |> function | true, v -> Some v | _ -> None), 
        (fun o e -> e.[k] <- o; e)

    let isoBoxLens<'T> : Lens<obj, 'T> =
        (fun o -> unbox o), (fun o _ -> box o)

[<RequireQualifiedAccess>]
module Request =

    let Header key =
        owinEnvLens Constants.requestHeaders
        >--> isoBoxLens<IDictionary<string, string []>>
        >-?> isoHeaderPLens key

    let Method =
        owinEnvLens Constants.requestMethod
        >--> isoBoxLens<string>
        >--> isoMethodLens

    let Path =
        owinEnvLens Constants.requestPath
        >--> isoBoxLens<string>

    let Protocol =
        owinEnvLens Constants.requestProtocol
        >--> isoBoxLens<string>
        >--> isoProtocolLens

    let Scheme = 
        owinEnvLens Constants.requestScheme 
        >--> isoBoxLens<string>
        >--> isoSchemeLens

[<RequireQualifiedAccess>]
module Response =

    let Header key =
        owinEnvLens Constants.responseHeaders
        >--> isoBoxLens<IDictionary<string, string []>>
        >-?> isoHeaderPLens key

    let ReasonPhrase =
        owinEnvPLens Constants.responseReasonPhrase
        >?-> isoBoxLens<string>

    let StatusCode =
        owinEnvPLens Constants.responseStatusCode
        >?-> isoBoxLens<int>
