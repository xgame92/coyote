// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.SystematicTesting.Interception;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    internal class ApiLogTransform : AssemblyTransform
    {
        /// <summary>
        /// The current method being transformed.
        /// </summary>
        private MethodDefinition Method;

        /// <summary>
        /// A helper class for editing method body.
        /// </summary>
        private ILProcessor Processor;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiLogTransform"/> class.
        /// </summary>
        internal ApiLogTransform(ILogger log)
            : base(log)
        {
        }

        /// <inheritdoc/>
        internal override void VisitType(TypeDefinition type)
        {
            this.Method = null;
            this.Processor = null;
        }

        /// <inheritdoc/>
        internal override void VisitMethod(MethodDefinition method)
        {
            this.Method = null;

            // Only non-abstract method bodies can be rewritten.
            if (method.IsAbstract)
            {
                return;
            }

            this.Method = method;
            this.Processor = method.Body.GetILProcessor();

            bool isTestMethod = false;
            if (method.CustomAttributes.Count > 0)
            {
                // Search for a method with a unit testing framework attribute.
                foreach (var attr in method.CustomAttributes)
                {
                    if (attr.AttributeType.FullName == "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute")
                    {
                        isTestMethod = true;
                        break;
                    }
                }
            }

            if (isTestMethod)
            {
                Instruction instruction = method.Body.Instructions.FirstOrDefault();

                TypeDefinition resolvedDeclaringType = method.DeclaringType.Resolve();
                string name = GetFullyQualifiedMethodName(method, resolvedDeclaringType);
                this.Processor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldstr, name));

                MethodReference loggerMethod = this.GetLoggerMethod("LogTestStarted");
                this.Processor.InsertBefore(instruction, Instruction.Create(OpCodes.Call, loggerMethod));

                FixInstructionOffsets(this.Method);
            }

            // Rewrite the method body instructions.
            this.VisitInstructions(method);
        }

        /// <inheritdoc/>
        internal override Instruction VisitInstruction(Instruction instruction)
        {
            if (this.Method is null)
            {
                return instruction;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                instruction.Operand is MethodReference methodReference)
            {
                instruction = this.VisitCallInstruction(instruction, methodReference);
            }

            return instruction;
        }

        /// <summary>
        /// Transforms the specified non-generic <see cref="OpCodes.Call"/> or <see cref="OpCodes.Callvirt"/> instruction.
        /// </summary>
        /// <returns>The unmodified instruction, or the newly replaced instruction.</returns>
        private Instruction VisitCallInstruction(Instruction instruction, MethodReference method)
        {
            try
            {
                TypeDefinition resolvedDeclaringType = method.DeclaringType.Resolve();
                if (IsEligibleType(resolvedDeclaringType))
                {
                    string name = GetFullyQualifiedMethodName(method, resolvedDeclaringType);
                    this.Processor.InsertBefore(instruction, Instruction.Create(OpCodes.Ldstr, name));

                    MethodReference loggerMethod = this.GetLoggerMethod("LogInvocation");
                    this.Processor.InsertBefore(instruction, Instruction.Create(OpCodes.Call, loggerMethod));

                    FixInstructionOffsets(this.Method);
                }
            }
            catch (AssemblyResolutionException)
            {
                // Skip this method, we are only interested in methods that can be resolved.
            }

            return instruction;
        }

        private static string GetFullyQualifiedMethodName(MethodReference method, TypeDefinition declaringType)
        {
            string name;
            if (method.DeclaringType is GenericInstanceType genericType)
            {
                name = $"{genericType.ElementType.FullName.Split('`')[0]}.{method.Name}";
            }
            else
            {
                name = $"{declaringType.FullName}.{method.Name}";
            }

            return name;
        }

        private MethodReference GetLoggerMethod(string name)
        {
            var loggerType = this.Method.Module.ImportReference(typeof(ApiLogger)).Resolve();
            MethodReference loggerMethod = loggerType.Methods.FirstOrDefault(m => m.Name == name);
            return this.Method.Module.ImportReference(loggerMethod);
        }

        /// <summary>
        /// Checks if the specified type should be logged.
        /// </summary>
        private static bool IsEligibleType(TypeDefinition type)
        {
            if (type is null)
            {
                return false;
            }

            string module = Path.GetFileName(type.Module.FileName);
            if (!(module is "System.Private.CoreLib.dll" || module is "mscorlib.dll"))
            {
                return false;
            }

            bool isEligibleSystemType = type.Namespace.StartsWith(typeof(System.Exception).Namespace) &&
                (type.Name is "Random" || type.Name is "Guid" || type.Name is "DateTime" || type.Name is "TimeSpan");
            bool isEligibleThreadingType = type.Namespace.StartsWith(typeof(System.Runtime.CompilerServices.TaskAwaiter).Namespace) ||
                type.Namespace.StartsWith(typeof(System.Threading.Thread).Namespace);
            bool isEligibleDiagnosticsType = type.Namespace.StartsWith(typeof(System.Diagnostics.Process).Namespace) &&
                type.Name is "Process";
            bool isEligibleTimersType = type.Namespace.StartsWith(typeof(System.Timers.Timer).Namespace);
            if (isEligibleSystemType || isEligibleThreadingType || isEligibleDiagnosticsType || isEligibleTimersType)
            {
                return true;
            }

            return false;
        }
    }
}
