namespace SIL.Machine.WebApi.DataAccess;

public static class DataAccessClassMap
{
    public static void RegisterConventions(string nspace, params IConvention[] conventions)
    {
        var conventionPack = new ConventionPack();
        conventionPack.AddRange(conventions);
        ConventionRegistry.Register(nspace, conventionPack, t => t.Namespace != null && t.Namespace.StartsWith(nspace));
    }

    public static void RegisterClass<T>(Action<BsonClassMap<T>> mapSetup)
    {
        BsonClassMap.RegisterClassMap<T>(
            cm =>
            {
                cm.AutoMap();
                mapSetup?.Invoke(cm);
            }
        );
    }
}
