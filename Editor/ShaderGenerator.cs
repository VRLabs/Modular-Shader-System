using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

namespace VRLabs.ModularShaderSystem
{
    public class ShaderGenerator
    {
        private ModularShader _shader;
        private List<ShaderModule> _modules;
        private List<EnableProperty> _nonCgPropertyEnablers;
        private List<string> _nonCgPropertyNames;
        private List<Property> _properties;

        public void GenerateMainShader(string path, ModularShader shader, bool hideVariants = false)
        {
            _shader = shader;
            var shaderFile = new StringBuilder();
            _modules = FindAllModules(_shader);
            _nonCgPropertyEnablers = _modules
                .Where(x => x != null && x.Templates?.Count(y => !y.IsCGOnly) > 0 && (x.Enabled != null && !string.IsNullOrWhiteSpace(x.Enabled.Name)))
                .Select(x => x.Enabled).ToList();
            _nonCgPropertyNames = _nonCgPropertyEnablers.Select(x => x.Name).Distinct().OrderBy(x => x).ToList();
            _properties = FindAllProperties(_shader);

            WriteProperties(shaderFile);
            
            int currentlyIteratedObject = 0;

            EnablePropertyValue[] currentSettings = new EnablePropertyValue[_nonCgPropertyNames.Count];

            var variants = GenerateVariantsRecursive(shaderFile, currentlyIteratedObject, currentSettings, hideVariants);

            foreach ((string variantCode, StringBuilder shaderVariant) in variants)
            {
                StringBuilder finalFile = CleanupShaderFile(shaderVariant);
                File.WriteAllText($"{path}/{_shader.Name}{variantCode}.shader", finalFile.ToString());
            }

            AssetDatabase.Refresh();
            
            _shader = null;
            _modules = null;
            _nonCgPropertyEnablers = null;
            _nonCgPropertyNames = null;
            _properties = null;
        }

        public void GenerateOptimizedShader(ModularShader shader, List<EnablePropertyValue> enableProperties)
        {
            _shader = shader;
            var shaderFile = new StringBuilder();
            _modules = FindAllModules(_shader);

            _properties = FindUsedProperties(_shader, enableProperties);

            var variantEnabledModules = _modules.Where(x => x.Enabled == null || string.IsNullOrWhiteSpace(x.Enabled.Name) || enableProperties.Select(y => y.Name).Contains(x.Enabled.Name));

            string suffix = string.Join("", enableProperties.Select(x => x.Value));

            shaderFile.PrependLine("{");
            shaderFile.PrependLine($"Shader \"Hidden/opt/{_shader.ShaderPath}{suffix}\"");

            WriteProperties(shaderFile);

            // Write shader skeleton from templates 
            WriteShaderSkeleton(shaderFile, enableProperties);

            var functions = new List<ShaderFunction>();
            foreach (var module in variantEnabledModules)
                functions.AddRange(module.Functions);

            // Write module variables
            WriteShaderVariables(shaderFile, functions);

            // Write Functions to shader
            WriteShaderFunctions(shaderFile, functions);

            shaderFile.AppendLine("}");

            File.WriteAllText($"{_shader.Name}{suffix}.shader", shaderFile.ToString());

            _modules = null;
            _properties = null;
        }

