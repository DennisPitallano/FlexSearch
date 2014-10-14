﻿namespace FlexSearch.TestSupport

open Xunit
open FlexSearch.Api
open FlexSearch.Client
open FlexSearch.Core
open NSubstitute
open Ploeh.AutoFixture
open Ploeh.AutoFixture.AutoNSubstitute
open Ploeh.AutoFixture.DataAnnotations
open Ploeh.AutoFixture.Xunit
open System
open System.Linq
open Xunit.Extensions
open Xunit.Sdk
open Autofac
open System.Threading
open Microsoft.Owin.Testing
open System.Net.Http

[<AutoOpen>]
module UnitTestAttributes = 
    /// <summary>
    /// Represents the lookup name for the plug-in
    /// </summary>
    [<Sealed>]
    [<System.AttributeUsage(System.AttributeTargets.Method)>]
    type ExampleAttribute(fileName : string, title : string) = 
        inherit Attribute()
        member this.FileName = fileName
        member this.Title = title
    
    /// <summary>
    /// Unit test domain customization
    /// </summary>
    type DomainCustomization() = 
        inherit CompositeCustomization(new AutoNSubstituteCustomization(), new SupportMutableValueTypesCustomization())
    
    /// <summary>
    /// Auto fixture based Xunit attribute
    /// </summary>
    type AutoMockDataAttribute() = 
        inherit AutoDataAttribute((new Fixture()).Customize(new DomainCustomization()))
    
    /// <summary>
    /// Auto fixture based Xunit inline data attribute
    /// </summary>
    [<AttributeUsage(AttributeTargets.Method, AllowMultiple = true)>]
    type InlineAutoMockDataAttribute([<ParamArray>] values : Object []) = 
        inherit CompositeDataAttribute([| new InlineDataAttribute(values) :> DataAttribute
                                          new AutoMockDataAttribute() :> DataAttribute |])

