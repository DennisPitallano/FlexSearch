﻿// ----------------------------------------------------------------------------
//  Licensed to FlexSearch under one or more contributor license 
//  agreements. See the NOTICE file distributed with this work 
//  for additional information regarding copyright ownership. 
//
//  This source code is subject to terms and conditions of the 
//  Apache License, Version 2.0. A copy of the license can be 
//  found in the License.txt file at the root of this distribution. 
//  You may also obtain a copy of the License at:
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
//  By using this source code in any fashion, you are agreeing
//  to be bound by the terms of the Apache License, Version 2.0.
//
//  You must not remove this notice, or any other, from this software.
// ----------------------------------------------------------------------------
namespace FlexSearch.Core

open FlexSearch.Api.Constants
open FlexSearch.Api.Model
open Microsoft.Extensions.Primitives
open System.Net
open System.IO

module ServiceHelpers =
    
    /// Gets a search query from an Owin request using the optional body
    let getQueryFromRequest request body = 
        let query = 
            match body with
            | Some(q) -> q
            | None -> new SearchQuery()
        query.QueryString <- request.HttpContext |> stringFromQueryString "q" query.QueryString
        query.Columns <- match request.HttpContext.Request.Query |> getFirstStringValue "c" with
                         | null -> query.Columns
                         | v -> v.Split([| ',' |], System.StringSplitOptions.RemoveEmptyEntries)
        query.Count <- request.HttpContext |> intFromQueryString "count" query.Count
        query.Skip <- request.HttpContext |> intFromQueryString "skip" query.Skip
        query.OrderBy <- request.HttpContext |> stringFromQueryString "orderby" query.OrderBy
        query.OrderByDirection <- 
            match request.HttpContext.Request.Query |> getFirstStringValue "orderbydirection" with
            | null -> query.OrderByDirection
            | value -> 
                if value = "asc" then
                    OrderByDirection.Ascending
                else
                    OrderByDirection.Descending
        query.PredefinedQuery <- request.HttpContext |> stringFromQueryString "predefinedquery" query.PredefinedQuery
        query.IndexName <- request.ResId.Value
        query

/// Returns OK status
[<Sealed>]
[<Name("GET-/ping")>]
type PingHandler() = 
    inherit HttpHandlerBase<NoBody, unit>()
    override __.Process(_, _) = SuccessResponse((), Ok)

/// Returns the homepage
[<Name("GET-/")>]
[<Sealed>]
type GetRootHandler() = 
    inherit HttpHandlerBase<NoBody, unit>()
    
    let filePath = Path.Combine(Constants.WebFolder, "home.html")
    let pageExists = filePath |> System.IO.File.Exists 
    
    override __.Process(request, _) = 
        if pageExists then
            request.HttpContext.Response.Redirect("/portal/home.html");
            NoResponse
        else
            FailureResponse(FileNotFound(filePath), NotFound)

/// Returns the homepage
[<Name("GET-/favicon.ico")>]
[<Sealed>]
type GetFaviconHandler() = 
    inherit HttpHandlerBase<NoBody, unit>()

    let filePath = Path.Combine(Constants.WebFolder, "favicon.ico")
    let pageExists = filePath |> System.IO.File.Exists 
    
    override __.Process(request, _) = 
        if pageExists then
            request.HttpContext.Response.Redirect("/portal/favicon.ico");
            NoResponse
        else
            FailureResponse(FileNotFound(filePath), NotFound)

///  Get all indices
[<Name("GET-/indices")>]
[<Sealed>]
type GetAllIndexHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<NoBody, Index []>()
    override __.Process(_, _) = SuccessResponse(indexService.GetAllIndex(), Ok)

///  Get an index
[<Name("GET-/indices/:id")>]
[<Sealed>]
type GetIndexByIdHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<NoBody, Index>(true)
    override __.Process(request, _) = SomeResponse(indexService.GetIndex(request.ResId.Value), Ok, NotFound)

/// Create an index
[<Name("POST-/indices")>]
[<Sealed>]
type PostIndexByIdHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<Index, CreationId>()
    override __.Process(_, body) = 
        match indexService.AddIndex(body.Value) with
        | Ok(response) -> SuccessResponse(response, Created)
        | Fail(error) -> 
            if error.OperationMessage().ErrorCode = "IndexAlreadyExists" then FailureResponse(error, Conflict)
            else FailureResponse(error, BadRequest)