        private List<(string, StringBuilder)> GenerateVariantsRecursive(StringBuilder shaderFile, int currentlyIteratedObject, EnablePropertyValue[] currentSettings, bool isVariantHidden)
        {
            var files = new List<(string, StringBuilder)>();

            //In case this is the end of the tree call it will generate the finalized variant
            if (currentlyIteratedObject >= _nonCgPropertyNames.Count)
            {
                var variantShader = new StringBuilder(shaderFile.ToString());
                files.Add(GenerateShaderVariant(variantShader, currentSettings, isVariantHidden));
                return files;
            }

            // Searches all possible values for the current property enabler
            List<int> possibleValues = _nonCgPropertyEnablers.Where(x => x.Name.Equals(_nonCgPropertyNames[currentlyIteratedObject]))
                .Select(x => x.EnableValue)
                .Append(0)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            // Recursively calls this function once for every possible branch referencing the next property to branch into.
            // Then aggregates all the resulting variants from this branch of the tree call, to then return it to its root
            foreach (int value in possibleValues)
            {
                var newSettings = new EnablePropertyValue[_nonCgPropertyNames.Count];
                Array.Copy(currentSettings, newSettings, _nonCgPropertyNames.Count);
                newSettings[currentlyIteratedObject] = new EnablePropertyValue { Name =_nonCgPropertyNames[currentlyIteratedObject], Value = value };
                List<(string, StringBuilder)> returnFiles = GenerateVariantsRecursive(shaderFile, currentlyIteratedObject + 1, newSettings, isVariantHidden);

                files.AddRange(returnFiles);
            }

            return files;
        }

        private (string, StringBuilder) GenerateShaderVariant(StringBuilder shaderFile, EnablePropertyValue[] currentSettings, bool isVariantHidden)
        {
            string suffix = "";
            if (currentSettings.Any(x => x.Value != 0))
                suffix = string.Join("", currentSettings.Select(x => x.Value));

            var variantEnabledModules = _modules
                .Where(x => x != null)
                .Where(x => x.Enabled == null || string.IsNullOrWhiteSpace(x.Enabled.Name) || currentSettings.Select(y => y.Name).Contains(x.Enabled.Name));

            // Add Shader Location
            shaderFile.PrependLine("{");
            
            if(isVariantHidden)
                shaderFile.PrependLine($"Shader \"Hidden/{_shader.ShaderPath}{suffix}\"");
            else
                shaderFile.PrependLine($"Shader \"{_shader.ShaderPath}{suffix}\"");

            // Write shader skeleton from templates 
            WriteShaderSkeleton(shaderFile, currentSettings);

            // Get functions currently used in this variant.
            var functions = new List<ShaderFunction>();
            foreach (var module in variantEnabledModules)
                functions.AddRange(module.Functions);

            // Write module variables
            WriteShaderVariables(shaderFile, functions);

            // Write Functions to shader
            WriteShaderFunctions(shaderFile, functions);

            if(!string.IsNullOrWhiteSpace(_shader.CustomEditor))
                shaderFile.AppendLine($"CustomEditor \"{_shader.CustomEditor}\"");
            shaderFile.AppendLine("}");

            
            MatchCollection m = Regex.Matches(shaderFile.ToString(), @"#K#.*$", RegexOptions.Multiline);
            foreach (Match match in m)
                shaderFile.Replace(match.Value, "");

            shaderFile.Replace("\r\n", "\n");
            return (suffix, shaderFile);
        }

        private static void WriteShaderVariables(StringBuilder shaderFile, List<ShaderFunction> functions)
        {
            WriteVariablesToSink(shaderFile, functions, MSSConstants.DEFAULT_VARIABLES_SINK, true);
            foreach (var sink in functions.Select(x => x.VariableSinkKeyword).Distinct().Where(x => !string.IsNullOrEmpty(x) && !x.Equals(MSSConstants.DEFAULT_VARIABLES_SINK)))
                WriteVariablesToSink(shaderFile, functions, sink);
        }

        private static void WriteVariablesToSink(StringBuilder shaderFile, List<ShaderFunction> functions, string sink, bool isDefaultSink = false)
        {
            var variablesDeclaration = new StringBuilder();
            foreach (var variable in functions
                .Where(x => x.VariableSinkKeyword.Equals(sink) || (isDefaultSink && string.IsNullOrWhiteSpace(x.VariableSinkKeyword)))
                .SelectMany(x => x.UsedVariables)
                .Distinct()
                .OrderBy(x => x.Type))
            {
                if (variable.Type.Equals("sampler2D") || variable.Type.Equals("Texture2D"))
                    variablesDeclaration.AppendLine($"{variable.Type} {variable.Name}; float4 {variable.Name}_ST;");
                else
                    variablesDeclaration.AppendLine($"{variable.Type} {variable.Name};");
            }

            variablesDeclaration.AppendLine("#K#" + sink);
            shaderFile.Replace("#K#" + sink, variablesDeclaration.ToString());
        }

