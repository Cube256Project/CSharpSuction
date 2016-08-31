using Common;
using CSharpSuction.Configuration;
using CSharpSuction.Exceptions;
using CSharpSuction.Generators.Documentation;
using CSharpSuction.Generators.Executable;
using CSharpSuction.Generators.Project;
using CSharpSuction.Input;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CSharpSuction
{
    /// <summary>
    /// Mediates between the <see cref="Suction"/> and the <see cref="Project"/> objects.
    /// </summary>
    class ProjectApplicator
    {
        /// <summary>
        /// Applies the project settings to the suction context.
        /// </summary>
        /// <param name="suction"></param>
        public IList<INameInfo> ApplyProject(Suction suction, ProjectDescriptor project)
        {
            foreach (var rp in project.ReferencePath)
            {
                suction.AssemblySearchPaths.Add(rp);
            }

            foreach (var r in project.References)
            {
                suction.AddAssemblyReference(new ArgumentReferenceInfo(r));
            }

            // add sources
            foreach (var include in project.Includes)
            {
                suction.AddSource(include.AbsolutePath, include.Filter);
            }

            // entry point, given as a GOAL
            if (null != project.EntryPoint)
            {
                var entryname = suction.LookupName(project.EntryPoint);
                if (!entryname.Any())
                {
                    throw new Exception("failed to resolve entry point '" + project.EntryPoint + "'.");
                }
                else if (entryname.Count() > 1)
                {
                    throw new Exception("entry point '" + project.EntryPoint + "' is ambigous.");
                }

                suction.EntryPoint = entryname.First().QualifiedName;
            }
            else
            {
                suction.OutputKind = Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary;
            }

            // [Output]
            suction.OutputName = project.OutputName ?? suction.OutputName;

            if (null != project.OutputKind)
            {
                OutputKind value;
                if (Enum.TryParse(project.OutputKind, out value))
                {
                    suction.OutputKind = value;
                }
                else if (project.OutputKind == "exe")
                {
                    suction.OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication;
                }
                else if (project.OutputKind == "dll")
                {
                    suction.OutputKind = Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary;
                }
                else
                {
                    throw new ArgumentException("output kind '" + project.OutputKind + "' is unsupported.");
                }
            }

            // version specification
            if(null != project.Version)
            {
                Version version;
                if (!Version.TryParse(project.Version, out version))
                {
                    throw new ArgumentException("bad version spec.");
                }

                suction.OutputVersion = version;
            }

            // object factory support
            suction.EnableObjectFactory = project.EnableObjectFactory;

            // project GOALs ...
            var result = new List<INameInfo>();
            foreach (var goal in project.Goals)
            {
                result.AddRange(suction.LookupName(goal));
            }

            // emitters
            foreach(var emit in project.Emits)
            {
                ResolveEmitKind(emit);
            }

            return result;
        }

        private void ResolveEmitKind(EmitInstruction emit)
        {
            if (null == emit.EmitterType)
            {
                switch (emit.Kind)
                {
                    case "assembly":
                        emit.EmitterType = typeof(EmitAssembly);
                        break;

                    case "documentation":
                        emit.EmitterType = typeof(EmitDocumentation);
                        break;

                    case "project-copy":
                        emit.EmitterType = typeof(EmitProjectCopy);
                        break;

                    case "project-original":
                        emit.EmitterType = typeof(EmitProjectOnOriginalSource);
                        break;

                    case "typescript":
                        emit.EmitterType = PartialTypeResolver.Resolve("EmitTypeScript");
                        break;

                    case "jscript":
                        emit.EmitterType = PartialTypeResolver.Resolve("EmitJScript");
                        break;

                    default:
                        throw new SuctionConfigurationException("unsupported emit type '" + emit.Kind + "'.");
                }
            }
        }
    }
}