/// Delete an index
[<Name("DELETE-/indices/:id")>]
[<Sealed>]
type DeleteIndexByIdHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<NoBody, unit>()
    override __.Process(request, _) = SomeResponse(indexService.DeleteIndex(request.ResId.Value), Ok, BadRequest)


/// Update the Index Fields
[<Name("PUT-/indices/:id/fields")>]
[<Sealed>]
type PutIndexFieldsHandler(indexService : IIndexService) =
    inherit HttpHandlerBase<FieldsUpdateRequest, unit>()
    override __.Process(request, body) = 
        SomeResponse(indexService.UpdateIndexFields(request.ResId.Value, body.Value.Fields), Ok, Conflict)

[<Name("PUT-/indices/:id/predefinedquery")>]
[<Sealed>]
type PutIndexPredefinedQueryHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<SearchQuery, unit>()
    override __.Process(request, body) =
        SomeResponse(indexService.AddOrUpdatePredefinedQuery(request.ResId.Value, body.Value), Ok, Conflict)

[<Name("PUT-/indices/:id/configuration")>]
[<Sealed>]
type PutIndexConfigurationHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<IndexConfiguration, unit>()
    override __.Process(request, body) =
        SomeResponse(indexService.UpdateIndexConfiguration(request.ResId.Value, body.Value), Ok, Conflict)

///// Update an index
//[<Name("PUT-/indices/:id")>]
//[<Sealed>]
//type PutIndexByIdHandler(indexService : IIndexService) = 
//    inherit HttpHandlerBase<Index.Dto, unit>()
//    override __.Process(request, body) = 
//        // Index name passed in URL takes precedence
//        body.Value.IndexName <- request.ResId.Value
//        SomeResponse(indexService.UpdateIndex(body.Value), Ok, BadRequest)

/// Get index status
[<Name("GET-/indices/:id/status")>]
[<Sealed>]
type GetStatusHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<NoBody, IndexStatusResponse>()
    override __.Process(request, _) = 
        let response = 
            match indexService.GetIndexState(request.ResId.Value) with
            | Ok(state) -> ok <| new IndexStatusResponse(IndexStatus = state)
            | Fail(error) -> fail(error)
        SomeResponse(response, Ok, BadRequest)

/// Update index status
[<Name("PUT-/indices/:id/status/:id")>]
[<Sealed>]
type PutStatusHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<NoBody, unit>()
    override __.Process(request, _) = 
        match request.SubResId.Value with
        | InvariantEqual "online" -> SomeResponse(indexService.OpenIndex(request.ResId.Value), Ok, BadRequest)
        | InvariantEqual "offline" -> SomeResponse(indexService.CloseIndex(request.ResId.Value), Ok, BadRequest)
        | _ -> FailureResponse(HttpNotSupported, BadRequest)

/// Check if an index exists
[<Name("GET-/indices/:id/exists")>]
[<Sealed>]
type GetExistsHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<NoBody, IndexExists>()
    override __.Process(request, _) = 
        let response = indexService.IndexExists(request.ResId.Value)
        SuccessResponse(new IndexExists(Exists = response), Ok)

/// Gets the size on disk of an index
[<Name("GET-/indices/:id/size")>]
[<Sealed>]
type GetIndexSizeHandler(indexService : IIndexService) = 
    inherit HttpHandlerBase<NoBody, int64>()
    override __.Process(request, _) = 
        SomeResponse(indexService.GetDiskUsage request.ResId.Value, Ok, BadRequest)

// -------------------------- //
// -------------------------- //
// Analysis Handlers          //  
// -------------------------- //
// -------------------------- //
/// <summary>
///  Get an analyzer by Id
/// </summary>
/// <method>GET</method>
/// <uri>/analyzers/:id</uri>
/// <resource>analyzer</resource>
/// <id>get-analyzer-by-id</id>
[<Name("GET-/analyzers/:id")>]
[<Sealed>]
type GetAnalyzerByIdHandler(analyzerService : IAnalyzerService) = 
    inherit HttpHandlerBase<NoBody, Analyzer>()
    override __.Process(request, _) = SomeResponse(analyzerService.GetAnalyzerInfo(request.ResId.Value), Ok, NotFound)

