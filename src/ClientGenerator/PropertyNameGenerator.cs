using NJsonSchema;
using NJsonSchema.CodeGeneration;

namespace Brighid.Identity.ClientGenerator
{
    /// <inheritdoc />
    public class PropertyNameGenerator : IPropertyNameGenerator
    {
        /// <inheritdoc />
        public string Generate(JsonSchemaProperty property)
        {
            object? displayName = null;
            property.ExtensionData?.TryGetValue("x-display-name", out displayName);

            var result = (string?)displayName ?? property.Name;
            return char.ToUpper(result[0]) + result[1..];
        }
    }
}
