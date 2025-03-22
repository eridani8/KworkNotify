﻿namespace KworkNotify.Core.Interfaces;

public interface IAppSettings
{
    string SiteUrl { get; init; }
    int PagesAmount { get; init; }
    int MinDelay { get; init; }
    int MaxDelay { get; init; }
    Dictionary<string, string> Headers { get; init; }
    List<long> AdminIds { get; init; }
    string BackupScriptPath { get; init; }
    string BackupWorkingDirectory { get; init; }
    int BackupInterval { get; init; }
    int StatisticPushInterval { get; init; }
}