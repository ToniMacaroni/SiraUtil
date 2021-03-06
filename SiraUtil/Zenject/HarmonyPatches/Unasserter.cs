using System;
using Zenject;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace SiraUtil.Zenject.HarmonyPatches
{
    [HarmonyPatch(typeof(InitializableManager), nameof(InitializableManager.Initialize))]
    internal class Unasserter
    {
        private static readonly MethodInfo _newFail = SymbolExtensions.GetMethodInfo(() => NewErrorBehavior(null, null, null));
        private static readonly MethodInfo _rootMethod = typeof(ModestTree.Assert).GetMethod("CreateException", new Type[] { typeof(Exception), typeof(string), typeof(object[]) });

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            for (int i = 0; i < codes.Count; i++)
            {
                var code = codes[i];

                if (code.operand != null && code.Is(OpCodes.Call, _rootMethod))
                {
                    codes[i] = new CodeInstruction(OpCodes.Callvirt, _newFail);
                    codes.RemoveAt(i + 1);
                    break;
                }
            }
            return codes.AsEnumerable();
        }

        private static void NewErrorBehavior(Exception exception, string _, object[] parameters)
        {
            var failedType = (Type)parameters[0];
            string failText = $"Error occurred while initializing IInitializable with type '{failedType.FullName}'";
            if (failedType.Name != failedType.FullName)
            {
                Plugin.Log.Critical(failText);
                Plugin.Log.Critical(exception);

                var asm = failedType.Assembly;
                var plugin = IPA.Loader.PluginManager.EnabledPlugins.FirstOrDefault(pl => pl.Assembly == asm);

                if (plugin != null)
                {
                    string addressTo = string.IsNullOrEmpty(plugin.Author) ? $"The author of {plugin.Name}" : plugin.Author;
                    for (int i = 0; i < 10; i++)
                    {
                        Plugin.Log.Warn($"Please tell {addressTo} to fix this!");
                    }
                }
            }
            else
            {
                UnityEngine.Debug.LogError(failText);
                UnityEngine.Debug.LogError(exception);
            } 
        }
    }
}