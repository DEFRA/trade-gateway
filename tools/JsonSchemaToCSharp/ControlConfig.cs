using System.ComponentModel.DataAnnotations;

namespace JsonSchemaToCSharp
{
    public record ControlConfig
    {
        [Required(ErrorMessage = "outputRoot is required")]
        public required string OutputRoot { get; init; }

        [Required(ErrorMessage = "namespaceRoot is required")]
        public required string NamespaceRoot { get; init; }

        [Required(ErrorMessage = "schemas array is required")]
        [MinLength(1, ErrorMessage = "schemas must contain at least one entry")]
        public required List<SchemaGroup> Schemas { get; init; }
    }

    public record SchemaGroup
    {
        [Required(ErrorMessage = "namespace is required for a schema group")]
        public required string Namespace { get; set; }

        [Required(ErrorMessage = "schemaItems is required for a schema group")]
        [MinLength(1, ErrorMessage = "schemaItems must contain at least one schema path")]
        public required List<string> SchemaItems { get; set; }
    }
}
