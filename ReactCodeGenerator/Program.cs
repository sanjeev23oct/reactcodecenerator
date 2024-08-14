using NJsonSchema.CodeGeneration.TypeScript;
using NSwag;
using NSwag.CodeGeneration.TypeScript;
using Scriban;
using System.IO;
using System.Text.Json;

namespace ReactCodeGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Load the OpenAPI spec
            var document = await OpenApiDocument.FromFileAsync(@"D:\rssb\repos\openapi.json");

            // Generate TypeScript types
            var typescriptTypes = GenerateTypeScriptTypes(document);

            // Generate React files
            var reactFiles = GenerateReactFiles(document, typescriptTypes);

            // Write the files to disk
            WriteFilesToDisk(reactFiles, "output/directory/path");

            Console.WriteLine("React files generated successfully!");
        }

        static string GenerateTypeScriptTypes(OpenApiDocument document)
        {
            var settings = new TypeScriptClientGeneratorSettings
            {
                GenerateClientClasses = false,
                GenerateDtoTypes = true, // Enable DTO type generation
                TypeScriptGeneratorSettings =
                {
                    TypeStyle = TypeScriptTypeStyle.Interface,
                    NullValue = TypeScriptNullValue.Null,
                    TemplateDirectory = "Templates",
                }
            };

            var generator = new TypeScriptClientGenerator(document, settings);
            return generator.GenerateFile();
        }

        static List<ReactFileModel> GenerateReactFiles(OpenApiDocument document, string typescriptTypes)
        {
            var files = new List<ReactFileModel>();

            // Example data for placeholders
            var modelName = "ExampleModel";
            var apiUrl = "/api/example";
            var sliceName = "example";
            var componentName = "ExampleComponent";

            // 1. Generate TypeScript types file
            files.Add(new ReactFileModel { FileName = $"{componentName}.types.ts", FileContent = typescriptTypes });

            // 2. Generate API file
            var apiTemplate = Template.Parse(@"
import axios from 'axios';

export const fetch{{ className }} = async () => {
    const response = await axios.get('{{ apiUrl }}');
    return response.data;
};
");
            var apiModel = new
            {
                className = modelName,
                apiUrl = apiUrl
            };
            var apiContent = apiTemplate.Render(apiModel);
            files.Add(new ReactFileModel { FileName = $"{componentName}.api.ts", FileContent = apiContent });

            // 3. Generate Slice file
            var sliceTemplate = Template.Parse(@"
import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { fetch{{ className }} } from './{{ fileName }}.api';

export const fetch{{ className }}Requested = createAsyncThunk(
    '{{ sliceName }}/fetch{{ className }}',
    async () => {
        const data = await fetch{{ className }}();
        return data;
    }
);

const {{ sliceName }}Slice = createSlice({
    name: '{{ sliceName }}',
    initialState: {
        data: [],
        status: 'idle',
        error: null
    },
    reducers: {},
    extraReducers: (builder) => {
        builder
            .addCase(fetch{{ className }}Requested.pending, (state) => {
                state.status = 'loading';
            })
            .addCase(fetch{{ className }}Requested.fulfilled, (state, action) => {
                state.status = 'succeeded';
                state.data = action.payload;
            })
            .addCase(fetch{{ className }}Requested.rejected, (state, action) => {
                state.status = 'failed';
                state.error = action.error.message;
            });
    }
});

export default {{ sliceName }}Slice.reducer;
");
            var sliceModel = new
            {
                className = modelName,
                fileName = componentName,
                sliceName = sliceName
            };
            var sliceContent = sliceTemplate.Render(sliceModel);
            files.Add(new ReactFileModel { FileName = $"{componentName}Slice.ts", FileContent = sliceContent });

            // 4. Generate Saga file
            var sagaTemplate = Template.Parse(@"
import { takeEvery, call, put } from 'redux-saga/effects';
import { fetch{{ className }}Requested } from './{{ fileName }}.api';

function* fetch{{ className }}Saga() {
    try {
        const data = yield call(fetch{{ className }});
        yield put({ type: '{{ sliceName }}/fetch{{ className }}Succeeded', payload: data });
    } catch (error) {
        yield put({ type: '{{ sliceName }}/fetch{{ className }}Failed', payload: error.message });
    }
}

export function* watch{{ className }}() {
    yield takeEvery('{{ sliceName }}/fetch{{ className }}Requested', fetch{{ className }}Saga);
}
");
            var sagaModel = new
            {
                className = modelName,
                fileName = componentName,
                sliceName = sliceName
            };
            var sagaContent = sagaTemplate.Render(sagaModel);
            files.Add(new ReactFileModel { FileName = $"{componentName}Saga.ts", FileContent = sagaContent });

            // 5. Generate Component Container file
            var containerTemplate = Template.Parse(@"
import React, { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { fetch{{ className }}Requested } from './{{ fileName }}Slice';
import { {{ className }}Content } from './{{ fileName }}Content';

export const {{ className }}Container = () => {
    const dispatch = useDispatch();
    const data = useSelector((state) => state.{{ sliceName }}.data);

    useEffect(() => {
        dispatch(fetch{{ className }}Requested());
    }, [dispatch]);

    return (
        <{{ className }}Content data={data} />
    );
};
");
            var containerModel = new
            {
                className = modelName,
                fileName = componentName,
                sliceName = sliceName
            };
            var containerContent = containerTemplate.Render(containerModel);
            files.Add(new ReactFileModel { FileName = $"{componentName}Container.tsx", FileContent = containerContent });

            // 6. Generate Component Content file
            var contentTemplate = Template.Parse(@"
import React from 'react';

export const {{ className }}Content = ({ data }) => {
    return (
        <div>
            <h1>{{ className }}</h1>
            <ul>
                {data.map((item, index) => (
                    <li key={index}>{item.name}</li>
                ))}
            </ul>
        </div>
    );
};
");
            var contentModel = new
            {
                className = modelName
            };
            var contentContent = contentTemplate.Render(contentModel);
            files.Add(new ReactFileModel { FileName = $"{componentName}Content.tsx", FileContent = contentContent });

            return files;
        }

        static void WriteFilesToDisk(List<ReactFileModel> files, string outputDirectory)
        {
            foreach (var file in files)
            {
                var filePath = Path.Combine(outputDirectory, file.FileName);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, file.FileContent);
            }
        }
    }

    public class ReactFileModel
    {
        public string FileName { get; set; }
        public string FileContent { get; set; }
    }
}
