namespace SIL.Machine.WebApi.Controllers;

public static class Scopes
{
    public const string CreateEngines = "create:engines";
    public const string ReadEngines = "read:engines";
    public const string UpdateEngines = "update:engines";
    public const string DeleteEngines = "delete:engines";

    public const string CreateHooks = "create:hooks";
    public const string ReadHooks = "read:hooks";
    public const string DeleteHooks = "delete:hooks";

    public static IEnumerable<string> All =>
        new[] { CreateEngines, ReadEngines, UpdateEngines, DeleteEngines, CreateHooks, ReadHooks, DeleteHooks };
}
