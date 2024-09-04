using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using System;
using System.Text;
using System.Text.RegularExpressions;

public static class Program
{
    static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: ReactCodeGenerator <OpenApiSpecPath> <ComponentName> <BaseProjectPath>");
            return;
        }
        string openApiSpecUrl = args[0];
        string endPoint = args[1];
        string componentName = args[2];
        string basePath = args[3];
        //string typeNameFilter = args.Length > 2 ? args[2] : null;

        var openApiDocument = await LoadOpenApiDocumentFromUrl(openApiSpecUrl);

        if (openApiDocument != null)
        {
            //   GenerateTypesForEndpoint(openApiDocument, componentName, typeNameFilter);
            GenerateTypesForEndpoint3(openApiDocument, endPoint, componentName, basePath);
            //GenerateTypesFile(openApiDocument, componentName, typeNameFilter);
            GenerateApiFileNew(openApiDocument, endPoint, componentName, basePath);
            GenerateSliceFile(openApiDocument, endPoint, componentName, basePath);
            GenerateSagaFile(openApiDocument, endPoint, componentName, basePath);
            //GenerateContainerAndContentFiles(componentName);
        }
    }

    static async Task<OpenApiDocument> LoadOpenApiDocumentFromUrl(string url)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to fetch the OpenAPI document. Status Code: {response.StatusCode}");
        }

        var stream = await response.Content.ReadAsStreamAsync();
        var openApiDocument = new OpenApiStreamReader().Read(stream, out var diagnostic);

        if (diagnostic.Errors.Count > 0)
        {
            throw new Exception($"Failed to parse the OpenAPI document. Errors: {string.Join(", ", diagnostic.Errors)}");
        }

        return openApiDocument;
    }
    static void GenerateTypesForPostRequestNew(OpenApiDocument openApiDocument, string endpointPath, string componentName, string basePath)
    {
        var sb = new StringBuilder();
        var components = openApiDocument.Components.Schemas;

        if (openApiDocument.Paths.ContainsKey(endpointPath))
        {
            var pathItem = openApiDocument.Paths[endpointPath];

            //if (pathItem.Operations.ContainsKey(OperationType.Post))
            //{
            var operation = pathItem.Operations[OperationType.Post];

            // Process request body types
            if (operation.RequestBody?.Content != null)
            {
                foreach (var content in operation.RequestBody.Content)
                {
                    if (content.Key.Equals("application/json", StringComparison.OrdinalIgnoreCase) || content.Key.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
                    {
                        var schema = content.Value.Schema;
                        if (schema != null)
                        {
                            var typeName = $"{componentName}Request";
                            var typeDefinition = GenerateType(schema, typeName, components);
                            sb.Append(typeDefinition);
                        }
                    }
                }
            }

            // Process response types (optional, you can modify based on your needs)
            foreach (var response in operation.Responses)
            {
                foreach (var content in response.Value.Content)
                {
                    if (content.Key.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
                    {
                        var schema = content.Value.Schema;
                        if (schema != null)
                        {
                            var typeName = $"{componentName}Response";
                            var typeDefinition = GenerateType(schema, typeName, components);
                            sb.Append(typeDefinition);
                        }
                    }
                }
            }

            // Write the generated types to a file
            if (sb.Length > 0)
            {
                File.WriteAllText($"{basePath}/types/{componentName}.types.ts", sb.ToString());
            }
            else
            {
                Console.WriteLine($"No suitable content types found for POST request at endpoint {endpointPath}.");
            }

            //else
            //{
            //    Console.WriteLine($"POST operation not found for endpoint {endpointPath}.");
            //}
        }
        else
        {
            Console.WriteLine($"Endpoint {endpointPath} not found in the OpenAPI document.");
        }
    }
    static string GenerateTypeOld3(OpenApiSchema schema, string typeName, IDictionary<string, OpenApiSchema> components, HashSet<string> generatedTypes)
    {
        var sb = new StringBuilder();

        if (!generatedTypes.Contains(typeName))  // Ensure the type hasn't been generated yet
        {
            sb.AppendLine($"export interface {typeName} {{");

            foreach (var property in schema.Properties)
            {
                var propertyName = property.Key;
                var propertySchema = property.Value;

                if (propertySchema.Reference != null)
                {
                    var refTypeName = propertySchema.Reference.Id;
                    if (components.ContainsKey(refTypeName) && !generatedTypes.Contains(refTypeName))
                    {
                        var nestedType = GenerateType(components[refTypeName], refTypeName, components, generatedTypes);
                        sb.Insert(0, nestedType);  // Insert nested type at the beginning
                        generatedTypes.Add(refTypeName);  // Add nested type to the set of generated types
                    }
                    sb.AppendLine($"    {propertyName}: {refTypeName};");
                }
                else if (propertySchema.Type == "array" && propertySchema.Items.Reference != null)
                {
                    var refTypeName = propertySchema.Items.Reference.Id;
                    if (components.ContainsKey(refTypeName) && !generatedTypes.Contains(refTypeName))
                    {
                        var nestedType = GenerateType(components[refTypeName], refTypeName, components, generatedTypes);
                        sb.Insert(0, nestedType);  // Insert nested type at the beginning
                        generatedTypes.Add(refTypeName);  // Add nested type to the set of generated types
                    }
                    sb.AppendLine($"    {propertyName}: {refTypeName}[];");
                }
                else
                {
                    // Handle primitive types
                    var tsType = MapOpenApiTypeToTypeScript(propertySchema.Type, propertySchema.Format);
                    sb.AppendLine($"    {propertyName}: {tsType};");
                }
            }

            sb.AppendLine("}");
        }

        return sb.ToString();
    }
    static void GenerateTypesForEndpointOld2(OpenApiDocument openApiDocument, string endpointPath, string componentName, string basePath)
    {
        var sb = new StringBuilder();
        var components = openApiDocument.Components.Schemas;
        var generatedTypes = new HashSet<string>();  // Set to track generated types

        var filteredPaths = openApiDocument.Paths
       .Where(p => p.Key.Contains(endpointPath, StringComparison.OrdinalIgnoreCase))
       .ToDictionary(p => p.Key, p => p.Value);

        //if (openApiDocument.Paths.ContainsKey(endpointPath))
        //if (openApiDocument.Paths.ContainsKey(endpointPath))
        foreach (var key in filteredPaths.Keys)
        {
            var pathItem = openApiDocument.Paths[key];

            foreach (var operation in pathItem.Operations)
            {
                // Process request body types (for POST, PUT, etc.)
                if (operation.Value.RequestBody?.Content != null)
                {
                    foreach (var content in operation.Value.RequestBody.Content)
                    {

                        var schema = content.Value.Schema;
                        if (schema != null)
                        {
                            var typeName = $"{componentName}{operation.Key.ToString().ToLower()}";
                            if (!generatedTypes.Contains(typeName))  // Check if type is already generated
                            {
                                var typeDefinition = GenerateType(schema, typeName, components, generatedTypes);
                                sb.Append(typeDefinition);
                                generatedTypes.Add(typeName);  // Add to the set of generated types
                            }
                        }

                    }
                }

                // Process response types for all operations
                foreach (var response in operation.Value.Responses)
                {
                    foreach (var content in response.Value.Content)
                    {
                        var schema = content.Value.Schema;
                        if (schema != null)
                        {
                            var typeName = $"{componentName}{operation.Key.ToString().ToLower()}";
                            if (!generatedTypes.Contains(typeName))  // Check if type is already generated
                            {
                                var typeDefinition = GenerateType(schema, typeName, components, generatedTypes);
                                sb.Append(typeDefinition);
                                generatedTypes.Add(typeName);  // Add to the set of generated types
                            }
                        }
                    }
                }
            }

            // Write the generated types to a file
            if (sb.Length > 0)
            {
                File.WriteAllText($"{basePath}/types/{componentName}.types.ts", sb.ToString());
            }
            else
            {
                Console.WriteLine($"No suitable content types found for operations at endpoint {endpointPath}.");
            }
        }
        //else
        //{
        //    Console.WriteLine($"Endpoint {endpointPath} not found in the OpenAPI document.");
        //}
    }
    static void GenerateTypesForEndpoint(OpenApiDocument openApiDocument, string endpointPath, string componentName)
    {
        var sb = new StringBuilder();
        var components = openApiDocument.Components.Schemas;

        if (openApiDocument.Paths.ContainsKey(endpointPath))
        {
            var pathItem = openApiDocument.Paths[endpointPath];

            foreach (var operation in pathItem.Operations)
            {
                // Process request body types for text/plain
                if (operation.Value.RequestBody?.Content != null)
                {
                    foreach (var content in operation.Value.RequestBody.Content)
                    {
                        if (content.Key.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
                        {
                            var schema = content.Value.Schema;
                            if (schema != null)
                            {
                                var typeName = $"{componentName}Request";
                                var typeDefinition = GenerateType(schema, typeName, components);
                                sb.Append(typeDefinition);
                            }
                        }
                    }
                }

                // Process response types for text/plain
                foreach (var response in operation.Value.Responses)
                {
                    foreach (var content in response.Value.Content)
                    {
                        if (content.Key.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
                        {
                            var schema = content.Value.Schema;
                            if (schema != null)
                            {
                                var typeName = $"{componentName}Response";
                                var typeDefinition = GenerateType(schema, typeName, components);
                                sb.Append(typeDefinition);
                            }
                        }
                    }
                }
            }

            // Write the generated types to a file
            if (sb.Length > 0)
            {
                File.WriteAllText($"{componentName}.types.ts", sb.ToString());
            }
            else
            {
                Console.WriteLine($"No text/plain content types found for endpoint {endpointPath}.");
            }
        }
        else
        {
            Console.WriteLine($"Endpoint {endpointPath} not found in the OpenAPI document.");
        }
    }


    static void GenerateTypesFile(OpenApiDocument openApiDocument, string componentName, string typeNameFilter)
    {
        var sb = new StringBuilder();

        foreach (var schema in openApiDocument.Components.Schemas)
        {
            if (typeNameFilter != null && !schema.Key.Equals(typeNameFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var typeDefinition = GenerateType(schema.Value, schema.Key, openApiDocument.Components.Schemas);
            sb.Append(typeDefinition);
        }

        File.WriteAllText($"{componentName}.types.ts", sb.ToString());
    }

    static string GenerateType(OpenApiSchema schema, string typeName, IDictionary<string, OpenApiSchema> components)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"export interface {typeName} {{");

        foreach (var property in schema.Properties)
        {
            var propertyName = property.Key;
            var propertySchema = property.Value;

            if (propertySchema.Reference != null)
            {
                var refTypeName = propertySchema.Reference.Id;
                if (components.ContainsKey(refTypeName))
                {
                    var nestedType = GenerateType(components[refTypeName], refTypeName, components);
                    sb.Insert(0, nestedType);
                }
                sb.AppendLine($"    {propertyName}: {refTypeName};");
            }
            else if (propertySchema.Type == "array" && propertySchema.Items.Reference != null)
            {
                var refTypeName = propertySchema.Items.Reference.Id;
                if (components.ContainsKey(refTypeName))
                {
                    var nestedType = GenerateType(components[refTypeName], refTypeName, components);
                    sb.Insert(0, nestedType);
                }
                sb.AppendLine($"    {propertyName}: {refTypeName}[];");
            }
            else
            {
                var tsType = MapOpenApiTypeToTypeScript(propertySchema.Type, propertySchema.Format);
                sb.AppendLine($"    {propertyName}: {tsType};");
            }
        }

        sb.AppendLine("}");

        return sb.ToString();
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

    static void GenerateApiFile(OpenApiDocument openApiDocument, string componentName, string typeNameFilter, string basePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"import {{ {componentName} }} from '../types/{componentName}.types';");
        sb.AppendLine("import fetchClient from '../fetchClient';");

        foreach (var path in openApiDocument.Paths)
        {
            foreach (var operation in path.Value.Operations)
            {
                foreach (var response in operation.Value.Responses)
                {
                    foreach (var content in response.Value.Content)
                    {
                        var schema = content.Value.Schema;
                        if (schema == null)
                        {
                            // Skip if the schema is null
                            continue;
                        }

                        if (schema.Reference != null)
                        {
                            if (typeNameFilter == null || schema.Reference.Id.Equals(typeNameFilter, StringComparison.OrdinalIgnoreCase))
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
                            // Handle inline schemas (non-referenced types)
                            var inlineTypeName = $"{componentName}InlineType";
                            var inlineTypeDefinition = GenerateType(schema, inlineTypeName, openApiDocument.Components.Schemas);
                            sb.AppendLine(inlineTypeDefinition);

                            var methodName = $"{operation.Key.ToString().ToLower()}{componentName}Inline";
                            sb.AppendLine($"export const {methodName} = async (): Promise<{inlineTypeName}[]> => {{");
                            sb.AppendLine($"    const response = await fetchClient.{operation.Key.ToString().ToLower()}<{inlineTypeName}[]>('{path.Key}');");
                            sb.AppendLine("    return response;");
                            sb.AppendLine("};");
                            sb.AppendLine();
                        }
                    }
                }
            }
        }
        File.WriteAllText($"{basePath}/types/{componentName}.api.ts", sb.ToString());
        //File.WriteAllText($"{componentName}.api.ts", sb.ToString());
    }
    static void GenerateApiFileNew(OpenApiDocument openApiDocument, string endpointPath, string componentName, string basePath)
    {
        var sb = new StringBuilder();
        var usedTypes = new HashSet<string>();  // Track which types are actually used

        // Filter paths based on the input endpointPath parameter
        var filteredPaths = openApiDocument.Paths
            .Where(p => p.Key.Contains(endpointPath, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(p => p.Key, p => p.Value);

        foreach (var path in filteredPaths)
        {
            foreach (var operation in path.Value.Operations)
            {
                // Use the operationId as the method name if available
                string methodName;
                if (!string.IsNullOrEmpty(operation.Value.OperationId))
                {
                    methodName = operation.Value.OperationId;
                }
                else
                {
                    // Fallback: Use HTTP method + path segments for unique method names
                    methodName = $"{string.Join("", path.Key.Split('/').Select(s => CapitalizeFirstChar(s)))}";
                }

                // Ensure method names are camelCase
                methodName = char.ToLowerInvariant(methodName[0]) + methodName.Substring(1);

                // Initialize types
                string payloadType = "any";
                string returnType = null;

                // Process the request body type for POST/PUT methods
                if ((operation.Key == OperationType.Post || operation.Key == OperationType.Put) && operation.Value.RequestBody?.Content != null)
                {
                    foreach (var content in operation.Value.RequestBody.Content)
                    {
                        if (content.Value.Schema?.Reference != null)
                        {
                            payloadType = content.Value.Schema.Reference.Id;
                            usedTypes.Add(payloadType);
                            break;  // Use the first matching content type found
                        }
                        //else if (content.Value.Schema != null)
                        //{
                        //    // Handle inline schemas by generating a type
                        //    payloadType = $"{methodName}Request";
                        //    var inlineTypeDefinition = GenerateType(content.Value.Schema, payloadType, openApiDocument.Components.Schemas, usedTypes);
                        //    sb.Append(inlineTypeDefinition);
                        //    usedTypes.Add(payloadType);
                        //    break;  // Use the first matching content type found
                        //}
                    }
                }

                // Process the response type for all methods
                foreach (var response in operation.Value.Responses)
                {
                    foreach (var content in response.Value.Content)
                    {
                        if (content.Value.Schema?.Reference != null)
                        {
                            returnType = content.Value.Schema.Reference.Id;
                            usedTypes.Add(returnType);
                            break;  // Use the first matching content type found
                        }
                        else if (content.Value.Schema != null)
                        {
                            // Handle inline schemas by generating a type
                            returnType = $"{methodName}Response";
                            var inlineTypeDefinition = GenerateType(content.Value.Schema, returnType, openApiDocument.Components.Schemas, usedTypes);
                            sb.Append(inlineTypeDefinition);
                            usedTypes.Add(returnType);
                            break;  // Use the first matching content type found
                        }
                    }
                }

                // Handle the different HTTP methods and generate appropriate API functions
                switch (operation.Key)
                {
                    case OperationType.Get:
                        sb.AppendLine($"export const {methodName} = async (): Promise<{returnType}[]> => {{");
                        sb.AppendLine($"    const response = await fetchClient.get<{returnType}[]>('{path.Key}');");
                        sb.AppendLine("    return response;");
                        sb.AppendLine("};");
                        break;

                    case OperationType.Post:
                        sb.AppendLine($"export const {methodName} = async (payload: {payloadType}): Promise<{returnType}> => {{");
                        sb.AppendLine($"    const response = await fetchClient.post<{returnType}>('{path.Key}', payload);");
                        sb.AppendLine("    return response;");
                        sb.AppendLine("};");
                        break;

                    case OperationType.Put:
                        sb.AppendLine($"export const {methodName} = async (payload: {payloadType}): Promise<{returnType}> => {{");
                        sb.AppendLine($"    const response = await fetchClient.put<{returnType}>('{path.Key}', payload);");
                        sb.AppendLine("    return response;");
                        sb.AppendLine("};");
                        break;

                    case OperationType.Delete:
                        sb.AppendLine($"export const {methodName} = async (id: string): Promise<{returnType}> => {{");
                        sb.AppendLine($"    const response = await fetchClient.delete<{returnType}>(`{path.Key}/${{id}}`);");
                        sb.AppendLine("    return response;");
                        sb.AppendLine("};");
                        break;

                    default:
                        Console.WriteLine($"Unsupported operation type: {operation.Key}");
                        break;
                }

                sb.AppendLine(); // Add a blank line between methods
            }
        }

        // Create the dynamic import statement based on the types that were actually used
        if (usedTypes.Count > 0)
        {
            var importLine = $"import {{ {string.Join(", ", usedTypes)} }} from '../types/{componentName}.types';";
            sb.Insert(0, importLine + Environment.NewLine + "import fetchClient from './fetchClient';" + Environment.NewLine);
        }

        // Write the generated API file to the specified basePath
        var outputPath = Path.Combine(basePath, "api", $"{componentName}.api.ts");
      //  Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllText(outputPath, sb.ToString());
    }

   


    // Helper method to capitalize the first character of a string
    


    // Extension method to capitalize the first character of each segment (used for fallback method name generation)
    static string FirstCharToUpper(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return input.First().ToString().ToUpper() + input.Substring(1);
    }



    static void GenerateApiFileNewOld(OpenApiDocument openApiDocument, string endpointPath, string componentName, string basePath)
    {
        var sb = new StringBuilder();
        var usedTypes = new HashSet<string>();  // Track which types are actually used
        var filteredPaths = openApiDocument.Paths
        .Where(p => p.Key.Contains(endpointPath, StringComparison.OrdinalIgnoreCase))
        .ToDictionary(p => p.Key, p => p.Value);

        foreach (var path in filteredPaths)
        {
            foreach (var operation in path.Value.Operations)
            {
                var methodName = $"{operation.Key.ToString().ToLower()}{componentName}";
                var returnType = $"{componentName}";
                var payloadType = $"{componentName}";

                // Add the types to the usedTypes set if they are going to be used
                if (operation.Key == OperationType.Get || operation.Key == OperationType.Delete)
                {
                    usedTypes.Add(returnType);
                }
                else if (operation.Key == OperationType.Post || operation.Key == OperationType.Put)
                {
                    usedTypes.Add(payloadType);
                    usedTypes.Add(returnType);
                }

                // Handle the different HTTP methods and generate appropriate API functions
                switch (operation.Key)
                {
                    case OperationType.Get:
                        sb.AppendLine($"export const fetch{componentName} = async (): Promise<{returnType}[]> => {{");
                        sb.AppendLine($"    const response = await fetchClient.get<{returnType}[]>('{path.Key}');");
                        sb.AppendLine("    return response;");
                        sb.AppendLine("};");
                        break;

                    case OperationType.Post:
                        sb.AppendLine($"export const create{componentName} = async (payload: {payloadType}): Promise<{returnType}> => {{");
                        sb.AppendLine($"    const response = await fetchClient.post<{returnType}>('{path.Key}', payload);");
                        sb.AppendLine("    return response;");
                        sb.AppendLine("};");
                        break;

                    case OperationType.Put:
                        sb.AppendLine($"export const update{componentName} = async (payload: {payloadType}): Promise<{returnType}> => {{");
                        sb.AppendLine($"    const response = await fetchClient.put<{returnType}>('{path.Key}', payload);");
                        sb.AppendLine("    return response;");
                        sb.AppendLine("};");
                        break;

                    case OperationType.Delete:
                        sb.AppendLine($"export const delete{componentName} = async (id: string): Promise<{returnType}> => {{");
                        sb.AppendLine($"    const response = await fetchClient.delete<{returnType}>(`{path.Key}/${{id}}`);");
                        sb.AppendLine("    return response;");
                        sb.AppendLine("};");
                        break;

                    default:
                        Console.WriteLine($"Unsupported operation type: {operation.Key}");
                        break;
                }

                sb.AppendLine(); // Add a blank line between methods
            }
        }

        // Create the dynamic import statement based on the types that were actually used
        var importLine = $"import {{ {string.Join(", ", usedTypes)} }} from '../../types/{componentName}.types';";
        sb.Insert(0, importLine + Environment.NewLine + "import fetchClient from '../fetchClient';" + Environment.NewLine);

        // Write the generated API file to the specified basePath
        //var outputPath = Path.Combine(basePath, "types", $"{componentName}.api.ts");
        //Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        //File.WriteAllText(outputPath, sb.ToString());
        File.WriteAllText($"{basePath}/api/{componentName}.api.ts", sb.ToString());
    }
    static void GenerateTypesForEndpoint3(OpenApiDocument openApiDocument, string endpointPath, string componentName, string basePath)
    {
        var sb = new StringBuilder();
        var components = openApiDocument.Components.Schemas;
        var generatedTypes = new HashSet<string>();  // Set to track generated types
        var filteredPaths = openApiDocument.Paths
           .Where(p => p.Key.Contains(endpointPath, StringComparison.OrdinalIgnoreCase))
           .ToDictionary(p => p.Key, p => p.Value);

        foreach (var key in filteredPaths.Keys)
        {
            var pathItem = openApiDocument.Paths[key];

            foreach (var operation in pathItem.Operations)
            {
                // Process request body types for all operations
                if (operation.Value.RequestBody?.Content != null)
                {
                    foreach (var content in operation.Value.RequestBody.Content)
                    {
                        var schema = content.Value.Schema;
                        if (schema != null)
                        {
                            var typeName = schema.Reference?.Id ?? $"{operation.Key.ToString().ToLower()}{componentName}Request";
                            if (!generatedTypes.Contains(typeName))
                            {
                                var typeDefinition = GenerateType(schema, typeName, components, generatedTypes);
                                sb.Append(typeDefinition);
                                generatedTypes.Add(typeName);  // Add to the set of generated types
                            }
                        }
                    }
                }

                // Process response types for all operations
                foreach (var response in operation.Value.Responses)
                {
                    foreach (var content in response.Value.Content)
                    {
                        var schema = content.Value.Schema;
                        if (schema != null)
                        {
                            var typeName = schema.Reference?.Id ?? $"{operation.Key.ToString().ToLower()}{componentName}Response";
                            if (!generatedTypes.Contains(typeName))
                            {
                                var typeDefinition = GenerateType(schema, typeName, components, generatedTypes);
                                sb.Append(typeDefinition);
                                generatedTypes.Add(typeName);  // Add to the set of generated types
                            }
                        }
                    }
                }
            }
        }

        // Write the generated types to a file
        if (sb.Length > 0)
        {
            Directory.CreateDirectory($"{basePath}/types");
            File.WriteAllText($"{basePath}/types/{componentName}.types.ts", sb.ToString());
        }
        else
        {
            Console.WriteLine($"No types generated for endpoint {endpointPath}. Possibly no valid schema references found.");
        }
    }

    static string GenerateType(OpenApiSchema schema, string typeName, IDictionary<string, OpenApiSchema> components, HashSet<string> generatedTypes)
    {
        var sb = new StringBuilder();

        if (!generatedTypes.Contains(typeName))  // Ensure the type hasn't been generated yet
        {
            sb.AppendLine($"export type {typeName} = {{");

            foreach (var property in schema.Properties)
            {
                var propertyName = property.Key;
                var propertySchema = property.Value;

                if (propertySchema.Reference != null)
                {
                    var refTypeName = propertySchema.Reference.Id;
                    if (components.ContainsKey(refTypeName) && !generatedTypes.Contains(refTypeName))
                    {
                        var nestedType = GenerateType(components[refTypeName], refTypeName, components, generatedTypes);
                        sb.Insert(0, nestedType);  // Insert nested type at the beginning
                        generatedTypes.Add(refTypeName);  // Add nested type to the set of generated types
                    }
                    sb.AppendLine($"    {propertyName}: {refTypeName};");
                }
                else if (propertySchema.Type == "array" && propertySchema.Items.Reference != null)
                {
                    var refTypeName = propertySchema.Items.Reference.Id;
                    if (components.ContainsKey(refTypeName) && !generatedTypes.Contains(refTypeName))
                    {
                        var nestedType = GenerateType(components[refTypeName], refTypeName, components, generatedTypes);
                        sb.Insert(0, nestedType);  // Insert nested type at the beginning
                        generatedTypes.Add(refTypeName);  // Add nested type to the set of generated types
                    }
                    sb.AppendLine($"    {propertyName}: {refTypeName}[];");
                }
                else
                {
                    // Handle primitive types
                    var tsType = MapOpenApiTypeToTypeScript(propertySchema.Type, propertySchema.Format);
                    sb.AppendLine($"    {propertyName}: {tsType};");
                }
            }

            sb.AppendLine("}");
        }

        return sb.ToString();
    }



    static void GenerateSliceFile(OpenApiDocument openApiDocument, string endpointPath, string componentName, string basePath)
    {
        var sb = new StringBuilder();
        var usedTypes = new HashSet<string>();  // Track which types are actually used
        var stateProperties = new Dictionary<string, string>();  // For storing method names and their return types

        // Add imports
        sb.AppendLine("import { createSlice, PayloadAction } from '@reduxjs/toolkit';");
        sb.AppendLine("import { StoreState } from '../store';");
        sb.AppendLine();

        // Initialize state interface
        sb.AppendLine($"interface {componentName}State {{");

        // Filter paths based on the input endpointPath parameter
        var filteredPaths = openApiDocument.Paths
            .Where(p => p.Key.Contains(endpointPath, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(p => p.Key, p => p.Value);

        foreach (var path in filteredPaths)
        {
            foreach (var operation in path.Value.Operations)
            {
                // Generate method names based on operationId or fallback logic
                string methodName;
                if (!string.IsNullOrEmpty(operation.Value.OperationId))
                {
                    methodName = operation.Value.OperationId;
                }
                else
                {
                    // Fallback: Use path segments for unique method names
                    methodName = $"{string.Join("", path.Key.Split('/').Select(s => CapitalizeFirstChar(s)))}";
                }

                // Ensure method names are camelCase and remove curly braces
                methodName = char.ToLowerInvariant(methodName[0]) + methodName.Substring(1);
                methodName = methodName.Replace("{", "").Replace("}", "");

                // Determine the correct response type for the reducer
                string responseType = $"{componentName}[]";

                foreach (var response in operation.Value.Responses)
                {
                    foreach (var content in response.Value.Content)
                    {
                        if (content.Value.Schema?.Reference != null)
                        {
                            responseType = content.Value.Schema.Reference.Id;
                            usedTypes.Add(responseType);
                            stateProperties[methodName] = responseType + " | null";
                        }
                        else if (content.Value.Schema?.Properties.Count > 0)
                        {
                            responseType = $"{methodName}Response";
                            usedTypes.Add(responseType);
                            stateProperties[methodName] = responseType + " | null";
                        }
                    }
                }

                // Debug: Print the method name and the corresponding state property
                Console.WriteLine($"Generated method name: {methodName}, Response type: {responseType}");

                // Generate state interface properties
                sb.AppendLine($"    {methodName}: {responseType} | null;");
            }
        }

        // Add loading and error states
        sb.AppendLine("    loading: boolean;");
        sb.AppendLine("    error: string | null;");
        sb.AppendLine("}");

        // Begin slice
        sb.AppendLine();
        sb.AppendLine($"const initialState: {componentName}State = {{");

        // Generate the initialState values based on the response types
        foreach (var stateProperty in stateProperties)
        {
            sb.AppendLine($"    {stateProperty.Key}: null,");
        }
        sb.AppendLine("    loading: false,");
        sb.AppendLine("    error: null,");
        sb.AppendLine("};");

        sb.AppendLine();
        sb.AppendLine($"const {componentName}Slice = createSlice({{");
        sb.AppendLine($"    name: '{componentName}',");
        sb.AppendLine("    initialState,");
        sb.AppendLine("    reducers: {");

        foreach (var path in filteredPaths)
        {
            foreach (var operation in path.Value.Operations)
            {
                string methodName;
                if (!string.IsNullOrEmpty(operation.Value.OperationId))
                {
                    methodName = operation.Value.OperationId;
                }
                else
                {
                    methodName = $"{string.Join("", path.Key.Split('/').Select(s => CapitalizeFirstChar(s)))}";
                }

                methodName = char.ToLowerInvariant(methodName[0]) + methodName.Substring(1);
                methodName = methodName.Replace("{", "").Replace("}", "");  // Remove curly braces

                // Check if the generated methodName exists in the stateProperties dictionary
                //if (!stateProperties.ContainsKey(methodName))
                //{
                //    throw new KeyNotFoundException($"The generated method name '{methodName}' was not found in the state properties dictionary.");
                //}

                sb.AppendLine($"        {methodName}Requested(state) {{");
                sb.AppendLine("            state.loading = true;");
                sb.AppendLine("            state.error = null;");
                sb.AppendLine("        },");
                sb.AppendLine();

                sb.AppendLine($"        {methodName}Succeeded(state, action: PayloadAction<{stateProperties[methodName].Replace(" | null", "")}>) {{");
                sb.AppendLine($"            state.{methodName} = action.payload;");
                sb.AppendLine("            state.loading = false;");
                sb.AppendLine("        },");
                sb.AppendLine();

                sb.AppendLine($"        {methodName}Failed(state, action: PayloadAction<string>) {{");
                sb.AppendLine("            state.loading = false;");
                sb.AppendLine("            state.error = action.payload;");
                sb.AppendLine("        },");
                sb.AppendLine();
            }
        }

        // End reducers and slice
        sb.AppendLine("    },");
        sb.AppendLine("});");

        // Add selectors
        sb.AppendLine();
        foreach (var stateProperty in stateProperties)
        {
            sb.AppendLine($"export const select{CapitalizeFirstChar(stateProperty.Key)} = (state: StoreState): {stateProperty.Value} => state.{componentName}.{stateProperty.Key};");
        }
       // sb.AppendLine($"export const select{componentName}Loading = (state: StoreState): boolean => state.{componentName}.loading;");
       // sb.AppendLine($"export const select{componentName}Error = (state: StoreState): string | null => state.{componentName}.error;");

        // Export actions and reducer
        sb.AppendLine($"export const {{");
        foreach (var stateProperty in stateProperties)
        {
            sb.AppendLine($"    {stateProperty.Key}Requested,");
            sb.AppendLine($"    {stateProperty.Key}Succeeded,");
            sb.AppendLine($"    {stateProperty.Key}Failed,");
        }
        sb.AppendLine($"}} = {componentName}Slice.actions;");
        sb.AppendLine();
        sb.AppendLine($"export default {componentName}Slice.reducer;");

        // Insert dynamic imports based on used types
        if (usedTypes.Count > 0)
        {
            var importLine = $"import {{ {string.Join(", ", usedTypes)} }} from '../types/{componentName}.types';";
            sb.Insert(0, importLine + Environment.NewLine);
        }
      

        // Write the generated slice to a file
        var outputPath = Path.Combine(basePath, "slices", $"{componentName}Slice.ts");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllText(outputPath, sb.ToString());
    }

   







    // Helper method to capitalize the first character of a string




    static void GenerateSliceFileOld(string componentName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("import { createSlice, PayloadAction } from '@reduxjs/toolkit';");
        sb.AppendLine("import { StoreState } from '../store';");
        sb.AppendLine($"import {{ {componentName} }} from '../types/{componentName}.types';");

        sb.AppendLine($"interface {componentName}State {{");
        sb.AppendLine($"    data: {componentName}[];");
        sb.AppendLine($"    loading: boolean;");
        sb.AppendLine($"    loadingMessage: string | null;");
        sb.AppendLine($"    error: string | null;");
        sb.AppendLine("}");

        sb.AppendLine($"const initialState: {componentName}State = {{");
        sb.AppendLine("    data: [],");
        sb.AppendLine("    loading: false,");
        sb.AppendLine("    error: null,");
        sb.AppendLine("    loadingMessage: null,");
        sb.AppendLine("};");

        sb.AppendLine($"const {componentName}Slice = createSlice({{");
        sb.AppendLine($"    name: '{componentName}',");
        sb.AppendLine("    initialState,");
        sb.AppendLine("    reducers: {");
        sb.AppendLine($"        fetch{componentName}Requested(state) {{");
        sb.AppendLine("            state.loading = true;");
        sb.AppendLine("            state.loadingMessage = 'Fetching items..';");
        sb.AppendLine("            state.data = [];");
        sb.AppendLine("        },");
        sb.AppendLine($"        fetch{componentName}Succeeded(state, action: PayloadAction<{componentName}[]>) {{");
        sb.AppendLine("            state.data = action.payload;");
        sb.AppendLine("            state.loading = false;");
        sb.AppendLine("            state.loadingMessage = '';");
        sb.AppendLine("        },");
        sb.AppendLine($"        fetch{componentName}Failed(state, action: PayloadAction<string>) {{");
        sb.AppendLine("            state.error = action.payload;");
        sb.AppendLine("            state.loading = false;");
        sb.AppendLine("            state.loadingMessage = '';");
        sb.AppendLine("        },");
        sb.AppendLine("        clearBestSellingState(state) {");
        sb.AppendLine("            state.data = [];");
        sb.AppendLine("            state.error = null;");
        sb.AppendLine("            state.loadingMessage = null;");
        sb.AppendLine("            state.loading = false;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("});");

        sb.AppendLine($"export const {{ fetch{componentName}Requested, fetch{componentName}Succeeded, fetch{componentName}Failed, clearBestSellingState }} = {componentName}Slice.actions;");
        sb.AppendLine($"export const selected{componentName} = (state: StoreState): {componentName}[] => state.{componentName}.data;");
        sb.AppendLine($"export const {componentName.ToLower()}Loading = (state: StoreState): boolean => state.{componentName}.loading;");
        sb.AppendLine($"export default {componentName}Slice.reducer;");

        File.WriteAllText($"{componentName}Slice.ts", sb.ToString());
    }
    static void GenerateSagaFile(OpenApiDocument openApiDocument, string endpointPath, string componentName, string basePath)
    {
        var sb = new StringBuilder();
        var usedTypes = new HashSet<string>();  // Track which types are actually used
        var methodNames = new List<string>();   // Track method names to generate saga watchers
        var sliceActions = new List<string>();  // Track slice actions to import
        var apiImports = new List<string>();    // Track API method imports

        // Filter paths based on the input endpointPath parameter
        var filteredPaths = openApiDocument.Paths
            .Where(p => p.Key.Contains(endpointPath, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(p => p.Key, p => p.Value);

        foreach (var path in filteredPaths)
        {
            foreach (var operation in path.Value.Operations)
            {
                string methodName;
                if (!string.IsNullOrEmpty(operation.Value.OperationId))
                {
                    methodName = operation.Value.OperationId;
                }
                else
                {
                    methodName = $"{string.Join("", path.Key.Split('/').Select(s => CapitalizeFirstChar(s)))}";
                }

                methodName = char.ToLowerInvariant(methodName[0]) + methodName.Substring(1);
                methodName = methodName.Replace("{", "").Replace("}", "");

                // Add method name for saga watcher
                methodNames.Add(methodName);

                // Add slice actions
                sliceActions.Add($"{methodName}Requested");
                sliceActions.Add($"{methodName}Succeeded");
                sliceActions.Add($"{methodName}Failed");

                // Add API method import
                apiImports.Add(methodName);

                // Determine response and request types
                string responseType = $"{componentName}";
                string payloadType = "void";  // Default to void if no payload is needed

                if (operation.Key == OperationType.Post || operation.Key == OperationType.Put || operation.Key == OperationType.Patch)
                {
                    foreach (var content in operation.Value.RequestBody?.Content)
                    {
                        if (content.Value.Schema?.Reference != null)
                        {
                            payloadType = content.Value.Schema.Reference.Id;
                            usedTypes.Add(payloadType);
                        }
                    }
                }

                foreach (var response in operation.Value.Responses)
                {
                    foreach (var content in response.Value.Content)
                    {
                        if (content.Value.Schema?.Reference != null)
                        {
                            responseType = content.Value.Schema.Reference.Id;
                            usedTypes.Add(responseType);
                        }
                    }
                }

                // Generate saga function for each operation
                sb.AppendLine($"function* waitFor{CapitalizeFirstChar(methodName)}() {{");
                sb.AppendLine($"  yield takeLatest({methodName}Requested, function* ({{ payload }}: PayloadAction<{payloadType}>) {{");
                sb.AppendLine("    try {");
                sb.AppendLine($"      const data: {responseType} = yield call({methodName}, payload);");
                sb.AppendLine($"      yield put({methodName}Succeeded(data));");
                sb.AppendLine("    } catch (e) {");
                sb.AppendLine("      const error = e as unknown as Error;");
                sb.AppendLine($"      yield put({methodName}Failed(error.message));");
                sb.AppendLine("    }");
                sb.AppendLine("  }});");
                sb.AppendLine("}");
                sb.AppendLine();
            }
        }

        // Add imports for slice actions and API functions
        sb.Insert(0, $"import {{ {string.Join(", ", sliceActions)} }} from '../slices/{componentName}Slice';\n");
        sb.Insert(0, $"import {{ {string.Join(", ", apiImports)} }} from '../api/{componentName}.api';\n");
        sb.Insert(0, "import { all, call, put, takeLatest } from 'redux-saga/effects';\n");

        if (usedTypes.Count > 0)
        {
            var importLine = $"import {{ {string.Join(", ", usedTypes)} }} from '../types/{componentName}.types';";
            sb.Insert(0, importLine + Environment.NewLine);
        }

        // Add the saga watcher functions
        sb.AppendLine("function* saga() {");
        sb.AppendLine("  yield all([");
        foreach (var method in methodNames)
        {
            sb.AppendLine($"    call(waitFor{CapitalizeFirstChar(method)}),");
        }
        sb.AppendLine("  ]);");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("export default saga;");

        // Write the generated saga file to a file
        var outputPath = Path.Combine(basePath, "sagas", $"{componentName}.saga.ts");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllText(outputPath, sb.ToString());
    }

    // Helper method to capitalize the first character of a string
    static string CapitalizeFirstChar(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return char.ToUpper(input[0]) + input.Substring(1);
    }


    static void GenerateSagaFile(string componentName)
    {
        var sb = new StringBuilder();

        sb.AppendLine("import { all, call, put, takeLatest } from 'redux-saga/effects';");
        sb.AppendLine($"import type {{ {componentName} }} from '../types/{componentName.ToLower()}.types';");
        sb.AppendLine();
        sb.AppendLine($"import {{ fetch{componentName} }} from '../api/{componentName.ToLower()}.api';");
        sb.AppendLine($"import {{");
        sb.AppendLine($"    fetch{componentName}Requested,");
        sb.AppendLine($"    fetch{componentName}Succeeded,");
        sb.AppendLine($"    fetch{componentName}Failed,");
        sb.AppendLine($"}} from '../slices/{componentName.ToLower()}Slice';");
        sb.AppendLine();
        sb.AppendLine($"function* fetch{componentName}Saga() {{");
        sb.AppendLine("    try {");
        sb.AppendLine($"        const data: {componentName}[] = yield call(fetch{componentName});");
        sb.AppendLine($"        yield put(fetch{componentName}Succeeded(data));");
        sb.AppendLine("    } catch (error) {");
        sb.AppendLine($"        yield put(fetch{componentName}Failed((error as Error).message));");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("export default function* rootSaga() {");
        sb.AppendLine("    yield all([");
        sb.AppendLine($"        takeLatest(fetch{componentName}Requested.type, fetch{componentName}Saga),");
        sb.AppendLine("    ]);");
        sb.AppendLine("}");

        File.WriteAllText($"{componentName}.saga.ts", sb.ToString());
    }

    static void GenerateContainerAndContentFiles(string componentName)
    {
        var containerContent = $@"
import React, {{ useEffect }} from 'react';
import {{ useDispatch, useSelector }} from 'react-redux';
import {{ fetch{componentName}Requested, selected{componentName}, {componentName.ToLower()}Loading }} from '../slices/{componentName}Slice';
import {componentName}Content from './{componentName}Content';

const {componentName}Container: React.FC = () => {{
    const dispatch = useDispatch();
    const data = useSelector(selected{componentName});
    const isLoading = useSelector({componentName.ToLower()}Loading);

    useEffect(() => {{
        dispatch(fetch{componentName}Requested());
    }}, [dispatch]);

    return (
        <div>
            <h1>{componentName} Container</h1>
            {componentName}Content data={{data}} isLoading={{isLoading}} />
        </div>
    );
}}

export default {componentName}Container;
";

        File.WriteAllText($"{componentName}Container.tsx", containerContent);

        var contentContent = $@"
import React from 'react';
import {{ {componentName} }} from '../types/{componentName}';

interface Props {{
    data: {componentName}[];
    isLoading: boolean;
}}

const {componentName}Content: React.FC<Props> = ({{ data, isLoading }}) => {{
    if (isLoading) {{
        return <div>Loading...</div>;
    }}

    return (
        <div>
            <h2>{componentName} Content</h2>
            <ul>
                {{data.map((item, index) => (
                    <li key={{index}}>{{item}}</li>
                ))}}
            </ul>
        </div>
    );
}}

export default {componentName}Content;
";

        File.WriteAllText($"{componentName}Content.tsx", contentContent);
    }
}