/// <summary>
///  Get all analyzer
/// </summary>
/// <method>GET</method>
/// <uri>/analyzers</uri>
/// <resource>analyzer</resource>
/// <id>get-all-analyzer</id>
[<Name("GET-/analyzers")>]
[<Sealed>]
type GetAllAnalyzerHandler(analyzerService : IAnalyzerService) = 
    inherit HttpHandlerBase<NoBody, Analyzer []>()
    override __.Process(_, _) = SomeResponse(ok <| analyzerService.GetAllAnalyzers(), Ok, BadRequest)

/// <summary>
///  Analyze a text string using the passed analyzer.
/// </summary>
/// <method>POST</method>
/// <uri>/analyzers/:analyzerName/analyze</uri>
/// <resource>analyzer</resource>
/// <id>get-analyze-text</id>
[<Name("POST-/analyzers/:id/analyze")>]
[<Sealed>]
type AnalyzeTextHandler(analyzerService : IAnalyzerService) = 
    inherit HttpHandlerBase<AnalysisRequest, string []>()
    override __.Process(request, body) = 
        SomeResponse(analyzerService.Analyze(request.ResId.Value, body.Value.Text), Ok, BadRequest)

/// <summary>
///  Delete an analyzer by Id
/// </summary>
/// <method>DELETE</method>
/// <uri>/analyzers/:id</uri>
/// <resource>analyzer</resource>
/// <id>delete-analyzer-by-id</id>
[<Name("DELETE-/analyzers/:id")>]
[<Sealed>]
type DeleteAnalyzerByIdHandler(analyzerService : IAnalyzerService) = 
    inherit HttpHandlerBase<NoBody, unit>()
    override __.Process(request, _) = SomeResponse(analyzerService.DeleteAnalyzer(request.ResId.Value), Ok, BadRequest)

/// <summary>
///  Create or update an analyzer
/// </summary>
/// <method>PUT</method>
/// <uri>/analyzers/:id</uri>
/// <resource>analyzer</resource>
/// <id>put-analyzer-by-id</id>
[<Name("PUT-/analyzers/:id")>]
[<Sealed>]
type CreateOrUpdateAnalyzerByIdHandler(analyzerService : IAnalyzerService) = 
    inherit HttpHandlerBase<Analyzer, unit>()
    override __.Process(request, body) = 
        body.Value.AnalyzerName <- request.ResId.Value
        SomeResponse(analyzerService.UpdateAnalyzer(body.Value), Ok, BadRequest)

// -------------------------- //
// -------------------------- //
// Document Handlers          //  
// -------------------------- //
// -------------------------- //
/// <summary>
///  Get top documents
/// </summary>
/// <remarks>
/// Returns top 10 documents from the index. This is not the preferred 
/// way to retrieve documents from an index. This is provided
/// for quick testing only.
/// </remarks>
/// <method>GET</method>
/// <uri>/indices/:indexName/documents</uri>
/// <resource>document</resource>
/// <id>get-documents</id>
[<Name("GET-/indices/:id/documents")>]
[<Sealed>]
type GetDocumentsHandler(documentService : IDocumentService) = 
    inherit HttpHandlerBase<NoBody, SearchResults>()
    override __.Process(request, _) = 
        let count = request.HttpContext |> intFromQueryString "count" 10
        SomeResponse(documentService.GetDocuments(request.ResId.Value, count), Ok, BadRequest)

/// <summary>
///  Get document by Id
/// </summary>
/// <remarks>
/// Returns a document by id. This returns all the fields associated
/// with the current document. Use 'Search' endpoint to customize the 
/// fields to be returned.
/// </remarks>
/// <method>GET</method>
/// <uri>/indices/:indexName/documents/:documentId</uri>
/// <resource>document</resource>
/// <id>get-document-by-id</id>
[<Name("GET-/indices/:id/documents/:id")>]
[<Sealed>]
type GetDocumentByIdHandler(documentService : IDocumentService) = 
    inherit HttpHandlerBase<NoBody, Document>()
    override __.Process(request, _) = 
        SomeResponse(documentService.GetDocument(request.ResId.Value, request.SubResId.Value), Ok, NotFound)

