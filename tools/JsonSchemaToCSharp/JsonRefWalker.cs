using System.Text.Json;

namespace JsonSchemaToCSharp
{
    public static class JsonRefWalker
    {
        public static void ProcessElement(
            JsonElement el,
            string currentDir,
            HashSet<string> processedFiles,
            Queue<string> toProcess
        )
        {
            switch (el.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in el.EnumerateObject())
                    {
                        if (prop.NameEquals("$ref") && prop.Value.ValueKind == JsonValueKind.String)
                        {
                            TryEnqueueRef(prop.Value.GetString(), currentDir, processedFiles, toProcess);
                        }
                        else if (prop.Value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
                        {
                            ProcessElement(prop.Value, currentDir, processedFiles, toProcess);
                        }
                    }
                    break;

                case JsonValueKind.Array:
                    foreach (var itemEl in el.EnumerateArray())
                    {
                        if (itemEl.ValueKind == JsonValueKind.Object || itemEl.ValueKind == JsonValueKind.Array)
                            ProcessElement(itemEl, currentDir, processedFiles, toProcess);
                    }
                    break;

                default:
                    break;
            }
        }

        private static void TryEnqueueRef(
            string? rv,
            string currentDir,
            HashSet<string> processedFiles,
            Queue<string> toProcess
        )
        {
            if (string.IsNullOrWhiteSpace(rv) || rv.StartsWith('#'))
                return;

            var filePart = rv.Split('#', 2)[0];
            if (string.IsNullOrWhiteSpace(filePart))
                return;

            if (!filePart.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                return;

            string refPath;

            // If it's a file:// URI, use LocalPath
            if (Uri.TryCreate(filePart, UriKind.Absolute, out var absUri) && absUri.IsFile)
            {
                refPath = absUri.LocalPath;
            }
            else if (Path.IsPathRooted(filePart))
            {
                refPath = Path.GetFullPath(filePart);
            }
            else
            {
                refPath = Path.GetFullPath(Path.Combine(currentDir, filePart));
            }

            if (!File.Exists(refPath))
            {
                throw new FileNotFoundException("Could not find dependent file", refPath);
            }

            if (!processedFiles.Contains(refPath))
                toProcess.Enqueue(refPath);
        }
    }
}
