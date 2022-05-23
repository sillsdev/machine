using NJsonSchema.Generation;

namespace SIL.Machine.WebApi.Server;

public class MachineSchemaNameGenerator : DefaultSchemaNameGenerator
{
    public override string Generate(Type type)
    {
        string name = base.Generate(type);
        if (name.EndsWith("Dto"))
            return name[0..^3];
        return name;
    }
}