        private void WriteShaderFunctions(StringBuilder shaderFile, List<ShaderFunction> functions)
        {
            foreach (var function in functions.Where(x => x.AppendAfter.StartsWith("#K#")))
            {
                if (!shaderFile.Contains(function.AppendAfter)) continue;

                ShaderModule module = _modules.Find(x => x.Functions.Contains(function));
                StringBuilder functionCode = new StringBuilder();
                StringBuilder functionCallSequence = new StringBuilder();
                int tabs = 2;
                tabs = WriteFunctionCallSequence(functions, function, module, functionCode, functionCallSequence, tabs);
                string codeSink = string.IsNullOrWhiteSpace(function.CodeSinkKeyword) ? MSSConstants.DEFAULT_CODE_SINK : function.CodeSinkKeyword;

                functionCode.AppendLine("#K#" + codeSink);
                shaderFile.Replace("#K#" + codeSink, functionCode.ToString());

                functionCallSequence.AppendLine(function.AppendAfter);
                shaderFile.Replace(function.AppendAfter, functionCallSequence.ToString());
            }
        }

        private static int WriteFunctionCallSequence(List<ShaderFunction> functions, ShaderFunction function, ShaderModule module, 
            StringBuilder functionCode, StringBuilder functionCallSequence, int tabs)
        {
            functionCode.AppendLine(function.ShaderFunctionCode.Template);

            if (module.Enabled != null && !string.IsNullOrWhiteSpace(module.Enabled.Name))
            {
                functionCallSequence.AppendLine($"if({module.Enabled.Name} == {module.Enabled.EnableValue})");
                functionCallSequence.AppendLine("{");
                tabs++;
                functionCallSequence.AppendLine($"{function.Name}();");
                foreach (var fn in functions.Where(x => x.AppendAfter.Equals(function.Name)).OrderBy(x => x.Priority))
                    WriteFunctionCallSequence(functions, fn, module, functionCode, functionCallSequence, tabs);

                functionCallSequence.AppendLine("}");
            }
            else
            {
                functionCallSequence.AppendLine($"{function.Name}();");
                foreach (var fn in functions.Where(x => x.AppendAfter.Equals(function.Name)).OrderBy(x => x.Priority))
                    WriteFunctionCallSequence(functions, fn, module, functionCode, functionCallSequence, tabs);
            }

            return tabs;
        }

        private void WriteShaderSkeleton(StringBuilder shaderFile, IEnumerable<EnablePropertyValue> currentSettings)
        {
            shaderFile.AppendLine("SubShader");
            shaderFile.AppendLine("{");
            
            shaderFile.AppendLine(_shader.ShaderTemplate.Template);

            WriteModuleTemplates(shaderFile, currentSettings);
        }

        public static List<ShaderModule> FindAllModules(ModularShader shader)
        {
            List<ShaderModule> modules = new List<ShaderModule>();
            if (shader == null) return modules;
            modules.AddRange(shader.BaseModules);
            modules.AddRange(shader.AdditionalModules);
            return modules;
        }

        public static List<ShaderFunction> FindAllFunctions(ModularShader shader)
        {
            var functions = new List<ShaderFunction>();
            if (shader == null) return functions;
            foreach (var module in shader.BaseModules)
                functions.AddRange(module.Functions);

            foreach (var module in shader.AdditionalModules)
                functions.AddRange(module.Functions);
            return functions;
        }