[<AutoOpen>]
module IntegrationTestHelpers = 
    let serverSettings = ServerSettings.GetDefault()
    let logger = FlexSearch.Logging.LogService.GetLogger(true)
    let Container = Main.GetContainer(serverSettings, logger, true)

    /// <summary>
    /// Baisc index configuration
    /// </summary>
    let MockIndexSettings() = 
        let index = new Index()
        index.IndexName <- "contact"
        index.Online <- true
        index.IndexConfiguration.DirectoryType <- DirectoryType.Ram
        index.Fields.Add(new Field("firstname", FieldType.Text))
        index.Fields.Add(new Field("lastname", FieldType.Text))
        index.Fields.Add(new Field("email", FieldType.ExactText))
        index.Fields.Add(new Field("country", FieldType.Text))
        index.Fields.Add(new Field("ipaddress", FieldType.ExactText))
        index.Fields.Add(new Field("cvv2", FieldType.Int))
        index.Fields.Add(new Field("description", FieldType.Highlight))
        // Computed fields
        index.Fields.Add(new Field("fullname", FieldType.Text, ScriptName = "fullname"))
        index.Scripts.Add
            (new Script("fullname", """return fields["firstname"] + " " + fields["lastname"];""", ScriptType.ComputedField))
        let searchProfileQuery = 
            new SearchQuery(index.IndexName, "firstname = '' AND lastname = '' AND cvv2 = '116' AND country = ''", QueryName = "test1")
        searchProfileQuery.MissingValueConfiguration.Add("firstname", MissingValueOption.ThrowError)
        searchProfileQuery.MissingValueConfiguration.Add("cvv2", MissingValueOption.Default)
        searchProfileQuery.MissingValueConfiguration.Add("topic", MissingValueOption.Ignore)
        index.SearchProfiles.Add(searchProfileQuery)
        index
    
    let GetBasicIndexSettingsForContact() = 
        let index = new Index()
        index.IndexName <- Guid.NewGuid().ToString("N")
        index.Online <- true
        index.IndexConfiguration.DirectoryType <- DirectoryType.Ram
        index.Fields.Add(new Field("gender", FieldType.ExactText))
        index.Fields.Add(new Field("title", FieldType.ExactText))
        index.Fields.Add(new Field("givenname", FieldType.Text))
        index.Fields.Add(new Field("middleinitial", FieldType.Text))
        index.Fields.Add(new Field("surname", FieldType.Text))
        index.Fields.Add(new Field("streetaddress", FieldType.Text))
        index.Fields.Add(new Field("city", FieldType.ExactText))
        index.Fields.Add(new Field("state", FieldType.ExactText))
        index.Fields.Add(new Field("zipcode", FieldType.ExactText))
        index.Fields.Add(new Field("country", FieldType.ExactText))
        index.Fields.Add(new Field("countryfull", FieldType.ExactText))
        index.Fields.Add(new Field("emailaddress", FieldType.ExactText))
        index.Fields.Add(new Field("username", FieldType.ExactText))
        index.Fields.Add(new Field("password", FieldType.ExactText))
        index.Fields.Add(new Field("cctype", FieldType.ExactText))
        index.Fields.Add(new Field("ccnumber",FieldType.ExactText))
        index.Fields.Add(new Field("occupation", FieldType.Text))
        index.Fields.Add(new Field("cvv2", FieldType.Int))
        index.Fields.Add(new Field("nationalid", FieldType.ExactText))
        index.Fields.Add(new Field("ups", FieldType.ExactText))
        index.Fields.Add(new Field("company", FieldType.Stored))
        index.Fields.Add(new Field("pounds", FieldType.Double))
        index.Fields.Add(new Field("centimeters", FieldType.Int))
        index.Fields.Add(new Field("guid", FieldType.ExactText))
        index.Fields.Add(new Field("latitude", FieldType.Double))
        index.Fields.Add(new Field("longitude", FieldType.Double))
        index.Fields.Add(new Field("importdate", FieldType.Date))
        index.Fields.Add(new Field("timestamp", FieldType.DateTime))
        index.Fields.Add(new Field("topic", FieldType.ExactText))
        index.Fields.Add(new Field("abstract", FieldType.Highlight))
        // Computed fields
        index.Fields.Add(new Field("fullname", FieldType.Text, ScriptName = "fullname"))
        index.Scripts.Add
            (new Script("fullname", """return fields.givenname + " " + fields.surname;""", ScriptType.ComputedField))
        let searchProfileQuery = 
            new SearchQuery(index.IndexName, "givenname = '' AND surname = '' AND cvv2 = '1' AND topic = ''", QueryName = "test1")
        searchProfileQuery.MissingValueConfiguration.Add("givenname", MissingValueOption.ThrowError)
        searchProfileQuery.MissingValueConfiguration.Add("cvv2", MissingValueOption.Default)
        searchProfileQuery.MissingValueConfiguration.Add("topic", MissingValueOption.Ignore)
        index.SearchProfiles.Add(searchProfileQuery)
        index
    
    /// <summary>
    /// Utility method to add data to an index
    /// </summary>
    /// <param name="indexService"></param>
    /// <param name="index"></param>
    /// <param name="testData"></param>
    let AddTestDataToIndex(index : Index, testData : string, documentService : IDocumentService, 
                           indexService : IIndexService) = 
        indexService.AddIndex(index) |> ExpectSuccess
        let lines = testData.Split([| "\r\n"; "\n" |], StringSplitOptions.RemoveEmptyEntries)
        if lines.Count() < 2 then failwithf "No data to index"
        let headers = lines.[0].Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
        for line in lines.Skip(1) do
            let items = line.Split([| "," |], StringSplitOptions.RemoveEmptyEntries)
            let indexDocument = new FlexDocument()
            indexDocument.Id <- items.[0].Trim()
            indexDocument.IndexName <- index.IndexName
            for i in 1..items.Length - 1 do
                indexDocument.Fields.Add(headers.[i].Trim(), items.[i].Trim())
            documentService.AddDocument(indexDocument) |> ExpectSuccess
        indexService.Commit(index.IndexName) |> ExpectSuccess
        indexService.Refresh(index.IndexName) |> ExpectSuccess
    
    //Thread.Sleep(200)
    //        let documents = GetSuccessChoice(documentService.GetDocuments(index.IndexName))
    //        Assert.Equal<int>(lines.Count() - 1, (documents.Count))
    /// <summary>
    /// Helper method to generate test index with supplied data
    /// </summary>
    /// <param name="testData"></param>
    let GenerateIndexWithTestData(testData : string, index : Index) = 
        AddTestDataToIndex(index, testData, Container.Resolve<IDocumentService>(), Container.Resolve<IIndexService>())
        index
    
    // Add mock contact index to our test server 
    GenerateIndexWithTestData(TestData.MockTestData, MockIndexSettings()) |> ignore
    
    /// <summary>
    /// Test setup fixture to use with Xunit IUseFixture
    /// </summary>
    type IndexFixture() = 
        member val Index = Unchecked.defaultof<_> with get, set
        
        member this.Setup(testData : string, index : Index) = 
            if this.Index = Unchecked.defaultof<_> then this.Index <- GenerateIndexWithTestData(testData, index)
        
        interface System.IDisposable with
            member this.Dispose() = ExpectSuccess(Container.Resolve<IIndexService>().DeleteIndex(this.Index.IndexName))
    
    let private VerifySearchCount (expected : int) (queryString : string) (indexName : string) = 
        let query = new SearchQuery(indexName, queryString)
        let searchService = Container.Resolve<ISearchService>()
        let result = GetSuccessChoice(searchService.Search(query))
        Assert.Equal<int>(expected, result.RecordsReturned)
    
    /// <summary>
    /// Base for creating all Xunit based indexing integration tests
    /// </summary>
    [<AbstractClass>]
    type IndexTestBase(testData : string, ?index0 : Index) = 
        let index = defaultArg index0 (GetBasicIndexSettingsForContact())
        member val Index = Unchecked.defaultof<_> with get, set
        member val IndexName = Unchecked.defaultof<_> with get, set
        member this.VerifySearchCount (expected : int) (queryString : string) = 
            VerifySearchCount expected queryString this.IndexName
        interface IUseFixture<IndexFixture> with
            member this.SetFixture(data) = 
                data.Setup(testData, index)
                this.Index <- data.Index
                this.IndexName <- data.Index.IndexName
    
    /// <summary>
    /// Unit test domain customization
    /// </summary>
    type IntegrationCustomization() = 
        interface ICustomization with
            member this.Customize(fixture : IFixture) = 
                let GetTestServer(indexService : IIndexService, httpFactory : IFlexFactory<IHttpResource>) = 
                    let testServer = 
                        TestServer.Create(fun app -> 
                            let owinServer = new OwinServer(indexService, httpFactory, FlexSearch.Logging.LogService.GetLogger(true))
                            owinServer.Configuration(app))
                    testServer
                fixture.Inject<IIndexService>(Container.Resolve<IIndexService>()) |> ignore
                fixture.Inject<ISearchService>(Container.Resolve<ISearchService>()) |> ignore
                fixture.Inject<IDocumentService>(Container.Resolve<IDocumentService>()) |> ignore
                fixture.Inject<IFlexFactory<IHttpResource>>(Container.Resolve<IFlexFactory<IHttpResource>>()) |> ignore
                fixture.Register<Index>(fun _ -> GetBasicIndexSettingsForContact()) |> ignore
                let testServer = GetTestServer(Container.Resolve<IIndexService>(), Container.Resolve<IFlexFactory<IHttpResource>>())
                let loggingHandler = new LoggingHandler(testServer.Handler)
                let httpClient = new HttpClient(loggingHandler)
                let flexClient = new FlexClient(httpClient)
                fixture.Inject<TestServer>(testServer) |> ignore
                fixture.Inject<LoggingHandler>(loggingHandler);
                fixture.Inject<IFlexClient>(flexClient) |> ignore
    
    /// <summary>
    /// Unit test domain customization
    /// </summary>
    type IntegrationDomainCustomization() = 
        inherit CompositeCustomization(new IntegrationCustomization(), new SupportMutableValueTypesCustomization())
    
    /// <summary>
    /// Auto fixture based Xunit attribute
    /// </summary>
    type AutoMockIntegrationDataAttribute() = 
        inherit AutoDataAttribute((new Fixture()).Customize(new IntegrationDomainCustomization()))
    
    /// <summary>
    /// Auto fixture based Xunit in-line data attribute
    /// </summary>
    [<AttributeUsage(AttributeTargets.Method, AllowMultiple = true)>]
    type InlineAutoMockIntegrationDataAttribute([<ParamArray>] values : Object []) = 
        inherit CompositeDataAttribute([| new InlineDataAttribute(values) :> DataAttribute
                                          new AutoMockIntegrationDataAttribute() :> DataAttribute |])
