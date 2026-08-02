public sealed class ExtractionProfile
{
	public required string Name { get; init; }
	public required List<FieldSpec> Fields { get; init; }
}