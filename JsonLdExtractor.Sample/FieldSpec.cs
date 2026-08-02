public sealed class FieldSpec
{
	public FieldSpec(string fieldName, params string[] semanticKeys)
	{
		FieldName = fieldName;
		SemanticKeys = semanticKeys;
	}

	public string FieldName { get; }
	public IReadOnlyList<string> SemanticKeys { get; }
}