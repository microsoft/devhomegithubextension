// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Windows.DevHome.SDK;

namespace GitHubExtension.Test;

public class MockExtensionAdaptiveCard : IExtensionAdaptiveCard
{
    private int updateCount;

    public int UpdateCount
    {
        get => updateCount;
        set => updateCount = value;
    }

    public MockExtensionAdaptiveCard(string templateJson, string dataJson, string state)
    {
        TemplateJson = templateJson;
        DataJson = dataJson;
        State = state;
    }

    public string DataJson
    {
        get; set;
    }

    public string State
    {
        get; set;
    }

    public string TemplateJson
    {
        get; set;
    }

    public ProviderOperationResult Update(string templateJson, string dataJson, string state)
    {
        UpdateCount++;
        TemplateJson = templateJson;
        DataJson = dataJson;
        State = state;
        return new ProviderOperationResult(ProviderOperationStatus.Success, null, "Update() succeeded", "Update() succeeded");
    }
}
