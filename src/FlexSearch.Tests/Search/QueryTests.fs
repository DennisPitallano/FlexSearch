﻿namespace FlexSearch.Tests

open FlexSearch.Tests
open FlexSearch.Tests.SearchTests
open Swensen.Unquote

type ``Operator: 'allOf' Tests``(ih : IntegrationHelper) = 
    inherit SearchTestsBase(ih)
    override __.``Works with Exact Field``() = ih |> verifyResultCount 4 "allof(et1, 'aaron')"
    override __.``Works with Id Field``() = ih |> verifyResultCount 1 "allof(_id, '1')"
    override __.``Works with TimeStamp Field``() = ih |> verifyResultCount 10 "allof(_timestamp, @IGNORE, -matchall)"
    override __.``Works with ModifyIndex Field``() = ih |> verifyResultCount 1 "allof(_modifyindex, '2')"
    override __.``Works with Int Field``() = ih |> verifyResultCount 1 "allof(i1, '-100')"
    override __.``Works with Multiple Int input``() = ih |> verifyResultCount 0 "allof(i1, '-100', '150')"
    override __.``Works with Long Field``() = ih |> verifyResultCount 1 "allof(l1, '-1000')"
    override __.``Works with Double Field``() = ih |> verifyResultCount 1 "allof(db1, '-1000')"
    override __.``Works with Float Field``() = ih |> verifyResultCount 1 "allof(f1, '-1000')"
    override __.``Works with DateTime Field``() = ih |> verifyResultCount 1 "allof(dt1, '20101010101010')"
    override __.``Works with Date Field``() = ih |> verifyResultCount 1 "allof(d1, '20101010')"
    override __.``Works with Bool Field``() = ih |> verifyResultCount 5 "allof(b1, 'T')"
    override __.``Works with Stored Field``() = ih |> storedFieldCannotBeSearched "allof(s1, '*')"
    override __.``Works with And clause``() = ih |> verifyResultCount 1 "allof(et1, 'fred') AND allof(l1, '1500')"
    override __.``Works with Or clause``() = ih |> verifyResultCount 2 "allof(et1, 'erik') OR allof(l1, '4000')"
    override __.``Works with Not clause``() = ih |> verifyResultCount 2 "allof(et1, 'aaron') AND NOT allof(l1, '1000')"
    override __.``Filter query``() = ("allof(et1, 'aaron')", "allof(et1, 'aaron') and allof(et1, 'aaron', -filter)")
    override __.``Works with AndOr clause``() = 
        ih |> verifyResultCount 3 "allof(t1, 'aaron') and (allof(t2, 'johnson') or allof(t2, 'Garner'))"
    override __.``Works with Multiple params``() = ih |> verifyResultCount 0 "allof(et1, 'aaron', 'erik')"
    override __.``Works with Constants``() = ih |> verifyResultCount 4 "allof(et1, 'aaron', @IGNORE)"
    member __.``Order of tokens does not matter``() = ih |> verifyResultCount 6 "allof(t3, 'parliamentary', 'monarchy')"
    member __.``Tokens don't have to be adjacent to each other``() = ih |> verifyResultCount 2 "allof(t3, 'federal', 'democracy')"

type ``Operator: 'anyOf' Tests``(ih : IntegrationHelper) = 
    inherit SearchTestsBase(ih)
    override __.``Works with Exact Field``() = ih |> verifyResultCount 4 "anyof(et1, 'aaron')"
    override __.``Works with Id Field``() = ih |> verifyResultCount 2 "anyof(_id, '1', '2')"
    override __.``Works with TimeStamp Field``() = ih |> verifyResultCount 10 "anyof(_timestamp, @IGNORE, -matchall)"
    override __.``Works with ModifyIndex Field``() = ih |> verifyResultCount 1 "anyof(_modifyindex, '2')"
    override __.``Works with Int Field``() = ih |> verifyResultCount 1 "anyof(i1, '-100')"
    override __.``Works with Multiple Int input``() = ih |> verifyResultCount 2 "anyof(i1, '-100', '150')"
    override __.``Works with Long Field``() = ih |> verifyResultCount 2 "anyof(l1, '-1000', '4000')"
    override __.``Works with Double Field``() = ih |> verifyResultCount 1 "anyof(db1, '-1000')"
    override __.``Works with Float Field``() = ih |> verifyResultCount 1 "anyof(f1, '-1000')"
    override __.``Works with DateTime Field``() = ih |> verifyResultCount 1 "anyof(dt1, '20101010101010')"
    override __.``Works with Date Field``() = ih |> verifyResultCount 1 "anyof(d1, '20101010')"
    override __.``Works with Bool Field``() = ih |> verifyResultCount 5 "anyof(b1, 'T')"
    override __.``Works with Stored Field``() = ih |> storedFieldCannotBeSearched "anyof(s1, '*')"
    override __.``Works with And clause``() = ih |> verifyResultCount 2 "anyof(et1, 'fred') AND anyof(l1, '1500', '-1000')"
    override __.``Works with Or clause``() = ih |> verifyResultCount 2 "anyof(et1, 'erik') OR anyof(l1, '4000')"
    override __.``Works with Not clause``() = ih |> verifyResultCount 2 "anyof(et1, 'aaron') AND NOT anyof(l1, '1000')"
    override __.``Filter query``() = ("anyof(et1, 'aaron')", "anyof(et1, 'aaron') and anyof(et1, 'aaron', -filter)")
    override __.``Works with AndOr clause``() = 
        ih |> verifyResultCount 3 "anyof(t1, 'aaron') and (anyof(t2, 'johnson') or anyof(t2, 'Garner'))"
    override __.``Works with Multiple params``() = ih |> verifyResultCount 5 "anyof(et1, 'aaron', 'erik')"
    override __.``Works with Constants``() = ih |> verifyResultCount 4 "anyof(et1, 'aaron', @IGNORE)"
    member __.``Order of tokens does not matter``() = ih |> verifyResultCount 7 "anyof(t3, 'parliamentary', 'monarchy')"
    member __.``Tokens don't have to be adjacent to each other``() = ih |> verifyResultCount 3 "anyof(t3, 'federal', 'democracy')"