/// <summary>
///  Create a new document
/// </summary>
/// <remarks>
/// Create a new document. By default this does not check if the id of the 
/// of the document is unique across the index. Use a timestamp of -1 to 
/// enforce unique id check.
/// </remarks>
/// <method>POST</method>
/// <uri>/indices/:indexName/documents</uri>
/// <resource>document</resource>
/// <id>create-document-by-id</id>
[<Name("POST-/indices/:id/documents")>]
[<Sealed>]
type PostDocumentByIdHandler(documentService : IDocumentService) = 
    inherit HttpHandlerBase<Document, CreationId>()
    override __.Process(_, body) = 
        match documentService.AddDocument(body.Value) with
        | Ok(response) -> SuccessResponse(response, Created)
        | Fail(error) -> 
            if error.OperationMessage().ErrorCode = "DocumentIdAlreadyExists" then FailureResponse(error, Conflict)
            else FailureResponse(error, BadRequest)

/// <summary>
///  Deletes all documents
/// </summary>
/// <remarks>
/// Deletes all document from the given index
/// </remarks>
/// <method>DELETE</method>
/// <uri>/indices/:indexId/documents</uri>
/// <resource>document</resource>
/// <id>delete-documents</id>
[<Name("DELETE-/indices/:id/documents")>]
[<Sealed>]
type DeleteDocumentsHandler(documentService : IDocumentService) = 
    inherit HttpHandlerBase<NoBody, unit>()
    override __.Process(request, _) = 
        SomeResponse(documentService.DeleteAllDocuments(request.ResId.Value), Ok, BadRequest)

/// <summary>
///  Delete a document
/// </summary>
/// <remarks>
/// Delete a document by Id.
/// </remarks>
/// <method>DELETE</method>
/// <uri>/indices/:indexId/documents/:documentId</uri>
/// <resource>document</resource>
/// <id>delete-document-by-id</id>
[<Name("DELETE-/indices/:id/documents/:id")>]
[<Sealed>]
type DeleteDocumentByIdHandler(documentService : IDocumentService) = 
    inherit HttpHandlerBase<NoBody, unit>()
    override __.Process(request, _) = 
        SomeResponse(documentService.DeleteDocument(request.ResId.Value, request.SubResId.Value), Ok, BadRequest)

/// <summary>
///  Create or update a document
/// </summary>
/// <remarks>
/// Creates or updates an existing document. This is idempotent as repeated calls to the
/// endpoint will have the same effect. Many concurrency control parameters can be 
/// applied using timestamp field.
/// </remarks>
/// <method>PUT</method>
/// <uri>/indices/:indexId/documents/:documentId</uri>
/// <resource>document</resource>
/// <id>update-document-by-id</id>
[<Name("PUT-/indices/:id/documents/:id")>]
[<Sealed>]
type PutDocumentByIdHandler(documentService : IDocumentService) = 
    inherit HttpHandlerBase<Document, unit>()
    override __.Process(_, body) = SomeResponse(documentService.AddOrUpdateDocument(body.Value), Ok, BadRequest)

// -------------------------- //
// -------------------------- //
// Demo & Job Handlers        //  
// -------------------------- //
// -------------------------- //
/// <summary>
///  Sets up a demo index. The name of the index is `country`.
/// </summary>
/// <method>PUT</method>
/// <uri>/setupdemo</uri>
/// <resource>setup</resource>
/// <id>put-setup-demo</id>
[<Name("PUT-/setupdemo")>]
[<Sealed>]
type SetupDemoHandler(service : DemoIndexService) = 
    inherit HttpHandlerBase<NoBody, unit>()
    override __.Process(_, _) = SomeResponse(service.Setup(), Ok, BadRequest)

[<Name("GET-/jobs/:id")>]
[<Sealed>]
type GetJobByIdHandler(jobService : IJobService) = 
    inherit HttpHandlerBase<NoBody, Job>()
    override __.Process(request, _) = SomeResponse(jobService.GetJob(request.ResId.Value), Ok, NotFound)

