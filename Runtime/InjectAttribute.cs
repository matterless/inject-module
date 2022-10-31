using System;

namespace Matterless.Inject
{
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class InjectAttribute : Attribute
    {
        public string name { get; private set; }

        public InjectAttribute()
        {
            name = null;
        }
    }
}