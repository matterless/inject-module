using System.Text;
using System;
using System.Collections.Generic;
using System.IO;

namespace Matterless.Inject
{
    public static class MermaidWriter
    {
        private const string TITLE = "[TITLE]";
        private const string BODY = "[BODY]";
        private const string SUB_TITLE = "[SUB_TITLE]";
        private const string DIAGRAM = "[DIAGRAM]";
        private const string TYPE = "[TYPE]";

        private static Dictionary<string, StringBuilder> s_DependenciesPerContext;
        private static Dictionary<string, StringBuilder> s_DependenciesPerType;
        private static StringBuilder s_BodyStringBuilder;
        private static string s_OutputFolder;
        private static string s_Title;

        private static string PAGE = @"<!DOCTYPE html><html><head>
            <title>[TITLE]</title>
            <script src=""https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js""></script>
            <script>mermaid.initialize({startOnLoad:true});</script>
            <style type=""text/css"">div.mermaid { /**width: 5000px;*/ margin: auto; border: 2px solid #73AD21; }</style></head>
            <body><h1>[TITLE]</h1><a href=""index.html"">main</a>
            [BODY]
            </body></html>";

        private static string MERMAID_DIV = @"<h2>[SUB_TITLE]</h2>
        <div class=""mermaid"">
        flowchart LR
        [DIAGRAM]
        </div>";
        
        private static string CLICK_LINE = @"click [TYPE] ""[TYPE].html"" ""click""";
        
        private static bool hasInitialized => s_OutputFolder != null;
        private static string s_CurrentContext;

        private static StringBuilder currentStringBuilder => s_DependenciesPerContext[s_CurrentContext];

        public static void Init(string outputFolder, string title)
        {
            s_OutputFolder = outputFolder;
            s_Title = title;
            s_DependenciesPerContext = new Dictionary<string, StringBuilder>();
            s_DependenciesPerType = new Dictionary<string, StringBuilder>();
            s_BodyStringBuilder = new StringBuilder();

            // create output directory if not exists
            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);
        }

        internal static void SetContext(string context)
        {
            if(!hasInitialized)
                return;
            
            s_CurrentContext = context;
            s_DependenciesPerContext.Add(context, new StringBuilder());
        }

        internal static void AddDependencies(Type type, object[] dependencies, int args)
        {
            if(!hasInitialized)
                return;
            
            if(dependencies == null)
                return;

            string typeName = type.ToString();
            s_DependenciesPerType.Add(typeName, new StringBuilder());
            
            for(var i=0; i<dependencies.Length-args; i++)
            {
                if (dependencies[i] != null)
                {
                    string dependencyTypeName = dependencies[i].GetType().ToString();
                    s_DependenciesPerType[typeName].AppendLine($"    {type} --> {dependencyTypeName}");

                    if (s_DependenciesPerType.ContainsKey(dependencyTypeName))
                        s_DependenciesPerType[typeName].Append(s_DependenciesPerType[dependencyTypeName]);

                    // context
                    currentStringBuilder.AppendLine($"    {type} --> {dependencies[i].GetType()}");
                }
            }

            s_DependenciesPerType[typeName].AppendLine(CLICK_LINE.Replace(TYPE, typeName));
        }

        internal static void Complete(string context)
        {
            if(!hasInitialized)
                return;

            GenerateContextDiagram(context);

            foreach (var pair in s_DependenciesPerType)
            {
                GenerateTypeDiagram(pair.Key);
            }

        }

        private static void GenerateContextDiagram(string context)
        {
            // generate click lines
            foreach (var pair in s_DependenciesPerType)
                currentStringBuilder.AppendLine(CLICK_LINE.Replace(TYPE, pair.Key));

            // generate mermaid div
            s_BodyStringBuilder.AppendLine(
                MERMAID_DIV.Replace(SUB_TITLE, context).Replace(DIAGRAM, currentStringBuilder.ToString())
            );

            // generate page
            var page = PAGE.Replace(TITLE, s_Title).Replace(BODY, s_BodyStringBuilder.ToString());
            var filePath = Path.Combine(s_OutputFolder, "index.html"); 
            File.WriteAllText(filePath, page);
        }

        private static void GenerateTypeDiagram(string type)
        {
            var diagram = MERMAID_DIV.Replace(SUB_TITLE, type).Replace(DIAGRAM, s_DependenciesPerType[type].ToString());
            var page = PAGE.Replace(TITLE, s_Title).Replace(BODY,diagram);
            var filePath = Path.Combine(s_OutputFolder, $"{type}.html"); 
            File.WriteAllText(filePath, page);
        }
    }
}