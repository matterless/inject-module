using System;

namespace Matterless.Inject
{
    public interface IContext
    {
        string id { get; }
        IContext parent { get; }

        Type GetClassType(Type type);
        Type GetClassTypeFromParent(Type type);

        object GetInstanceFromParent(Type type);
        object GetInstanceOfType(Type type);

        bool HasSettings(Type type);
        TSettings GetSettings<TSettings>();
        object GetSettings(Type type);
    }
}