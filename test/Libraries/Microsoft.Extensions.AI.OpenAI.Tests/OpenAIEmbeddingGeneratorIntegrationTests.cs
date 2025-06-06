﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

public class OpenAIEmbeddingGeneratorIntegrationTests : EmbeddingGeneratorIntegrationTests
{
    protected override IEmbeddingGenerator<string, Embedding<float>>? CreateEmbeddingGenerator() =>
        IntegrationTestHelpers.GetOpenAIClient()
        ?.GetEmbeddingClient(TestRunnerConfiguration.Instance["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small").AsIEmbeddingGenerator();
}