        public static List<ModuleTemplate> FindAllTemplates(ModularShader shader)
        {
            var templates = new List<ModuleTemplate>();
            if (shader == null) return templates;
            foreach (var module in shader.BaseModules)
                templates.AddRange(module.Templates);

            foreach (var module in shader.AdditionalModules)
                templates.AddRange(module.Templates);
            return templates;
        }

        public static List<Property> FindAllProperties(ModularShader shader)
        {
            
            List<Property> properties = new List<Property>();
            if (shader == null) return properties;

            properties.AddRange(shader.Properties);

            foreach (var module in shader.BaseModules.Where(x => x != null))
            {
                properties.AddRange(module.Properties.Where(x => !string.IsNullOrWhiteSpace(x.Name)));
                if(!string.IsNullOrWhiteSpace(module.Enabled.Name))
                    properties.Add(module.Enabled);
            }

            foreach (var module in shader.AdditionalModules.Where(x => x != null))
            {
                properties.AddRange(module.Properties.Where(x => !string.IsNullOrWhiteSpace(x.Name)));
                if(!string.IsNullOrWhiteSpace(module.Enabled.Name))
                    properties.Add(module.Enabled);
            }

            return properties.Distinct().ToList();
        }

        public static List<Property> FindUsedProperties(ModularShader shader, IEnumerable<EnablePropertyValue> values)
        {
            List<Property> properties = new List<Property>();

            properties.AddRange(shader.Properties);

            foreach (var module in shader.BaseModules.Where(x => x.Enabled == null || string.IsNullOrWhiteSpace(x.Enabled.Name) || 
                values.Count(y => y.Name.Equals(x.Enabled.Name) && y.Value == x.Enabled.EnableValue) > 0))
                properties.AddRange(module.Properties);

            foreach (var module in shader.AdditionalModules.Where(x => x.Enabled == null || string.IsNullOrWhiteSpace(x.Enabled.Name) || 
                values.Count(y => y.Name.Equals(x.Enabled.Name) && y.Value == x.Enabled.EnableValue) > 0))
                properties.AddRange(module.Properties);

            return properties.Distinct().ToList();
        }
        
        private void WriteModuleTemplates(StringBuilder shaderFile, IEnumerable<EnablePropertyValue> currentSettings)
        {
            foreach (var module in _modules.Where(x => x != null && (string.IsNullOrWhiteSpace(x.Enabled.Name) || currentSettings.Select(y => y.Name).Contains(x.Enabled.Name))))
            {
                foreach (var template in module.Templates)
                {
                    var tmp = new StringBuilder();
                    tmp.AppendLine(template.Template.ToString());
                    tmp.AppendLine("#K#" + template.Keyword);
                    shaderFile.Replace("#K#" + template.Keyword, tmp.ToString());
                }
            }

            shaderFile.AppendLine("}");
        }

        private void WriteProperties(StringBuilder shaderFile)
        {
            shaderFile.AppendLine("Properties");
            shaderFile.AppendLine("{");

            foreach (var prop in _properties)
            {
                shaderFile.AppendLine($"{prop.Attributes} {prop.Name}(\"{prop.DisplayName}\", {prop.Type}) = {prop.DefaultValue}");
            }
            shaderFile.AppendLine("}");
        }
        
        private static StringBuilder CleanupShaderFile(StringBuilder shaderVariant)
        {
            var finalFile = new StringBuilder(); ;
            using (var sr = new StringReader(shaderVariant.ToString()))
            {
                string line;
                int tabs = 0;
                bool deleteEmptyLine = false;
                while ((line = sr.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (string.IsNullOrEmpty(line))
                    {
                        if (deleteEmptyLine)
                            continue;
                        deleteEmptyLine = true;
                    }
                    else
                    {
                        deleteEmptyLine = false;
                    }

                    if (line.StartsWith("}"))
                        tabs--;
                    finalFile.AppendLineTabbed(tabs, line);
                    if (line.StartsWith("{"))
                        tabs++;
                }
            }

            return finalFile;
        }
    }
}