using UnityEngine;

namespace Matterless.Inject
{
    public class MonoInstaller : MonoBehaviour
    {
        protected DiContainer container { get; private set; }
        protected object[] arguments { get; private set; }

        /// <summary>
        /// Use this for initialization
        /// </summary>
        protected virtual void InstallBindings()
        {
            // user bindings...
        }

        internal void Install(DiContainer container, object[] arguments)
        {
            // set container
            this.container = container;
            // set arguments
            this.arguments = arguments;
            // user defined bindings
            InstallBindings();
        }
    }
}