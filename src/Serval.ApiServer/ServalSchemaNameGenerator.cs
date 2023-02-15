using NJsonSchema.Generation;

namespace Serval.ApiServer;

public class ServalSchemaNameGenerator : DefaultSchemaNameGenerator
{
    public override string Generate(Type type)
    {
        string name = base.Generate(type);
        if (name.EndsWith("Dto"))
            return name[0..^3];
        return name;
    }
}
