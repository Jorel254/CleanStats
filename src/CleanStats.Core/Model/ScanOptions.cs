using System;
using System.Collections.Generic;
using System.Text;

namespace CleanStats.Core.Model;

public class ScanOptions
{
    public string RootPath { get; }
    public string[] ExcludedPaths => _excludedPathsSet.ToArray();
    private readonly HashSet<string> _excludedPathsSet;
    public bool IncludeHiddenFiles { get; }

    public ScanOptions(string rootPath, string[] excludedPaths, bool includeHiddenFiles)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("El path raíz no puede ser nulo o vacío.", nameof(rootPath));
        }
        RootPath = rootPath;
        _excludedPathsSet = new HashSet<string>(excludedPaths ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        IncludeHiddenFiles = includeHiddenFiles;
    }
}