// -------------------------- //
// -------------------------- //
// Search Handlers            //  
// -------------------------- //
// -------------------------- //
/// <summary>
///  Search for documents
/// </summary>
/// <remarks>
/// Search across the index for documents using SQL like query syntax.
/// {{note: Any parameter passed as part of query string takes precedence over the same parameter in the request body.}}
/// </remarks>
/// <parameters>
/// <parameter name="q" required="true">Short hand for 'QueryString'.</parameter>
/// <parameter name="c" required="false">Short hand for 'Columns'.</parameter>
/// <parameter name="count">Count parameter. Refer to 'Search Query' properties.</parameter>
/// <parameter name="skip">Skip parameter. Refer to 'Search Query' properties.</parameter>
/// <parameter name="orderby">Order by parameter. Refer to 'Search Query' properties.</parameter>
/// <parameter name="returnflatresult">Return flat results parameter. Refer to 'Search Query' properties.</parameter>
/// </parameters>
/// <method>GET|POST</method>
/// <uri>/indices/:id/search</uri>
/// <resource>search</resource>
/// <id>search-an-index</id>
[<Name("GET|POST-/indices/:id/search")>]
[<Sealed>]
type GetSearchHandler(searchService : ISearchService) = 
    inherit HttpHandlerBase<SearchQuery, SearchResults>(false)

    override __.Process(request, body) = 
        let query = ServiceHelpers.getQueryFromRequest request body
            
        match searchService.Search(query) with
        | Ok(result) -> SuccessResponse(result, Ok)
        | Fail(error) -> FailureResponse(error, BadRequest)

/// <summary>
///  Deletes documents returned by search query
/// </summary>
/// <remarks>
/// Deletes all document returned by the search query for the given index. Returns the records identified
/// by the search query.
/// </remarks>
/// <method>DELETE</method>
/// <parameters>
/// <parameter name="q" required="true">Short hand for 'QueryString'.</parameter>
/// <parameter name="count">Count parameter. Refer to 'Search Query' properties.</parameter>
/// <parameter name="skip">Skip parameter. Refer to 'Search Query' properties.</parameter>
/// <parameter name="orderby">Order by parameter. Refer to 'Search Query' properties.</parameter>
/// <parameter name="orderbydirection">Order by Direction parameter. Refer to 'Search Query' properties.</parameter>
/// </parameters>
/// <uri>/indices/:indexId/search</uri>
/// <resource>document</resource>
/// <id>delete-documents</id>
[<Name("DELETE-/indices/:id/search")>]
[<Sealed>]
type DeleteDocumentsFromSearchHandler(documentService : IDocumentService) = 
    inherit HttpHandlerBase<NoBody, SearchResults>()
    override __.Process(request, _) = 
        let query = ServiceHelpers.getQueryFromRequest request <| Some (new SearchQuery())

        match documentService.DeleteDocumentsFromSearch(request.ResId.Value, query) with
        | Ok(result) -> SuccessResponse(result, Ok)
        | Fail(error) -> FailureResponse(error, BadRequest)

[<Name("POST-/indices/:id/predefinedquerytest")>]
[<Sealed>]
type PostPredefinedQueryTestHandler(searchService : ISearchService) = 
    inherit HttpHandlerBase<PredefinedQueryTestDto, SearchResults>()
    override __.Process(request, body) =
        body.Value.SearchQuery.IndexName <- request.ResId.Value
        match searchService.Search(body.Value.SearchQuery, body.Value.PredefinedQuery) with
        | Ok(result) -> SuccessResponse(result, Ok)
        | Fail(error) -> FailureResponse(error, BadRequest)

[<Name("GET-/memory")>]
[<Sealed>]
type GetMemoryDetails() =
    inherit HttpHandlerBase<NoBody, MemoryDetails>()
    override __.Process(request, body) =
        let usedMemory = 
            let procName = System.Diagnostics.Process.GetCurrentProcess().ProcessName
            let counter = new System.Diagnostics.PerformanceCounter("Process", "Working Set - Private", procName)
            counter.RawValue
        let totalMemory = (new Microsoft.VisualBasic.Devices.ComputerInfo()).TotalPhysicalMemory
        SuccessResponse(new MemoryDetails(
            UsedMemory = usedMemory,
            TotalMemory = int64 totalMemory,
            Usage = float(usedMemory) / float(totalMemory) * 100.0), Ok)