using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: ReactCodeGenerator <OpenApiSpecPath> <ComponentName> [TypeName]");
            return;
        }

        string openApiSpecPath = args[0];
        string componentName = args[1];
        string typeNameFilter = args.Length > 2 ? args[2] : null;

        var openApiDocument = await LoadOpenApiDocument(openApiSpecPath);

        if (openApiDocument != null)
        {
            GenerateTypesFile(openApiDocument, componentName, typeNameFilter);
            GenerateApiFile(openApiDocument, componentName, typeNameFilter);
            GenerateSliceFile(componentName);
            GenerateSagaFile(componentName);
            GenerateContainerAndContentFiles(componentName);
        }
    }

    static async Task<OpenApiDocument> LoadOpenApiDocument(string filePath)
    {
        using (var stream = File.OpenRead(filePath))
        {
            var openApiReader = new OpenApiStreamReader();
            var document = await openApiReader.ReadAsync(stream);
            return document.OpenApiDocument;
        }
    }

    static void GenerateTypesFile(OpenApiDocument openApiDocument, string componentName, string typeNameFilter)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"export interface {componentName} {{");

        foreach (var schema in openApiDocument.Components.Schemas)
        {
            if (typeNameFilter != null && !schema.Key.Equals(typeNameFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var property in schema.Value.Properties)
            {
                sb.AppendLine($"    {property.Key}: {MapOpenApiTypeToTypeScript(property.Value.Type, property.Value.Format)};");
            }
        }

        sb.AppendLine("}");

        File.WriteAllText($"{componentName}.types.ts", sb.ToString());
    }

    static string MapOpenApiTypeToTypeScript(string openApiType, string format)
    {
        return openApiType switch
        {
            "string" => format == "date-time" ? "Date" : "string",
            "integer" => "number",
            "boolean" => "boolean",
            "array" => "any[]",
            _ => "any",
        };
    }
    public static void GenerateApiFile(OpenApiDocument openApiDocument, string componentName, string typeNameFilter)
    {
        StringBuilder sb = new StringBuilder();

        // Define imports
        sb.AppendLine($"import {{ {componentName}Request, {componentName}Response }} from '../../types/{componentName.ToLower()}.types';");
        sb.AppendLine("import fetchClient from '../fetchClient';");
        sb.AppendLine($"import {{ serialize{componentName} }} from './{componentName.ToLower()}.serializer';");
        sb.AppendLine();

        foreach (var path in openApiDocument.Paths)
        {
            // Check if the path contains the typeNameFilter
            if (path.Key.Contains(typeNameFilter, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var operation in path.Value.Operations)
                {
                    var methodName = $"{operation.Key.ToString().ToLower()}{componentName}";
                    var returnType = $"{componentName}Response[]";
                    var payloadType = $"{componentName}Request[]";
                    var url = path.Key;

                    if (operation.Key == OperationType.Get)
                    {
                        // GET method
                        sb.AppendLine($"export const fetch{componentName} = async (): Promise<{returnType}> => {{");
                        sb.AppendLine($"  const response = await fetchClient.get<any>(`{url}`);");
                        sb.AppendLine($"  return serialize{componentName}(response);");
                        sb.AppendLine("};");
                        sb.AppendLine();
                    }
                    else if (operation.Key == OperationType.Post)
                    {
                        // POST method
                        sb.AppendLine($"export const set{componentName} = async (payload: {payloadType}): Promise<{returnType}> => {{");
                        sb.AppendLine($"  return await fetchClient.post<{returnType}>(`{url}`, payload);");
                        sb.AppendLine("};");
                        sb.AppendLine();
                    }
                    // You can add more method types (PUT, DELETE) here if needed.
                }
            }
        }

        // Write to file
        var fileName = $"{componentName.ToLower()}.api.ts";
        //var fullPath = Path.Combine(outputPath, fileName);
        File.WriteAllText(fileName, sb.ToString());
    }
    static void GenerateApiFile2(OpenApiDocument openApiDocument, string componentName, string typeNameFilter)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"import {{ {componentName} }} from '../../types/{componentName}.types';");
        sb.AppendLine("import fetchClient from '../fetchClient';");

        foreach (var path in openApiDocument.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                // Iterate through each response for the current operation
                foreach (var response in operation.Value.Responses)
                {
                    // Log or inspect the content of the response to understand its structure
                    Console.WriteLine($"Checking response for operation {operation.Key} on path {path.Key}");

                    foreach (var content in response.Value.Content)
                    {
                        if (content.Value.Schema.Reference != null)
                        {
                            Console.WriteLine($"Found schema reference: {content.Value.Schema.Reference.Id}");

                            if (typeNameFilter == null || content.Value.Schema.Reference.Id.Equals(typeNameFilter, StringComparison.OrdinalIgnoreCase))
                            {
                                var methodName = $"{operation.Key.ToString().ToLower()}{componentName}";
                                sb.AppendLine($"export const {methodName} = async (): Promise<{componentName}[]> => {{");
                                sb.AppendLine($"    const response = await fetchClient.{operation.Key.ToString().ToLower()}<{componentName}[]>('{path.Key}');");
                                sb.AppendLine("    return response;");
                                sb.AppendLine("};");
                                sb.AppendLine();
                            }
                        }
                        else
                        {
                            Console.WriteLine($"No schema reference found for content type {content.Key} in response.");
                        }
                    }
                }
            }
        }

        File.WriteAllText($"{componentName}.api.ts", sb.ToString());
    }



    static void GenerateSliceFile(string componentName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("import { createSlice, PayloadAction } from '@reduxjs/toolkit';");
        sb.AppendLine("import { Loaded } from '../../types/common.types';");
        sb.AppendLine("import { StoreState } from '../../store';");
        sb.AppendLine($"import {{ {componentName} }} from '../../types/{componentName}.types';");

        sb.AppendLine($"interface {componentName}State {{");
        sb.AppendLine($"    data: {componentName}[];");
        sb.AppendLine($"    loading: boolean;");
        sb.AppendLine($"    error: string | null;");
        sb.AppendLine("}");

        sb.AppendLine($"const initialState: {componentName}State = {{");
        sb.AppendLine("    data: [],");
        sb.AppendLine("    loading: false,");
        sb.AppendLine("    error: null,");
        sb.AppendLine("};");

        sb.AppendLine($"const {componentName}Slice = createSlice({{");
        sb.AppendLine($"    name: '{componentName}',");
        sb.AppendLine("    initialState,");
        sb.AppendLine("    reducers: {");
        sb.AppendLine($"        fetch{componentName}Requested(state, action: PayloadAction<{componentName}[]>) {{");
        sb.AppendLine("            state.loading = true;");
        sb.AppendLine("            state.loadingMessage = 'Fetching items..';");
        sb.AppendLine("    state.data: [];");
        sb.AppendLine("        },");
        sb.AppendLine($"        fetch{componentName}Succeeded(state, action: PayloadAction<{componentName}[]>) {{");
        sb.AppendLine("            state.data = action.payload;");
        sb.AppendLine("            state.loading = false;");
        sb.AppendLine("            state.loadingMessage = '';");
        sb.AppendLine("        },");
        sb.AppendLine("        fetch{componentName}Failed(state, action: PayloadAction<string>) {");
        sb.AppendLine("            state.error = action.payload;");
        sb.AppendLine("            state.loadingMessage = '';");
        sb.AppendLine("            state.loading = false;");
        sb.AppendLine("        },");
        sb.AppendLine("    },");
        sb.AppendLine("});");

        sb.AppendLine($"export const selected{componentName} = (state: StoreState): {componentName}[] => state.{componentName}.data;");
        sb.AppendLine($"export const {componentName.ToLower()}Loading = (state: StoreState): boolean => state.{componentName}.loading as boolean;");
        sb.AppendLine($"export default {componentName}Slice.reducer;");

        File.WriteAllText($"{componentName}Slice.ts", sb.ToString());
    }

    static void GenerateSagaFile(string componentName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("import { call, put, takeLatest } from 'redux-saga/effects';");
        sb.AppendLine($"import {{ fetchStart, fetchSuccess, fetchFailure }} from '../slices/{componentName}Slice';");
        sb.AppendLine($"import {{ fetch{componentName} }} from '../api/{componentName}.api';");

        sb.AppendLine($"function* fetch{componentName}Saga() {{");
        sb.AppendLine("    try {");
        sb.AppendLine("        yield put(fetchStart());");
        sb.AppendLine($"        const data = yield call(fetch{componentName});");
        sb.AppendLine("        yield put(fetchSuccess(data));");
        sb.AppendLine("    } catch (error) {");
        sb.AppendLine("        yield put(fetchFailure(error.message));");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        sb.AppendLine($"export function* watchFetch{componentName}() {{");
        sb.AppendLine($"    yield takeLatest('{componentName}/fetch', fetch{componentName}Saga);");
        sb.AppendLine("}");

        File.WriteAllText($"{componentName}.saga.ts", sb.ToString());
    }

    static void GenerateContainerAndContentFiles(string componentName)
    {
        // Container file
        var containerSb = new StringBuilder();
        containerSb.AppendLine("import React, { useEffect } from 'react';");
        containerSb.AppendLine($"import {{ useDispatch, useSelector }} from 'react-redux';");
        containerSb.AppendLine($"import {{ fetch{componentName} }} from '../../slices/{componentName}Slice';");
        containerSb.AppendLine($"import {componentName}Content from './{componentName}Content';");

        containerSb.AppendLine($"const {componentName}Container = () => {{");
        containerSb.AppendLine("    const dispatch = useDispatch();");
        containerSb.AppendLine($"    const data = useSelector(state => state.{componentName}.data);");

        containerSb.AppendLine("    useEffect(() => {");
        containerSb.AppendLine($"        dispatch(fetch{componentName}());");
        containerSb.AppendLine("    }, [dispatch]);");

        containerSb.AppendLine($"    return <{componentName}Content data={{data}} />;");
        containerSb.AppendLine("};");

        containerSb.AppendLine($"export default {componentName}Container;");

        File.WriteAllText($"{componentName}Container.tsx", containerSb.ToString());

        // Content file
        var contentSb = new StringBuilder();
        contentSb.AppendLine("import React from 'react';");

        contentSb.AppendLine($"const {componentName}Content = ({'{'} data {'}'}) => {{");
        contentSb.AppendLine("    return (");
        contentSb.AppendLine("        <div>");
        contentSb.AppendLine($"            <h1>{componentName} Data</h1>");
        contentSb.AppendLine("            <ul>");
        contentSb.AppendLine("                {data.map((item, idx) => (");
        contentSb.AppendLine("                    <li key={idx}>{JSON.stringify(item)}</li>");
        contentSb.AppendLine("                ))}");
        contentSb.AppendLine("            </ul>");
        contentSb.AppendLine("        </div>");
        contentSb.AppendLine("    );");
        contentSb.AppendLine("};");

        contentSb.AppendLine($"export default {componentName}Content;");

        File.WriteAllText($"{componentName}Content.tsx", contentSb.ToString());
    }
}
