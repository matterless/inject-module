using UnityEngine;

namespace Matterless.Inject
{
    [DefaultExecutionOrder(-200)]
    public class RootContext : BaseContext
    {
        public override string id => "root";

        protected override void StartContextInternal()
        {
            MermaidWriter.SetContext(id);
            // init with null parent;
            Init(null);
            // complete mermaid
            MermaidWriter.Complete(id);
        }
    }
}